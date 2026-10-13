using HomeAutomation.Core.Constants;
using HomeAutomation.Core.Models;
using HomeAutomation.Core.ScheduledJobs;
using HomeAutomation.Core.Services;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace HomeAutomation.Core.Tests.ScheduledJobs;

public class FetchFusionSolarEnergyFlowScheduleJobTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 10, 13, 10, 0, 0, TimeSpan.FromHours(2));
    private static readonly FusionSolarEnergyFlowModel EnergyFlow = new()
    {
        StationId = "station-123",
        Timestamp = Timestamp,
        SolarGeneration = 2.5m,
        BatteryFlow = -0.35m,
        BatteryChargeLevel = 78m,
        GridFlow = -0.75m,
        HouseConsumption = 1.25m
    };

    [Fact]
    public async Task Execute_PersistsChangedValuesForMappedVirtualDevices()
    {
        IFusionSolarService fusionSolarService = Substitute.For<IFusionSolarService>();
        fusionSolarService.GetEnergyFlow(Arg.Any<CancellationToken>()).Returns(Task.FromResult<FusionSolarEnergyFlowModel?>(EnergyFlow));
        IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
        List<DeviceEntity> devices = CreateDevices();
        deviceRepository.GetOfSource(DeviceSource.FusionSolar, Arg.Any<CancellationToken>()).Returns(Task.FromResult(devices));
        ISensorValueService sensorValueService = Substitute.For<ISensorValueService>();
        IStateStoreService stateStoreService = Substitute.For<IStateStoreService>();
        stateStoreService.Get(Arg.Any<string>()).Returns((string?)null);
        FetchFusionSolarEnergyFlowScheduleJob job = CreateJob(fusionSolarService, deviceRepository, sensorValueService, stateStoreService);

        await job.Execute(Timestamp.DateTime, null, CancellationToken.None);

        await sensorValueService.Received(5).AddValue(
            Arg.Any<int>(),
            Arg.Any<SensorValueKind>(),
            Arg.Any<string>(),
            Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>());
        await sensorValueService.Received(1).AddValue(1, SensorValueKind.EnergyFlow, "2.5", Timestamp.UtcDateTime, CancellationToken.None);
        await sensorValueService.Received(1).AddValue(2, SensorValueKind.EnergyFlow, "-0.35", Timestamp.UtcDateTime, CancellationToken.None);
        await sensorValueService.Received(1).AddValue(2, SensorValueKind.ChargeLevel, "78", Timestamp.UtcDateTime, CancellationToken.None);
        await sensorValueService.Received(1).AddValue(3, SensorValueKind.EnergyFlow, "-0.75", Timestamp.UtcDateTime, CancellationToken.None);
        await sensorValueService.Received(1).AddValue(4, SensorValueKind.EnergyFlow, "1.25", Timestamp.UtcDateTime, CancellationToken.None);
    }

    [Fact]
    public async Task Execute_SuppressesUnchangedValuesButWritesForNewlyMappedDevice()
    {
        IFusionSolarService fusionSolarService = Substitute.For<IFusionSolarService>();
        fusionSolarService.GetEnergyFlow(Arg.Any<CancellationToken>()).Returns(Task.FromResult<FusionSolarEnergyFlowModel?>(EnergyFlow));
        IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
        deviceRepository.GetOfSource(DeviceSource.FusionSolar, Arg.Any<CancellationToken>()).Returns(Task.FromResult<List<DeviceEntity>>(
        [
            CreateDevice(1, "station-123_panels"),
            CreateDevice(2, "station-123_battery"),
            CreateDevice(3, "station-123_grid"),
            CreateDevice(4, "station-123_house")
        ]));
        ISensorValueService sensorValueService = Substitute.For<ISensorValueService>();
        IStateStoreService stateStoreService = Substitute.For<IStateStoreService>();
        stateStoreService.Get(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>() switch
        {
            string key when key == StateKeys.SensorValue(1, SensorValueKind.EnergyFlow) => "2.500",
            string key when key == StateKeys.SensorValue(2, SensorValueKind.EnergyFlow) => "-0.35",
            string key when key == StateKeys.SensorValue(2, SensorValueKind.ChargeLevel) => "78",
            string key when key == StateKeys.SensorValue(3, SensorValueKind.EnergyFlow) => "-0.75",
            _ => null
        });
        FetchFusionSolarEnergyFlowScheduleJob job = CreateJob(fusionSolarService, deviceRepository, sensorValueService, stateStoreService);

        await job.Execute(Timestamp.DateTime, null, CancellationToken.None);

        await sensorValueService.Received(1).AddValue(4, SensorValueKind.EnergyFlow, "1.25", Timestamp.UtcDateTime, CancellationToken.None);
        await sensorValueService.Received(1).AddValue(
            Arg.Any<int>(),
            Arg.Any<SensorValueKind>(),
            Arg.Any<string>(),
            Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_UnmappedAndNonSensorDevicesAreIgnored()
    {
        IFusionSolarService fusionSolarService = Substitute.For<IFusionSolarService>();
        fusionSolarService.GetEnergyFlow(Arg.Any<CancellationToken>()).Returns(Task.FromResult<FusionSolarEnergyFlowModel?>(EnergyFlow));
        IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
        deviceRepository.GetOfSource(DeviceSource.FusionSolar, Arg.Any<CancellationToken>()).Returns(Task.FromResult<List<DeviceEntity>>(
        [
            new DeviceEntity
            {
                Id = 1,
                Source = DeviceSource.FusionSolar,
                SourceId = "station-123_panels",
                Kind = DeviceKind.Lightbulb
            }
        ]));
        ISensorValueService sensorValueService = Substitute.For<ISensorValueService>();
        IStateStoreService stateStoreService = Substitute.For<IStateStoreService>();
        FetchFusionSolarEnergyFlowScheduleJob job = CreateJob(fusionSolarService, deviceRepository, sensorValueService, stateStoreService);

        await job.Execute(Timestamp.DateTime, null, CancellationToken.None);

        _ = sensorValueService.DidNotReceiveWithAnyArgs().AddValue(default, default, default!, default, default);
    }

    [Fact]
    public async Task Execute_InvalidOrFailedEnergyFlowDoesNotSaveAnyReading()
    {
        IFusionSolarService fusionSolarService = Substitute.For<IFusionSolarService>();
        fusionSolarService.GetEnergyFlow(Arg.Any<CancellationToken>()).Returns(Task.FromResult<FusionSolarEnergyFlowModel?>(null));
        IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
        ISensorValueService sensorValueService = Substitute.For<ISensorValueService>();
        IStateStoreService stateStoreService = Substitute.For<IStateStoreService>();
        FetchFusionSolarEnergyFlowScheduleJob job = CreateJob(fusionSolarService, deviceRepository, sensorValueService, stateStoreService);

        await job.Execute(Timestamp.DateTime, null, CancellationToken.None);

        _ = deviceRepository.DidNotReceiveWithAnyArgs().GetOfSource(default, default);
        _ = sensorValueService.DidNotReceiveWithAnyArgs().AddValue(default, default, default!, default, default);
    }

    private static FetchFusionSolarEnergyFlowScheduleJob CreateJob(
        IFusionSolarService fusionSolarService,
        IDeviceRepository deviceRepository,
        ISensorValueService sensorValueService,
        IStateStoreService stateStoreService)
    {
        return new FetchFusionSolarEnergyFlowScheduleJob(
            fusionSolarService,
            deviceRepository,
            sensorValueService,
            stateStoreService,
            NullLogger<FetchFusionSolarEnergyFlowScheduleJob>.Instance);
    }

    private static List<DeviceEntity> CreateDevices()
    {
        return
        [
            CreateDevice(1, "station-123_panels"),
            CreateDevice(2, "station-123_battery"),
            CreateDevice(3, "station-123_grid"),
            CreateDevice(4, "station-123_house")
        ];
    }

    private static DeviceEntity CreateDevice(int id, string sourceId)
    {
        return new DeviceEntity
        {
            Id = id,
            Source = DeviceSource.FusionSolar,
            SourceId = sourceId,
            Name = sourceId,
            Kind = DeviceKind.Sensor
        };
    }
}
