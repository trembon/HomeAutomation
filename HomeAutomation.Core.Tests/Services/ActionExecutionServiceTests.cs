using HomeAutomation.Core.Enums;
using HomeAutomation.Core.Services;
using HomeAutomation.Core.Tests.TestSupport;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Interfaces;
using HomeAutomation.Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace HomeAutomation.Core.Tests.Services;

public class ActionExecutionServiceTests
{
    [Fact]
    public async Task Execute_WhenActionDoesNotExist_DoesNotResolveDevices()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
        IEvaluateConditionService conditionService = Substitute.For<IEvaluateConditionService>();
        ActionExecutionService service = CreateService(context, deviceRepository, conditionService);

        await service.Execute(404, null, CancellationToken.None);

        _ = deviceRepository.DidNotReceiveWithAnyArgs().GetForAction(default, default);
        _ = conditionService.DidNotReceiveWithAnyArgs().MeetConditions(default!);
    }

    [Fact]
    public async Task Execute_WhenActionIsDisabled_DoesNotEvaluateOrDispatch()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        ActionEntity action = await AddAction(context, ActionKind.SendMessage, disabled: true);
        IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
        IEvaluateConditionService conditionService = Substitute.For<IEvaluateConditionService>();
        ActionExecutionService service = CreateService(context, deviceRepository, conditionService);

        await service.Execute(action.Id, null, CancellationToken.None);

        _ = conditionService.DidNotReceiveWithAnyArgs().MeetConditions(default!);
        _ = deviceRepository.DidNotReceiveWithAnyArgs().GetForAction(default, default);
    }

    [Fact]
    public async Task Execute_WhenConditionFails_DoesNotResolveDevices()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        ActionEntity action = await AddAction(context, ActionKind.SendMessage);
        IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
        IEvaluateConditionService conditionService = Substitute.For<IEvaluateConditionService>();
        conditionService.MeetConditions(Arg.Any<IConditionedEntity>()).Returns((false, "blocked"));
        ActionExecutionService service = CreateService(context, deviceRepository, conditionService);

        await service.Execute(action.Id, null, CancellationToken.None);

        _ = deviceRepository.DidNotReceiveWithAnyArgs().GetForAction(default, default);
    }

    [Fact]
    public async Task Execute_SendMessage_ExpandsSourceAndDeviceTokens()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        ActionEntity action = await AddAction(context, ActionKind.SendMessage);
        action.MessageChannel = "events";
        action.MessageToSend = "{source} changed {devices}";
        await context.SaveChangesAsync();

        DeviceEntity device = Device("Kitchen");
        IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
        deviceRepository.GetForAction(action.Id, Arg.Any<CancellationToken>()).Returns([device]);
        IEvaluateConditionService conditionService = PassingConditionService();
        INotificationService notificationService = Substitute.For<INotificationService>();
        notificationService.SendToSlack("events", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        IServiceProvider serviceProvider = ServiceProvider((typeof(INotificationService), notificationService));
        ActionExecutionService service = CreateService(
            context,
            deviceRepository,
            conditionService,
            serviceProvider);

        await service.Execute(action.Id, "Door", CancellationToken.None);

        _ = notificationService.Received(1).SendToSlack(
            "events",
            "Door changed Kitchen",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_DeviceEvent_DispatchesTelldusCommand()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        ActionEntity action = await AddAction(context, ActionKind.DeviceEvent);
        action.DeviceEventToSend = DeviceEvent.On;
        await context.SaveChangesAsync();

        DeviceEntity device = Device("Switch");
        device.Source = DeviceSource.Telldus;
        device.SourceId = "42";
        IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
        deviceRepository.GetForAction(action.Id, Arg.Any<CancellationToken>()).Returns([device]);
        ITelldusAPIService telldusService = Substitute.For<ITelldusAPIService>();
        telldusService.ConvertStateToCommand(DeviceEvent.On).Returns(TelldusDeviceMethods.TurnOn);
        telldusService
            .SendCommand(42, TelldusDeviceMethods.TurnOn, Arg.Any<CancellationToken>())
            .Returns(true);
        ActionExecutionService service = CreateService(
            context,
            deviceRepository,
            PassingConditionService(),
            ServiceProvider((typeof(ITelldusAPIService), telldusService)));

        await service.Execute(action.Id, null, CancellationToken.None);

        _ = telldusService.Received(1).SendCommand(
            42,
            TelldusDeviceMethods.TurnOn,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_DeviceEvent_DispatchesZWaveCommand()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        ActionEntity action = await AddAction(context, ActionKind.DeviceEvent);
        action.DeviceEventToSend = DeviceEvent.On;
        await context.SaveChangesAsync();

        DeviceEntity device = Device("Z-Wave switch");
        device.Source = DeviceSource.ZWave;
        device.SourceId = "8";
        IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
        deviceRepository.GetForAction(action.Id, Arg.Any<CancellationToken>()).Returns([device]);
        IZWaveAPIService zwaveService = Substitute.For<IZWaveAPIService>();
        zwaveService
            .ConvertStateToCommand(DeviceEvent.On, out object? parameter)
            .Returns(call =>
            {
                call[1] = 255;
                return ZWaveCommandClass.Basic;
            });
        zwaveService
            .SendCommand(8, ZWaveCommandClass.Basic, 255, Arg.Any<CancellationToken>())
            .Returns(true);
        ActionExecutionService service = CreateService(
            context,
            deviceRepository,
            PassingConditionService(),
            ServiceProvider((typeof(IZWaveAPIService), zwaveService)));

        await service.Execute(action.Id, null, CancellationToken.None);

        _ = zwaveService.Received(1).SendCommand(
            8,
            ZWaveCommandClass.Basic,
            255,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_DeviceEvent_DispatchesTuyaCommand()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        ActionEntity action = await AddAction(context, ActionKind.DeviceEvent);
        action.DeviceEventToSend = DeviceEvent.On;
        await context.SaveChangesAsync();

        DeviceEntity device = Device("Tuya switch");
        device.Source = DeviceSource.Tuya;
        device.SourceId = "tuya-1";
        Dictionary<int, object> dps = new() { [1] = true };
        IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
        deviceRepository.GetForAction(action.Id, Arg.Any<CancellationToken>()).Returns([device]);
        ITuyaAPIService tuyaService = Substitute.For<ITuyaAPIService>();
        tuyaService
            .ConvertStateToDPS(DeviceEvent.On, device.Kind, Arg.Any<Dictionary<string, string>>())
            .Returns(dps);
        tuyaService
            .SendCommand(device.SourceId, dps, Arg.Any<CancellationToken>())
            .Returns(true);
        ActionExecutionService service = CreateService(
            context,
            deviceRepository,
            PassingConditionService(),
            ServiceProvider((typeof(ITuyaAPIService), tuyaService)));

        await service.Execute(action.Id, null, CancellationToken.None);

        _ = tuyaService.Received(1).SendCommand(
            device.SourceId,
            dps,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_DeviceEvent_DispatchesIkeaCommand()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        ActionEntity action = await AddAction(context, ActionKind.DeviceEvent);
        action.DeviceEventToSend = DeviceEvent.On;
        action.DeviceEventProperties = new Dictionary<string, string> { ["color"] = "#FF0000" };
        await context.SaveChangesAsync();

        DeviceEntity device = Device("IKEA switch");
        device.Source = DeviceSource.IkeaDirigera;
        device.SourceId = "ikea-1";
        device.Kind = DeviceKind.Lightbulb;
        object powerPayload = new[] { new { attributes = new { isOn = true } } };
        object colorPayload = new[] { new { attributes = new { colorHue = 0d, colorSaturation = 1d } } };
        object brightnessPayload = new[] { new { attributes = new { lightLevel = 100 } } };
        object[] payloads = [powerPayload, colorPayload, brightnessPayload];
        IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
        deviceRepository.GetForAction(action.Id, Arg.Any<CancellationToken>()).Returns([device]);
        IIkeaDirigeraService ikeaService = Substitute.For<IIkeaDirigeraService>();
        ikeaService
            .ConvertStateToActions(DeviceEvent.On, device.Kind, Arg.Any<Dictionary<string, string>>())
            .Returns(payloads);
        ikeaService
            .SendAction(device.SourceId, Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(true);
        ActionExecutionService service = CreateService(
            context,
            deviceRepository,
            PassingConditionService(),
            ServiceProvider((typeof(IIkeaDirigeraService), ikeaService)));

        await service.Execute(action.Id, null, CancellationToken.None);

        _ = ikeaService.Received(1).SendAction(
            device.SourceId,
            powerPayload,
            Arg.Any<CancellationToken>());
        _ = ikeaService.Received(1).SendAction(
            device.SourceId,
            colorPayload,
            Arg.Any<CancellationToken>());
        _ = ikeaService.Received(1).SendAction(
            device.SourceId,
            brightnessPayload,
            Arg.Any<CancellationToken>());

        _ = ikeaService.Received(1).ConvertStateToActions(
            DeviceEvent.On,
            device.Kind,
            Arg.Is<Dictionary<string, string>>(properties => properties.Count == 1 && properties["color"] == "#FF0000"));
    }

    [Fact]
    public async Task ExecuteManual_WhenDispatchFails_PropagatesError()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        ActionEntity action = await AddAction(context, ActionKind.DeviceEvent);
        action.DeviceEventToSend = DeviceEvent.On;
        await context.SaveChangesAsync();

        DeviceEntity device = Device("Switch");
        device.Source = DeviceSource.Telldus;
        device.SourceId = "invalid";
        IDeviceRepository deviceRepository = Substitute.For<IDeviceRepository>();
        deviceRepository.GetForAction(action.Id, Arg.Any<CancellationToken>()).Returns([device]);
        ITelldusAPIService telldusService = Substitute.For<ITelldusAPIService>();
        telldusService.ConvertStateToCommand(DeviceEvent.On).Returns(TelldusDeviceMethods.TurnOn);
        ActionExecutionService service = CreateService(
            context,
            deviceRepository,
            PassingConditionService(),
            ServiceProvider((typeof(ITelldusAPIService), telldusService)));

        await Assert.ThrowsAsync<FormatException>(
            () => service.ExecuteManual(action.Id, CancellationToken.None));
    }

    private static ActionExecutionService CreateService(
        TestDefaultContext context,
        IDeviceRepository deviceRepository,
        IEvaluateConditionService conditionService,
        IServiceProvider? serviceProvider = null)
    {
        return new ActionExecutionService(
            context,
            deviceRepository,
            conditionService,
            serviceProvider ?? Substitute.For<IServiceProvider>(),
            NullLogger<ActionExecutionService>.Instance);
    }

    private static IEvaluateConditionService PassingConditionService()
    {
        IEvaluateConditionService service = Substitute.For<IEvaluateConditionService>();
        service.MeetConditions(Arg.Any<IConditionedEntity>()).Returns((true, string.Empty));
        return service;
    }

    private static IServiceProvider ServiceProvider(params (Type type, object service)[] registrations)
    {
        IServiceProvider provider = Substitute.For<IServiceProvider>();
        foreach ((Type type, object service) in registrations)
            provider.GetService(type).Returns(service);

        return provider;
    }

    private static async Task<ActionEntity> AddAction(
        TestDefaultContext context,
        ActionKind kind,
        bool disabled = false)
    {
        ActionEntity action = new()
        {
            Name = "Test action",
            Kind = kind,
            Disabled = disabled
        };
        context.Actions.Add(action);
        await context.SaveChangesAsync();
        return action;
    }

    private static DeviceEntity Device(string name)
    {
        return new DeviceEntity
        {
            Id = 1,
            Name = name,
            Source = DeviceSource.Telldus,
            SourceId = "1",
            Kind = DeviceKind.PowerSwitch
        };
    }
}
