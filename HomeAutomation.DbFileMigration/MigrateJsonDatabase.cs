using HomeAutomation.Database;
using HomeAutomation.Entities;
using HomeAutomation.Entities.Action;
using HomeAutomation.Entities.Devices;
using HomeAutomation.Entities.Triggers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Text;

namespace HomeAutomation.DbFileMigration;

public class MigrateJsonDatabase
{
    private const string DATABASE_FILE = "database.json";

    public void Migrate(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        string databaseFile = configuration.GetConnectionString("Json") ?? DATABASE_FILE;
        if (!File.Exists(databaseFile))
            return;

        string databaseString = File.ReadAllText(DATABASE_FILE, Encoding.UTF8);
        var memoryEntities = JsonConvert.DeserializeObject<MemoryEntities>(databaseString);

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<DefaultContext>();

        Dictionary<int, int> deviceIdMapping = [];
        foreach (var device in memoryEntities?.Devices ?? [])
        {
            int newId = AddDevice(device, context);
            deviceIdMapping.Add(device.ID, newId);
        }

        Dictionary<int, int> triggerIdMapping = [];
        foreach (var trigger in memoryEntities?.Triggers ?? [])
        {
            int newId = AddTrigger(trigger, deviceIdMapping, context);
            triggerIdMapping.Add(trigger.ID, newId);
        }

        Dictionary<int, int> actionIdMapping = [];
        foreach (var action in memoryEntities?.Actions ?? [])
        {
            int newId = AddAction(action, deviceIdMapping, context);
            if (newId > 0)
                actionIdMapping.Add(action.ID, newId);
        }

        foreach (var trigger in memoryEntities?.Triggers ?? [])
        {
            if (trigger.Actions != null && trigger.Actions.Length > 0)
            {
                foreach (int actionId in trigger.Actions)
                {
                    if (actionIdMapping.TryGetValue(actionId, out int newActionId))
                    {
                        context.TriggerActions.Add(new Database.Entities.TriggerActionEntity
                        {
                            ActionId = newActionId,
                            TriggerId = triggerIdMapping[trigger.ID]
                        });
                    }
                }
            }
        }
        context.SaveChanges();

        try
        {
            File.Delete(databaseFile);
        }
        catch { }
    }

    private int AddAction(Entities.Action.Action action, Dictionary<int, int> deviceIdMapping, DefaultContext context)
    {
        if (action.Devices is null || action.Devices.Length == 0)
            return -1;

        Database.Entities.ActionEntity actionEntity = new()
        {
            Disabled = action.Disabled,
        };

        switch (action)
        {
            case DelayAction: // will not be migrated and not supported in new system
                return -1;

            case SnapshotAction snapshotAction:
                actionEntity.Name = $"{nameof(SnapshotAction)} - {action.ID}";
                actionEntity.Kind = Database.Enums.ActionKind.SendSnapshot;
                actionEntity.MessageToSend = snapshotAction.Message;
                actionEntity.MessageChannel = snapshotAction.Channel;
                break;

            case MessageAction messageAction:
                actionEntity.Name = $"{nameof(MessageAction)} - {action.ID}";
                actionEntity.Kind = Database.Enums.ActionKind.SendMessage;
                actionEntity.MessageToSend = messageAction.Message;
                actionEntity.MessageChannel = messageAction.Channel;
                break;

            case StateAction stateAction:
                actionEntity.Name = $"{nameof(StateAction)} - {action.ID}";
                actionEntity.Kind = Database.Enums.ActionKind.DeviceEvent;
                actionEntity.DeviceEventToSend = ConvertDeviceState(stateAction.State);
                actionEntity.DeviceEventProperties = stateAction.Parameters;
                break;
        }

        context.Actions.Add(actionEntity);
        context.SaveChanges();

        foreach (int device in action.Devices)
        {
            context.ActionDevices.Add(new Database.Entities.ActionDeviceEntity
            {
                ActionId = actionEntity.Id,
                DeviceId = deviceIdMapping[device]
            });
        }
        context.SaveChanges();

        return actionEntity.Id;
    }

    private int AddTrigger(Entities.Triggers.Trigger trigger, Dictionary<int, int> deviceIdMapping, DefaultContext context)
    {
        Database.Entities.TriggerEntity triggerEntity = new()
        {
            Disabled = trigger.Disabled
        };

        switch (trigger)
        {
            case DeviceTrigger deviceTrigger:
                triggerEntity.Kind = Database.Enums.TriggerKind.DeviceState;
                triggerEntity.Name = $"{nameof(DeviceTrigger)} - {trigger.ID}";
                triggerEntity.ListenOnDeviceId = deviceIdMapping[deviceTrigger.Devices.First()];
                triggerEntity.ListenOnDeviceEvent = ConvertDeviceEvent(deviceTrigger.Events.FirstOrDefault());
                break;

            case ScheduleTrigger scheduleTrigger:
                triggerEntity.Kind = Database.Enums.TriggerKind.Scheduled;
                triggerEntity.Name = $"{nameof(ScheduleTrigger)} - {trigger.ID}";
                triggerEntity.SchedulingMode = ConvertScheduleMode(scheduleTrigger.Mode);
                triggerEntity.ScheduledAt = new TimeOnly(scheduleTrigger.At.Hours, scheduleTrigger.At.Minutes, scheduleTrigger.At.Seconds);
                break;
        }

        context.Triggers.Add(triggerEntity);
        context.SaveChanges();

        return triggerEntity.Id;
    }

    private static int AddDevice(Entities.Devices.Device device, DefaultContext context)
    {
        Database.Entities.DeviceEntity deviceEntity = new()
        {
            Name = device.Name,
            Source = device.Source,
            SourceId = device.SourceID
        };

        switch (device)
        {
            case CameraDevice cameraDevice:
                deviceEntity.Kind = Database.Enums.DeviceKind.Camera;
                deviceEntity.ThumbnailUrl = cameraDevice.ThumbnailURL;
                deviceEntity.Url = cameraDevice.URL;
                break;

            case LightbulbDevice:
                deviceEntity.Kind = Database.Enums.DeviceKind.Lightbulb;
                break;

            case MotionSensorDevice:
                deviceEntity.Kind = Database.Enums.DeviceKind.MotionSensor;
                break;

            case PowerSwitchDevice:
                deviceEntity.Kind = Database.Enums.DeviceKind.PowerSwitch;
                break;

            case RemoteDevice:
                deviceEntity.Kind = Database.Enums.DeviceKind.Remote;
                break;

            case SensorDevice:
                deviceEntity.Kind = Database.Enums.DeviceKind.Sensor;
                break;
        }

        context.Devices.Add(deviceEntity);
        context.SaveChanges();

        return deviceEntity.Id;
    }

    private static Database.Enums.DeviceEvent ConvertDeviceEvent(Entities.Enums.DeviceEvent deviceEvent)
    {
        switch (deviceEvent)
        {
            case Entities.Enums.DeviceEvent.Unknown: return Database.Enums.DeviceEvent.Unknown;
            case Entities.Enums.DeviceEvent.On: return Database.Enums.DeviceEvent.On;
            case Entities.Enums.DeviceEvent.Off: return Database.Enums.DeviceEvent.Off;
            case Entities.Enums.DeviceEvent.Motion: return Database.Enums.DeviceEvent.Motion;
            default: return Database.Enums.DeviceEvent.Unknown;
        }
    }
    private static Database.Enums.DeviceEvent ConvertDeviceState(Entities.Enums.DeviceState deviceState)
    {
        switch (deviceState)
        {
            case Entities.Enums.DeviceState.On: return Database.Enums.DeviceEvent.On;
            case Entities.Enums.DeviceState.Off: return Database.Enums.DeviceEvent.Off;
            default: return Database.Enums.DeviceEvent.Off;
        }
    }
    private static Database.Enums.TimeMode ConvertScheduleMode(Entities.Enums.ScheduleMode scheduleMode)
    {
        switch (scheduleMode)
        {
            case Entities.Enums.ScheduleMode.Time: return Database.Enums.TimeMode.Specified;
            case Entities.Enums.ScheduleMode.Sunrise: return Database.Enums.TimeMode.Sunrise;
            case Entities.Enums.ScheduleMode.Sunset: return Database.Enums.TimeMode.Sunset;
            default: return Database.Enums.TimeMode.Specified;
        }
    }
}
