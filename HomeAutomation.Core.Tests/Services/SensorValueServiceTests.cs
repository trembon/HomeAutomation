using HomeAutomation.Core.Services;
using HomeAutomation.Core.Tests.TestSupport;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HomeAutomation.Core.Tests.Services;

public class SensorValueServiceTests
{
    [Fact]
    public async Task AddValue_PersistsReadingAndUpdatesCurrentState()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        IStateStoreService stateStore = Substitute.For<IStateStoreService>();
        SensorValueService service = new(
            new Repository<SensorValueEntity>(context),
            stateStore);
        DateTime timestamp = new(2026, 7, 26, 12, 30, 0, DateTimeKind.Utc);

        await service.AddValue(
            7,
            SensorValueKind.Temperature,
            "21.5",
            timestamp,
            CancellationToken.None);

        SensorValueEntity reading = await context.SensorValues.SingleAsync();
        Assert.Equal(7, reading.DeviceId);
        Assert.Equal(SensorValueKind.Temperature, reading.Type);
        Assert.Equal("21.5", reading.Value);
        Assert.Equal(timestamp, reading.Timestamp);
        await stateStore.Received(1).SetSensorValue(
            7,
            SensorValueKind.Temperature,
            "21.5",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddValue_WithoutTimestamp_UsesCurrentUtcTime()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        SensorValueService service = new(
            new Repository<SensorValueEntity>(context),
            Substitute.For<IStateStoreService>());
        DateTime before = DateTime.UtcNow;

        await service.AddValue(
            7,
            SensorValueKind.Humidity,
            "40",
            null,
            CancellationToken.None);

        DateTime after = DateTime.UtcNow;
        SensorValueEntity reading = await context.SensorValues.SingleAsync();
        Assert.InRange(reading.Timestamp, before, after);
    }

    [Fact]
    public async Task GetCurrentValues_ReturnsRequestedReadingsAsStoredAndOmitsMissingValues()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        IStateStoreService stateStore = Substitute.For<IStateStoreService>();
        Dictionary<string, string> values = new()
        {
            ["sensor:7:ChargeLevel"] = "59",
            ["sensor:7:EnergyFlow"] = "-0.412",
            ["sensor:8:EnergyFlow"] = "0"
        };
        stateStore.Get(Arg.Any<string>()).Returns(call => values.GetValueOrDefault(call.Arg<string>()));
        SensorValueService service = new(new Repository<SensorValueEntity>(context), stateStore);

        Dictionary<int, Dictionary<string, string>> result = service.GetCurrentValues([7, 8, 7]);

        Assert.Equal(2, result.Count);
        Assert.Equal("59", result[7][nameof(SensorValueKind.ChargeLevel)]);
        Assert.Equal("-0.412", result[7][nameof(SensorValueKind.EnergyFlow)]);
        Assert.Single(result[8]);
        Assert.Equal("0", result[8][nameof(SensorValueKind.EnergyFlow)]);
        await stateStore.DidNotReceiveWithAnyArgs().SetSensorValue(default, default, default!, default);
    }
}
