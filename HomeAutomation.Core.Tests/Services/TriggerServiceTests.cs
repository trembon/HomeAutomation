using HomeAutomation.Core.Services;
using HomeAutomation.Core.Tests.TestSupport;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Interfaces;
using HomeAutomation.Database.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace HomeAutomation.Core.Tests.Services;

public class TriggerServiceTests
{
    [Fact]
    public async Task FireTriggersFromDevice_WithUnknownEvent_DoesNothing()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        IActionExecutionService actionService = Substitute.For<IActionExecutionService>();
        IEvaluateConditionService conditionService = PassingConditionService();
        TriggerService service = CreateService(context, actionService, conditionService);

        await service.FireTriggersFromDevice(Device(), DeviceEvent.Unknown, CancellationToken.None);

        _ = conditionService.DidNotReceiveWithAnyArgs().MeetConditions(default!);
        _ = actionService.DidNotReceiveWithAnyArgs().Execute(default, default, default);
    }

    [Fact]
    public async Task FireTriggersFromDevice_WithMatchingTrigger_ExecutesLinkedAction()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        (DeviceEntity device, TriggerEntity trigger, ActionEntity action) =
            await AddAutomation(context, disabled: false);
        IActionExecutionService actionService = Substitute.For<IActionExecutionService>();
        TriggerService service = CreateService(context, actionService, PassingConditionService());

        await service.FireTriggersFromDevice(device, DeviceEvent.On, CancellationToken.None);

        await actionService.Received(1).Execute(
            action.Id,
            Arg.Is<object>(source => ReferenceEquals(source, device)),
            Arg.Any<CancellationToken>());
        Assert.False(trigger.Disabled);
    }

    [Fact]
    public async Task FireTriggersFromDevice_WhenTriggerIsDisabled_DoesNotExecuteAction()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        (DeviceEntity device, _, _) = await AddAutomation(context, disabled: true);
        IActionExecutionService actionService = Substitute.For<IActionExecutionService>();
        TriggerService service = CreateService(context, actionService, PassingConditionService());

        await service.FireTriggersFromDevice(device, DeviceEvent.On, CancellationToken.None);

        _ = actionService.DidNotReceiveWithAnyArgs().Execute(default, default, default);
    }

    [Fact]
    public async Task FireTriggersFromDevice_WhenConditionFails_DoesNotExecuteAction()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        (DeviceEntity device, _, _) = await AddAutomation(context, disabled: false);
        IActionExecutionService actionService = Substitute.For<IActionExecutionService>();
        IEvaluateConditionService conditionService = Substitute.For<IEvaluateConditionService>();
        conditionService.MeetConditions(Arg.Any<IConditionedEntity>()).Returns((false, "condition"));
        TriggerService service = CreateService(context, actionService, conditionService);

        await service.FireTriggersFromDevice(device, DeviceEvent.On, CancellationToken.None);

        _ = actionService.DidNotReceiveWithAnyArgs().Execute(default, default, default);
    }

    [Fact]
    public async Task FireTriggersFromDevice_WithDifferentEvent_DoesNotExecuteAction()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        (DeviceEntity device, _, _) = await AddAutomation(context, disabled: false);
        IActionExecutionService actionService = Substitute.For<IActionExecutionService>();
        TriggerService service = CreateService(context, actionService, PassingConditionService());

        await service.FireTriggersFromDevice(device, DeviceEvent.Off, CancellationToken.None);

        _ = actionService.DidNotReceiveWithAnyArgs().Execute(default, default, default);
    }

    private static TriggerService CreateService(
        TestDefaultContext context,
        IActionExecutionService actionService,
        IEvaluateConditionService conditionService)
    {
        return new TriggerService(
            new TriggerRepository(context),
            actionService,
            conditionService,
            NullLogger<TriggerService>.Instance);
    }

    private static IEvaluateConditionService PassingConditionService()
    {
        IEvaluateConditionService service = Substitute.For<IEvaluateConditionService>();
        service.MeetConditions(Arg.Any<IConditionedEntity>()).Returns((true, string.Empty));
        return service;
    }

    private static async Task<(DeviceEntity device, TriggerEntity trigger, ActionEntity action)> AddAutomation(
        TestDefaultContext context,
        bool disabled)
    {
        DeviceEntity device = Device();
        TriggerEntity trigger = new()
        {
            Name = "Switch trigger",
            Kind = TriggerKind.DeviceState,
            ListenOnDevice = device,
            ListenOnDeviceEvent = DeviceEvent.On,
            Disabled = disabled
        };
        ActionEntity action = new()
        {
            Name = "Notify",
            Kind = ActionKind.SendMessage
        };
        context.AddRange(device, trigger, action);
        await context.SaveChangesAsync();
        context.TriggerActions.Add(new TriggerActionEntity
        {
            TriggerId = trigger.Id,
            Trigger = trigger,
            ActionId = action.Id,
            Action = action
        });
        await context.SaveChangesAsync();
        return (device, trigger, action);
    }

    private static DeviceEntity Device()
    {
        return new DeviceEntity
        {
            Name = "Switch",
            Source = DeviceSource.Telldus,
            SourceId = "42",
            Kind = DeviceKind.PowerSwitch
        };
    }
}
