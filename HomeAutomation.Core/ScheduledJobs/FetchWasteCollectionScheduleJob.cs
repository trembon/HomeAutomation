using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Database;
using HomeAutomation.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace HomeAutomation.Core.ScheduledJobs;

[ScheduledJob(900, DelayInSeconds = 90)]
public class FetchWasteCollectionScheduleJob(
    DefaultContext context,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<FetchWasteCollectionScheduleJob> logger) : IScheduledJob
{
    private const int DefaultFetchHour = 2;

    public async Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
    {
        string? apiUrl = configuration["WasteCollection:ApiUrl"];
        string? unitId = configuration["WasteCollection:UnitId"];
        if (string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(unitId))
        {
            logger.LogWarning("Schedule.FetchWasteCollection :: skipped, WasteCollection:ApiUrl or WasteCollection:UnitId is not configured");
            return;
        }

        int fetchHour = configuration.GetValue("WasteCollection:FetchHour", DefaultFetchHour);
        if (fetchHour is < 0 or > 23)
        {
            logger.LogWarning("Schedule.FetchWasteCollection :: skipped, WasteCollection:FetchHour must be between 0 and 23");
            return;
        }

        var lastFetchedAt = await context.WasteCollections
            .Where(x => x.ExternalAddressId == ExtractAddressId(apiUrl))
            .MaxAsync(x => (DateTime?)x.LastSeenAt, cancellationToken);

        bool hasFetchedToday = lastFetchedAt?.ToLocalTime().Date == currentExecution.Date;
        if (hasFetchedToday || (lastFetchedAt.HasValue && currentExecution.Hour < fetchHour))
            return;

        logger.LogInformation("Schedule.FetchWasteCollection :: starting");

        using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
        request.Headers.Add("module", configuration.GetValue("WasteCollection:Module", "universal"));
        request.Headers.Add("unit", unitId);

        var client = httpClientFactory.CreateClient(nameof(FetchWasteCollectionScheduleJob));
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<WasteCollectionApiResponse>(cancellationToken);
        if (payload?.Data is null)
            throw new InvalidOperationException("Waste collection API returned no data.");

        var existingRows = await context.WasteCollections
            .Where(x => x.ExternalAddressId == payload.Data.Uuid)
            .ToListAsync(cancellationToken);

        foreach (var existingRow in existingRows)
            existingRow.IsActive = false;

        foreach (var service in payload.Data.Services)
        {
            var existing = existingRows.SingleOrDefault(x =>
                x.Type == service.Type &&
                x.CollectionDate == service.CollectionAt);

            if (existing is null)
            {
                context.WasteCollections.Add(new WasteCollectionEntity
                {
                    ExternalAddressId = payload.Data.Uuid,
                    Address = payload.Data.Address,
                    City = payload.Data.City,
                    PropertyId = payload.Data.PropertyId,
                    Type = service.Type,
                    Description = service.Description,
                    CollectionDate = service.CollectionAt,
                    Precision = service.Precision,
                    FirstSeenAt = currentExecution,
                    LastSeenAt = currentExecution,
                    IsActive = true
                });
                continue;
            }

            existing.Address = payload.Data.Address;
            existing.City = payload.Data.City;
            existing.PropertyId = payload.Data.PropertyId;
            existing.Description = service.Description;
            existing.Precision = service.Precision;
            existing.LastSeenAt = currentExecution;
            existing.IsActive = true;
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Schedule.FetchWasteCollection :: stored {count} active collection(s) for {address}", payload.Data.Services.Count, payload.Data.Address);
    }

    private static string ExtractAddressId(string apiUrl)
    {
        if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var uri))
            return string.Empty;

        return uri.Segments
            .Select(x => x.Trim('/'))
            .LastOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;
    }

    private sealed class WasteCollectionApiResponse
    {
        [JsonPropertyName("data")]
        public WasteCollectionData? Data { get; set; }
    }

    private sealed class WasteCollectionData
    {
        [JsonPropertyName("uuid")]
        public string Uuid { get; set; } = null!;

        [JsonPropertyName("address")]
        public string Address { get; set; } = null!;

        [JsonPropertyName("city")]
        public string City { get; set; } = null!;

        [JsonPropertyName("property_id")]
        public string PropertyId { get; set; } = null!;

        [JsonPropertyName("services")]
        public List<WasteCollectionService> Services { get; set; } = [];
    }

    private sealed class WasteCollectionService
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = null!;

        [JsonPropertyName("type")]
        public string Type { get; set; } = null!;

        [JsonPropertyName("collection_at")]
        public DateOnly CollectionAt { get; set; }

        [JsonPropertyName("precision")]
        public string Precision { get; set; } = null!;
    }
}
