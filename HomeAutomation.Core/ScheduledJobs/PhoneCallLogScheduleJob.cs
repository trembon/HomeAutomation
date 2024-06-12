using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Core.Services;
using HomeAutomation.Database.Contexts;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace HomeAutomation.ScheduledJobs
{
    public class PhoneCallLogScheduleJob(IConfiguration configuration, DefaultContext defaultContext, INotificationService notificationService, ILogger<CleanupLogScheduleJob> logger) : IScheduledJob
    {
        public async Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
        {
            logger.LogInformation("Schedule.PhoneCalls :: starting");

            var calls = await ParsePhoneCalls();
            if (calls is not null)
            {
                var latestCall = defaultContext.PhoneCalls.OrderByDescending(x => x.Timestamp).FirstOrDefault();
                DateTime latestCallTimestamp = latestCall is null ? DateTime.MinValue : latestCall.Timestamp;

                var newCalls = calls.Where(x => x.Timestamp > latestCallTimestamp).ToList();
                foreach (var call in newCalls)
                {
                    defaultContext.PhoneCalls.Add(call);

                    if (call.Type == PhoneCallType.Missed && call.Timestamp > DateTime.Now.AddDays(-7))
                        _ = await notificationService.SendToSlack(configuration["PhoneLog:SlackChannel"] ?? "", $"Missat samtal från {call.Number} ({call.Timestamp.ToString("R")})");
                }

                if (newCalls.Any())
                    await defaultContext.SaveChangesAsync();
            }

            logger.LogInformation("Schedule.PhoneCalls :: done");
        }

        private static string EncodeAuthentication(string username, string password, string key)
        {
            byte[] keyBytes = Encoding.ASCII.GetBytes(key);
            using (HMACSHA1 myhmacsha1 = new(keyBytes))
            {
                byte[] byteArray = Encoding.ASCII.GetBytes(username + password);
                using (MemoryStream stream = new(byteArray))
                {
                    return myhmacsha1.ComputeHash(stream).Aggregate("", (s, e) => s + string.Format("{0:x2}", e), s => s);
                }
            }
        }

        private async Task<List<PhoneCall>> ParsePhoneCalls()
        {
            List<PhoneCall> parsedCalls = new();

            var baseAddress = new Uri($"http://{configuration["PhoneLog:RouterIP"]}/");
            var cookieContainer = new CookieContainer();

            using var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            using var client = new HttpClient(handler) { BaseAddress = baseAddress };

            var preLoad = await client.GetAsync("/");
            string preLoadData = await preLoad.Content.ReadAsStringAsync();
            string authToken = preLoadData.Substring(preLoadData.IndexOf("form.__pass.value,"));
            authToken = authToken.Substring(authToken.IndexOf("\"") + 1);
            authToken = authToken.Substring(0, authToken.IndexOf("\""));

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__formtok", ""),
                new KeyValuePair<string, string>("__user", "admin"),
                new KeyValuePair<string, string>("__auth", "login"),
                new KeyValuePair<string, string>("__hash", EncodeAuthentication(configuration["PhoneLog:Username"], configuration["PhoneLog:Password"], authToken))
            });

            var loginResult = await client.PostAsync("/", content);
            if (loginResult.StatusCode != HttpStatusCode.OK)
            {
                logger.LogError("Unable to contact router to fetch phone call data");
                return null;
            }

            HtmlDocument startpage = new();
            startpage.LoadHtml(await loginResult.Content.ReadAsStringAsync());

            foreach (var phoneLink in startpage.DocumentNode.SelectNodes("//a[contains(@href, '/account/?account=')]"))
            {
                string phoneId = phoneLink.Attributes["href"].Value;
                phoneId = phoneId.Substring(phoneId.IndexOf('=') + 1);

                var dataResult = await client.GetAsync("/account/all_calls/?account=" + phoneId);
                string dataString = await dataResult.Content.ReadAsStringAsync();

                HtmlDocument document = new();
                document.LoadHtml(dataString);

                var table = document.GetElementbyId("content").SelectSingleNode(".//table");
                foreach (var tr in table.SelectNodes(".//tr"))
                {
                    var tds = tr.SelectNodes(".//td");
                    if (tds == null || tds.Count == 0)
                        continue;

                    PhoneCall call = new();
                    call.Timestamp = DateTime.Parse(tds[0].InnerText.Trim(' ', '\n'));
                    call.Number = tds[2].InnerText.Trim(' ', '\n');
                    call.Length = TimeSpan.Parse(tds[3].InnerText.Trim(' ', '\n'));

                    string type = tds[1].InnerText.Trim(' ', '\n');
                    switch (type.ToLowerInvariant())
                    {
                        case "incoming call":
                            call.Type = PhoneCallType.Incoming;
                            break;

                        case "outgoing call":
                            call.Type = PhoneCallType.Outgoing;
                            break;

                        case "missed call":
                        case "missed call read":
                            call.Type = PhoneCallType.Missed;
                            break;

                        default:
                            call.Type = PhoneCallType.Unknown;
                            break;
                    }

                    parsedCalls.Add(call);
                }
            }

            return parsedCalls;
        }
    }
}
