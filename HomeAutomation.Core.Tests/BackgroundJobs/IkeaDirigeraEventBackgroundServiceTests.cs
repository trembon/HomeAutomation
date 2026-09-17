using HomeAutomation.Core.BackgroundJobs;
using HomeAutomation.Core.Models;
using HomeAutomation.Core.Services;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Reflection;

namespace HomeAutomation.Core.Tests.BackgroundJobs;

public class IkeaDirigeraEventBackgroundServiceTests
{
    [Fact]
    public async Task HandleEvent_WithLightSettings_DoesNotUpdateDeviceStateOrFireTriggers()
    {
        IServiceScopeFactory scopeFactory = Substitute.For<IServiceScopeFactory>();
        IServiceScope scope = Substitute.For<IServiceScope>();
        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
        ITriggerService triggerService = Substitute.For<ITriggerService>();
        IStateStoreService stateStoreService = Substitute.For<IStateStoreService>();

        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(serviceProvider);
        serviceProvider.GetService(typeof(IDeviceRepository)).Returns(deviceRepository);
        serviceProvider.GetService(typeof(ITriggerService)).Returns(triggerService);
        serviceProvider.GetService(typeof(IStateStoreService)).Returns(stateStoreService);

        DeviceEntity device = new()
        {
            Id = 12,
            Kind = DeviceKind.Lightbulb,
            Source = DeviceSource.IkeaDirigera,
            SourceId = "ikea-light-1",
            Name = "IKEA light"
        };
        deviceRepository
            .GetBySource(DeviceSource.IkeaDirigera, device.SourceId, Arg.Any<CancellationToken>())
            .Returns(device);

        IkeaDirigeraEventBackgroundService service = new(
            scopeFactory,
            Substitute.For<IIkeaDirigeraService>(),
            NullLogger<IkeaDirigeraEventBackgroundService>.Instance);

        IkeaDirigeraEventModel eventItem = new()
        {
            DeviceId = device.SourceId,
            Attributes = new Dictionary<string, object>
            {
                ["colorMode"] = "color",
                ["colorHue"] = 250m,
                ["colorSaturation"] = 1m
            }
        };

        await InvokeHandleEvent(service, eventItem);

        _ = stateStoreService.DidNotReceiveWithAnyArgs().SetDeviceState(default, default, default);
        _ = triggerService.DidNotReceiveWithAnyArgs().FireTriggersFromDevice(default!, default, default);
    }

    private static async Task InvokeHandleEvent(IkeaDirigeraEventBackgroundService service, IkeaDirigeraEventModel eventItem)
    {
        MethodInfo method = typeof(IkeaDirigeraEventBackgroundService)
            .GetMethod("HandleEvent", BindingFlags.Instance | BindingFlags.NonPublic)!;

        Task task = (Task)method.Invoke(service, [eventItem, CancellationToken.None])!;
        await task;
    }
}
