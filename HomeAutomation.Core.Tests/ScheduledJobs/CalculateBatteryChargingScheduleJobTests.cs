using HomeAutomation.Core.ScheduledJobs;
using HomeAutomation.Core.Services;
using HomeAutomation.Core.Tests.TestSupport;
using HomeAutomation.Database.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Net;
using System.Text.Json;

namespace HomeAutomation.Core.Tests.ScheduledJobs;

public class CalculateBatteryChargingScheduleJobTests
{
    [Theory]
    [InlineData(0, 0, 40, 4)]
    [InlineData(30, 10, 40, 3)]
    [InlineData(30, 20, 40, 1)]
    [InlineData(30, 10, 60, 1)]
    public async Task Execute_SendsRequiredModeAndScheduleInSingleRequest(
        int olderSolarKwh,
        int recentSolarKwh,
        int price,
        int expectedScheduleCount)
    {
        using var context = TestContextFactory.Create();
        DateTime execution = new(2026, 6, 11, 22, 0, 0);
        DateTime tomorrow = execution.Date.AddDays(1);
        EnergyPricingEntity pricing = new()
        {
            Date = DateOnly.FromDateTime(tomorrow),
            AllDefinitive = true,
            PricingData = JsonSerializer.Serialize(Enumerable.Range(0, 96).Select(index => new FetchedPricingRow
            {
                DateTime = tomorrow.AddMinutes(index * 15),
                Value = price,
                Definitive = true
            }))
        };
        context.EnergyPricing.Add(pricing);
        for (int daysAgo = 1; daysAgo <= 7; daysAgo++)
        {
            context.SolarGenerationSummaries.Add(new SolarGenerationSummaryEntity
            {
                Date = DateOnly.FromDateTime(execution.AddDays(-daysAgo)),
                TotalKwh = daysAgo <= 3 ? recentSolarKwh : olderSolarKwh
            });
        }
        await context.SaveChangesAsync();

        StubHttpMessageHandler handler = StubHttpMessageHandler.Respond(HttpStatusCode.OK, "{\"result\":true}");
        using HttpClient client = new(handler);
        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(nameof(FusionSolarService)).Returns(client);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FusionSolar:APIUrl"] = "https://fusionsolar.test/"
            })
            .Build();
        FusionSolarService fusionSolarService = new(configuration, httpClientFactory);
        CalculateBatteryChargingScheduleJob job = new(
            context,
            fusionSolarService,
            Substitute.For<INotificationService>(),
            NullLogger<CalculateBatteryChargingScheduleJob>.Instance);

        await job.Execute(execution, null, CancellationToken.None);

        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Equal("https://fusionsolar.test/set-config-signals?type=dongle", handler.LastRequestUri?.AbsoluteUri);
        using JsonDocument payload = JsonDocument.Parse(handler.LastContent!);
        JsonElement signals = payload.RootElement;
        Assert.Equal(JsonValueKind.Array, signals.ValueKind);
        Assert.Equal(2, signals.GetArrayLength());
        Assert.Equal("230190100", signals[0].GetProperty("id").GetString());
        Assert.Equal("5", signals[0].GetProperty("value").GetString());
        Assert.Equal("230190101", signals[1].GetProperty("id").GetString());

        using JsonDocument schedule = JsonDocument.Parse(signals[1].GetProperty("value").GetString()!);
        Assert.Equal(expectedScheduleCount, schedule.RootElement.GetArrayLength());
        if (expectedScheduleCount == 1)
        {
            JsonElement discharge = schedule.RootElement[0];
            Assert.Equal("00:00", discharge.GetProperty("startTime").GetString());
            Assert.Equal("23:59", discharge.GetProperty("endTime").GetString());
            Assert.Equal(1, discharge.GetProperty("onOff").GetInt32());
        }
        Assert.True(pricing.IsConfigured);
    }
}
