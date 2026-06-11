using HomeAutomation.Core.Constants;
using HomeAutomation.Core.ScheduledJobs;
using HomeAutomation.Core.Services;
using HomeAutomation.Database;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Text.Json;

namespace HomeAutomation.Core.Tests.ScheduledJobs;

public class SetBatteryDischargeLevelScheduleJobTests
{
    private static readonly string TemperatureDeviceStateKey = StateKeys.CustomState(StateKeys.BatteryRoomTemperatureDeviceId);
    private static readonly string ModeStateKey = StateKeys.CustomState(StateKeys.BatteryDischargeLevelMode);

    [Fact]
    public async Task Execute_MissingTemperatureDeviceState_SkipsFusionSolarCall()
    {
        using var context = CreateContext();
        var stateStoreService = CreateStateStoreService();
        var fusionSolarService = Substitute.For<IFusionSolarService>();
        var sut = CreateSut(context, stateStoreService, fusionSolarService);

        await sut.Execute(new DateTime(2026, 6, 11, 8, 0, 0), null, CancellationToken.None);

        _ = fusionSolarService.DidNotReceiveWithAnyArgs().SetConfigSignals(default!, default!, default);
    }

    [Fact]
    public async Task Execute_InvalidTemperatureDeviceState_SkipsFusionSolarCall()
    {
        using var context = CreateContext();
        var stateStoreService = CreateStateStoreService();
        stateStoreService.Get(TemperatureDeviceStateKey).Returns("not-a-device-id");
        var fusionSolarService = Substitute.For<IFusionSolarService>();
        var sut = CreateSut(context, stateStoreService, fusionSolarService);

        await sut.Execute(new DateTime(2026, 6, 11, 8, 0, 0), null, CancellationToken.None);

        _ = fusionSolarService.DidNotReceiveWithAnyArgs().SetConfigSignals(default!, default!, default);
    }

    [Fact]
    public async Task Execute_NoMatchingDailySummaries_SkipsFusionSolarCall()
    {
        using var context = CreateContext();
        var stateStoreService = CreateStateStoreService();
        stateStoreService.Get(TemperatureDeviceStateKey).Returns("42");
        var fusionSolarService = Substitute.For<IFusionSolarService>();
        var sut = CreateSut(context, stateStoreService, fusionSolarService);

        await sut.Execute(new DateTime(2026, 6, 11, 8, 0, 0), null, CancellationToken.None);

        _ = fusionSolarService.DidNotReceiveWithAnyArgs().SetConfigSignals(default!, default!, default);
    }

    [Fact]
    public async Task Execute_AverageAboveThreshold_AppliesSummerLevel()
    {
        using var context = CreateContext();
        context.SensorDailySummaries.AddRange(
            Summary(42, new DateOnly(2026, 6, 6), 10m, 1),
            Summary(42, new DateOnly(2026, 6, 7), 12m, 9));
        await context.SaveChangesAsync();

        object? payload = null;
        var stateStoreService = CreateStateStoreService();
        stateStoreService.Get(TemperatureDeviceStateKey).Returns("42");
        var fusionSolarService = CreateFusionSolarService(success: true, value => payload = value);
        var sut = CreateSut(context, stateStoreService, fusionSolarService);

        await sut.Execute(new DateTime(2026, 6, 11, 8, 0, 0), null, CancellationToken.None);

        AssertPayload(payload, "3");
        _ = fusionSolarService.Received(1).SetConfigSignals("battery", Arg.Any<object>(), Arg.Any<CancellationToken>());
        _ = stateStoreService.Received(1).Set(ModeStateKey, "summer", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_AverageAtThreshold_AppliesWinterLevel()
    {
        using var context = CreateContext();
        context.SensorDailySummaries.AddRange(
            Summary(42, new DateOnly(2026, 6, 6), 10m, 1),
            Summary(42, new DateOnly(2026, 6, 7), 11.1m, 10));
        await context.SaveChangesAsync();

        object? payload = null;
        var stateStoreService = CreateStateStoreService();
        stateStoreService.Get(TemperatureDeviceStateKey).Returns("42");
        var fusionSolarService = CreateFusionSolarService(success: true, value => payload = value);
        var sut = CreateSut(context, stateStoreService, fusionSolarService);

        await sut.Execute(new DateTime(2026, 6, 11, 8, 0, 0), null, CancellationToken.None);

        AssertPayload(payload, "10");
        _ = fusionSolarService.Received(1).SetConfigSignals("battery", Arg.Any<object>(), Arg.Any<CancellationToken>());
        _ = stateStoreService.Received(1).Set(ModeStateKey, "winter", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ExistingMatchingMode_SkipsFusionSolarCall()
    {
        using var context = CreateContext();
        context.SensorDailySummaries.Add(Summary(42, new DateOnly(2026, 6, 10), 15m, 5));
        await context.SaveChangesAsync();

        var stateStoreService = CreateStateStoreService();
        stateStoreService.Get(TemperatureDeviceStateKey).Returns("42");
        stateStoreService.Get(ModeStateKey).Returns("summer");
        var fusionSolarService = Substitute.For<IFusionSolarService>();
        var sut = CreateSut(context, stateStoreService, fusionSolarService);

        await sut.Execute(new DateTime(2026, 6, 11, 8, 0, 0), null, CancellationToken.None);

        _ = fusionSolarService.DidNotReceiveWithAnyArgs().SetConfigSignals(default!, default!, default);
    }

    [Fact]
    public async Task Execute_FailedFusionSolarCall_DoesNotUpdateModeState()
    {
        using var context = CreateContext();
        context.SensorDailySummaries.Add(Summary(42, new DateOnly(2026, 6, 10), 15m, 5));
        await context.SaveChangesAsync();

        var stateStoreService = CreateStateStoreService();
        stateStoreService.Get(TemperatureDeviceStateKey).Returns("42");
        var fusionSolarService = CreateFusionSolarService(success: false);
        var sut = CreateSut(context, stateStoreService, fusionSolarService);

        await sut.Execute(new DateTime(2026, 6, 11, 8, 0, 0), null, CancellationToken.None);

        _ = fusionSolarService.Received(1).SetConfigSignals("battery", Arg.Any<object>(), Arg.Any<CancellationToken>());
        _ = stateStoreService.DidNotReceive().Set(ModeStateKey, Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    private static DefaultContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDefaultContext(options);
    }

    private static IStateStoreService CreateStateStoreService()
    {
        var stateStoreService = Substitute.For<IStateStoreService>();
        stateStoreService.Set(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        return stateStoreService;
    }

    private static IFusionSolarService CreateFusionSolarService(bool success, Action<object>? capturePayload = null)
    {
        var fusionSolarService = Substitute.For<IFusionSolarService>();
        fusionSolarService
            .SetConfigSignals(
                Arg.Any<string>(),
                Arg.Do<object>(payload => capturePayload?.Invoke(payload)),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(success));
        return fusionSolarService;
    }

    private static SetBatteryDischargeLevelScheduleJob CreateSut(DefaultContext context, IStateStoreService stateStoreService, IFusionSolarService fusionSolarService)
    {
        return new SetBatteryDischargeLevelScheduleJob(context, stateStoreService, fusionSolarService, Substitute.For<INotificationService>(), NullLogger<SetBatteryDischargeLevelScheduleJob>.Instance);
    }

    private static SensorDailySummaryEntity Summary(int deviceId, DateOnly date, decimal avgValue, int readingCount)
    {
        return new SensorDailySummaryEntity
        {
            DeviceId = deviceId,
            Type = SensorValueKind.Temperature,
            Date = date,
            AvgValue = avgValue,
            ReadingCount = readingCount
        };
    }

    private static void AssertPayload(object? payload, string expectedValue)
    {
        Assert.NotNull(payload);

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(payload));
        var signal = json.RootElement.EnumerateArray().Single();
        Assert.Equal("230320278", signal.GetProperty("id").GetString());
        Assert.Equal(expectedValue, signal.GetProperty("value").GetString());
    }

    private sealed class TestDefaultContext(DbContextOptions<DefaultContext> options) : DefaultContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ActionEntity>().Ignore(x => x.DeviceEventProperties);
        }
    }
}
