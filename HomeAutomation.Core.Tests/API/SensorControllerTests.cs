using HomeAutomation.API;
using HomeAutomation.Core.Services;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System.Text.Json;

namespace HomeAutomation.Core.Tests.API;

public class SensorControllerTests
{
    private static readonly int[] REQUESTED_DEVICE_IDS = [12, 13];

    [Fact]
    public async Task Current_ReturnsRequestedSensorsAndReadingsWithStableJsonKeys()
    {
        IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
        ISensorValueService sensorValueService = Substitute.For<ISensorValueService>();
        deviceRepository.GetSensorsByIds(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns([new DeviceEntity { Id = 12, Kind = DeviceKind.Sensor }, new DeviceEntity { Id = 13, Kind = DeviceKind.Sensor }]);
        sensorValueService.GetCurrentValues(Arg.Any<IReadOnlyCollection<int>>()).Returns(new Dictionary<int, Dictionary<string, string>>
        {
            [12] = new() { ["ChargeLevel"] = "59", ["EnergyFlow"] = "-0.412" },
            [13] = new() { ["EnergyFlow"] = "0.503" }
        });
        SensorController controller = new(deviceRepository, sensorValueService);

        IActionResult result = await controller.Current("12, 13,12", CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        using JsonDocument json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.Equal("59", json.RootElement.GetProperty("12").GetProperty("ChargeLevel").GetString());
        Assert.Equal("-0.412", json.RootElement.GetProperty("12").GetProperty("EnergyFlow").GetString());
        Assert.Equal("0.503", json.RootElement.GetProperty("13").GetProperty("EnergyFlow").GetString());
        await deviceRepository.Received(1).GetSensorsByIds(
            Arg.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(REQUESTED_DEVICE_IDS)),
            Arg.Any<CancellationToken>());
        _ = sensorValueService.Received(1).GetCurrentValues(Arg.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(REQUESTED_DEVICE_IDS)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12,")]
    [InlineData(",12")]
    [InlineData("12,,13")]
    [InlineData("0")]
    [InlineData("-12")]
    [InlineData("abc")]
    [InlineData("2147483648")]
    public async Task Current_WhenDeviceIdsAreMissingOrInvalid_ReturnsBadRequest(string? deviceIds)
    {
        IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
        ISensorValueService sensorValueService = Substitute.For<ISensorValueService>();
        SensorController controller = new(deviceRepository, sensorValueService);

        IActionResult result = await controller.Current(deviceIds, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        await deviceRepository.DidNotReceiveWithAnyArgs().GetSensorsByIds(default!, default);
        _ = sensorValueService.DidNotReceiveWithAnyArgs().GetCurrentValues(default!);
    }

    [Fact]
    public async Task Current_WhenAnyRequestedIdIsNotASensor_ReturnsNotFoundWithoutReturningValues()
    {
        IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
        ISensorValueService sensorValueService = Substitute.For<ISensorValueService>();
        deviceRepository.GetSensorsByIds(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns([new DeviceEntity { Id = 12, Kind = DeviceKind.Sensor }]);
        SensorController controller = new(deviceRepository, sensorValueService);

        IActionResult result = await controller.Current("12,999", CancellationToken.None);

        NotFoundObjectResult notFound = Assert.IsType<NotFoundObjectResult>(result);
        using JsonDocument json = JsonDocument.Parse(JsonSerializer.Serialize(notFound.Value));
        Assert.Equal(999, json.RootElement.GetProperty("deviceIds")[0].GetInt32());
        _ = sensorValueService.DidNotReceiveWithAnyArgs().GetCurrentValues(default!);
    }

    [Fact]
    public async Task Current_WhenSensorHasNoReadings_ReturnsAnEmptyObjectForThatSensor()
    {
        IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
        ISensorValueService sensorValueService = Substitute.For<ISensorValueService>();
        deviceRepository.GetSensorsByIds(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns([new DeviceEntity { Id = 12, Kind = DeviceKind.Sensor }]);
        sensorValueService.GetCurrentValues(Arg.Any<IReadOnlyCollection<int>>()).Returns(new Dictionary<int, Dictionary<string, string>>
        {
            [12] = []
        });
        SensorController controller = new(deviceRepository, sensorValueService);

        IActionResult result = await controller.Current("12", CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        using JsonDocument json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.Equal(JsonValueKind.Object, json.RootElement.GetProperty("12").ValueKind);
        Assert.Empty(json.RootElement.GetProperty("12").EnumerateObject());
    }
}
