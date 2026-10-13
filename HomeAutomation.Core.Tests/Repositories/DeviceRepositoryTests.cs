using HomeAutomation.Core.Tests.TestSupport;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;

namespace HomeAutomation.Core.Tests.Repositories;

public class DeviceRepositoryTests
{
    [Fact]
    public async Task GetSensorsByIds_ReturnsOnlyRequestedSensors()
    {
        await using var context = TestContextFactory.Create();
        context.Devices.AddRange(
            new DeviceEntity { Id = 12, Name = "Battery", Kind = DeviceKind.Sensor, SourceId = "battery" },
            new DeviceEntity { Id = 13, Name = "Camera", Kind = DeviceKind.Camera, SourceId = "camera" },
            new DeviceEntity { Id = 14, Name = "Panels", Kind = DeviceKind.Sensor, SourceId = "panels" });
        await context.SaveChangesAsync();
        DeviceRepository repository = new(context);

        List<DeviceEntity> result = await repository.GetSensorsByIds([12, 13, 99], CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(12, result[0].Id);
        Assert.All(result, sensor => Assert.Equal(DeviceKind.Sensor, sensor.Kind));
    }
}
