using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Core.Services;
using HomeAutomation.Database;
using HomeAutomation.Database.Entities;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace HomeAutomation.Core.ScheduledJobs;

[ScheduledJob(900, DelayInSeconds = 60)]
public class FetchBoneoListingsScheduleJob(DefaultContext context, IConfiguration configuration, INotificationService notificationService, ILogger<FetchBoneoListingsScheduleJob> logger) : IScheduledJob
{
    private const string ForSaleStatus = "ForSale";
    private const string PendingStatus = "Pending";
    private const string SoldStatus = "Sold";

    private const string PendingLabel = "På G";
    private const string NotificationChannel = "hus";
    private const int RunHour = 9;

    private static readonly HttpClient Http = CreateHttpClient();

    public async Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
    {
        if (!ShouldRunToday(currentExecution, lastExecution))
            return;

        var url = configuration.GetSection("Boneo:Url").Get<string>();
        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogWarning("Schedule.FetchBoneoListings :: skipped, Boneo:Url is not configured");
            return;
        }

        logger.LogInformation("Schedule.FetchBoneoListings :: starting");
        try
        {
            var scrapedListings = await FetchListingsAsync(url, cancellationToken);
            var existingListings = await context.HouseListings.ToDictionaryAsync(x => x.ExternalId, cancellationToken);
            existingListings.Remove(existingListings.First().Key);
            existingListings.Remove(existingListings.Last().Key);

            var notifications = new List<string>();
            foreach (var listing in scrapedListings)
            {
                if (!existingListings.TryGetValue(listing.BoneoId, out var existing))
                {
                    context.HouseListings.Add(new HouseListingEntity
                    {
                        ExternalId = listing.BoneoId,
                        Address = listing.Address,
                        Price = listing.Price,
                        Info = listing.Info,
                        Url = listing.Url,
                        Status = listing.Status,
                        FirstSeen = currentExecution,
                        LastSeen = currentExecution,
                        LastChanged = currentExecution
                    });

                    notifications.Add($"*Ny annons:*\n{listing.Address}\n{FormatListing(listing)}");
                    continue;
                }

                bool changed = false;
                string previousPrice = existing.Price;
                string previousStatus = existing.Status;
                string previousAddress = existing.Address;
                string previousInfo = existing.Info;
                string previousUrl = existing.Url;

                if (!string.Equals(previousPrice, listing.Price, StringComparison.Ordinal))
                {
                    notifications.Add($"*Prisändring:*\n{listing.Address}\n{previousPrice} -> {listing.Price}\n{listing.Url}");
                    existing.Price = listing.Price;
                    changed = true;
                }

                if (string.Equals(previousStatus, PendingStatus, StringComparison.Ordinal) &&
                    string.Equals(listing.Status, ForSaleStatus, StringComparison.Ordinal))
                {
                    notifications.Add($"*Ändrat på gång -> till salu:*\n{listing.Address}\n{FormatListing(listing)}");
                    changed = true;
                }

                existing.Address = listing.Address;
                existing.Info = listing.Info;
                existing.Url = listing.Url;
                existing.Status = listing.Status;
                existing.LastSeen = currentExecution;

                if (changed ||
                    !string.Equals(previousAddress, listing.Address, StringComparison.Ordinal) ||
                    !string.Equals(previousInfo, listing.Info, StringComparison.Ordinal) ||
                    !string.Equals(previousUrl, listing.Url, StringComparison.Ordinal) ||
                    !string.Equals(previousStatus, listing.Status, StringComparison.Ordinal))
                {
                    existing.LastChanged = currentExecution;
                }
            }

            var scrapedIds = scrapedListings.Select(x => x.BoneoId).ToHashSet(StringComparer.Ordinal);
            foreach (var existing in existingListings.Values)
            {
                if (scrapedIds.Contains(existing.ExternalId) || string.Equals(existing.Status, SoldStatus, StringComparison.Ordinal))
                    continue;

                existing.Status = SoldStatus;
                existing.LastChanged = currentExecution;
                notifications.Add($"*Annons borttagen / sålt:*\n{existing.Address}\n{existing.Url}");
            }

            await context.SaveChangesAsync(cancellationToken);

            foreach (string message in notifications)
                await notificationService.SendToSlack(NotificationChannel, message, cancellationToken);

            logger.LogInformation("Schedule.FetchBoneoListings :: done, processed {count} listings and sent {notifications} notifications", scrapedListings.Count, notifications.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Schedule.FetchBoneoListings :: error while fetching listings");
        }
    }

    private static bool ShouldRunToday(DateTime currentExecution, DateTime? lastExecution)
    {
        if (!lastExecution.HasValue)
            return true;

        if (currentExecution.Hour != RunHour)
            return false;

        return lastExecution.Value.Date != currentExecution.Date || lastExecution.Value.Hour != RunHour;
    }

    private static async Task<List<BoneoListing>> FetchListingsAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await Http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var listings = new List<BoneoListing>();

        // Each listing row has data-cy="property-row" and class containing "list-no-{id}"
        var rows = doc.DocumentNode.SelectNodes("//div[@data-cy='property-row' and contains(@class,'list-no-')]");
        if (rows is null)
            return listings;

        foreach (var row in rows)
        {
            // Extract ID from class like "property-row list-no-3621595"
            var rowClass = row.GetAttributeValue("class", string.Empty);
            var boneoId = ExtractBoneoId(rowClass);
            if (string.IsNullOrWhiteSpace(boneoId))
                continue;

            // The main anchor holds the URL
            var anchor = row.SelectSingleNode(".//a[contains(@class,'list-outer-main')]");
            if (anchor is null)
                continue;

            var href = anchor.GetAttributeValue("href", string.Empty);
            var listingUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? href
                : $"https://www.boneo.se{href}";

            // Address: <h2 class="propertyTitle">
            var titleNode = row.SelectSingleNode(".//h2[@class='propertyTitle']");
            var streetAddress = titleNode is not null
                ? HtmlEntity.DeEntitize(titleNode.InnerText).Trim()
                : "Okänd adress";

            // Area: <span class="addressInfo">
            var areaNode = row.SelectSingleNode(".//span[@class='addressInfo']");
            var area = areaNode is not null
                ? HtmlEntity.DeEntitize(areaNode.InnerText).Trim()
                : string.Empty;

            var address = string.IsNullOrWhiteSpace(area)
                ? streetAddress
                : $"{streetAddress}, {area}";

            // Price: <span class="property-price">
            var priceNode = row.SelectSingleNode(".//span[@class='property-price ']")
                         ?? row.SelectSingleNode(".//span[contains(@class,'property-price')]");
            var price = priceNode is not null
                ? HtmlEntity.DeEntitize(priceNode.InnerText).Trim()
                : string.Empty;

            // Info: <div class="object-other-detail"> contains spans like "Villa", "6 rok", "166 + 9 m²", "1 308 m² tomt"
            var infoNode = row.SelectSingleNode(".//div[@class='object-other-detail ']")
                        ?? row.SelectSingleNode(".//div[contains(@class,'object-other-detail')]");
            var info = string.Empty;
            if (infoNode is not null)
            {
                var parts = infoNode.SelectNodes(".//span")?
                    .Select(s => HtmlEntity.DeEntitize(s.InnerText).Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
                if (parts is not null)
                    info = string.Join(" · ", parts);
            }

            // Status: check for "På G!" badge
            var status = ExtractStatus(row);

            listings.Add(new BoneoListing
            {
                BoneoId = boneoId,
                Address = address,
                Price = price,
                Info = info,
                Url = listingUrl,
                Status = status
            });
        }

        return listings;
    }

    private static string ExtractBoneoId(string cssClass)
    {
        // class="property-row list-no-3621595"
        const string prefix = "list-no-";
        var idx = cssClass.IndexOf(prefix, StringComparison.Ordinal);
        if (idx < 0) return string.Empty;

        var start = idx + prefix.Length;
        var end = cssClass.IndexOf(' ', start);
        return end < 0
            ? cssClass[start..]
            : cssClass[start..end];
    }

    private static string ExtractStatus(HtmlNode row)
    {
        // Badge nodes use class "status-badge kommande" for På G
        var badge = row.SelectSingleNode(".//*[contains(@class,'status-badge')]");
        if (badge is not null)
        {
            var badgeText = HtmlEntity.DeEntitize(badge.InnerText).Trim();
            if (badgeText.Contains(PendingLabel, StringComparison.OrdinalIgnoreCase))
                return PendingStatus;
        }

        return ForSaleStatus;
    }

    private static string FormatListing(BoneoListing listing)
    {
        var parts = new List<string> { listing.Price };
        if (!string.IsNullOrWhiteSpace(listing.Info))
            parts.Add(listing.Info);
        parts.Add($"Status: {(listing.Status == "ForSale" ? "Till salu" : "På gång")}");
        parts.Add(listing.Url);
        return string.Join("\n", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            UseCookies = true,
            CookieContainer = new CookieContainer()
        });

        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("sv-SE,sv;q=0.9,en;q=0.8");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Add("upgrade-insecure-requests", "1");
        client.DefaultRequestHeaders.Add("sec-fetch-dest", "document");
        client.DefaultRequestHeaders.Add("sec-fetch-mode", "navigate");
        client.DefaultRequestHeaders.Add("sec-fetch-site", "none");
        client.DefaultRequestHeaders.Add("sec-fetch-user", "?1");

        return client;
    }

    private sealed class BoneoListing
    {
        public string BoneoId { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string Price { get; set; } = string.Empty;
        public string Info { get; set; } = string.Empty;
        public string Url { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
}
