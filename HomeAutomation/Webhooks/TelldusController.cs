using HomeAutomation.Base.Enums;
using HomeAutomation.Core.Services;
using HomeAutomation.Database.Enums;
using HomeAutomation.Webhooks.Models.Telldus;
using Microsoft.AspNetCore.Mvc;

namespace HomeAutomation.Webhooks;

[ApiController]
[Route("webhooks/telldus")]
public class TelldusController : ControllerBase
{
    private static readonly object duplicationRequestLock = new();
    private static readonly List<DuplicateRecord> duplicationRequestLog = [];

    private readonly ILogger<TelldusController> logger;

    private readonly ISensorValueService sensorValueService;
    private readonly IJsonDatabaseService jsonDatabaseService;
    private readonly ITelldusAPIService telldusAPIService;
    private readonly ITriggerService triggerService;
    private readonly IConfiguration configuration;

    public TelldusController(ISensorValueService sensorValueService, IJsonDatabaseService jsonDatabaseService, ITelldusAPIService telldusAPIService, ITriggerService triggerService, IConfiguration configuration, ILogger<TelldusController> logger)
    {
        this.logger = logger;

        this.sensorValueService = sensorValueService;
        this.jsonDatabaseService = jsonDatabaseService;
        this.telldusAPIService = telldusAPIService;
        this.triggerService = triggerService;
        this.configuration = configuration;
    }

    [HttpPost("sensorupdates")]
    public async Task<ActionResult<bool>> TelldusSensorUpdate(SensorUpdatesModel model)
    {
        lock (duplicationRequestLock)
        {
            if (IsDuplicateRequest($"{model.SensorID}|{model.Type}|{model.Value}"))
                return Ok(false);
        }

        telldusAPIService.SendLogMessage($"DEVICE {model?.SensorID}: {model?.Type.ToString()} - {model?.Value}", model?.Timestamp ?? DateTime.Now);

        var sensor = jsonDatabaseService.Sensors.FirstOrDefault(s => s.Source == DeviceSource.Telldus && s.SourceID == model?.SensorID.ToString());
        if (sensor != null)
        {
            logger.LogInformation($"Received sensor update from sensor '{sensor}'.");
        }
        else
        {
            logger.LogInformation($"Received sensor update from telldus sensor with ID '{model?.SensorID}'.");
        }

        try
        {
            await sensorValueService.AddValue(DeviceSource.Telldus, model.SensorID.ToString(), model.Type, model.Value, model.Timestamp);

            return Ok(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to insert sensor value for sensor '{model?.SensorID}' into database.");
            return StatusCode(500);
        }
    }

    [HttpPost("deviceevents")]
    public async Task<ActionResult<bool>> TelldusDeviceEvents(DeviceEventsModel model)
    {
        lock (duplicationRequestLock)
        {
            if (IsDuplicateRequest(model.DeviceID, model.Command, model.Parameter))
                return Ok(false);
        }

        telldusAPIService.SendLogMessage($"DEVICEID {model?.DeviceID}: {model?.Command.ToString()} ({model?.Parameter})");

        var device = jsonDatabaseService.Devices.FirstOrDefault(s => s.Source == DeviceSource.Telldus && s.SourceID == model?.DeviceID.ToString());
        if (device != null)
        {
            var state = telldusAPIService.ConvertCommandToEvent(model.Command);

            logger.LogInformation($"Telldus.DeviceEvent :: {device.ID} :: DeviceId:{model?.DeviceID}, Command:{model?.Command.ToString()}, Parameter:{model?.Parameter}, MappedState:{state}");
            await triggerService.FireTriggersFromDevice(device, state);
        }

        return Ok(true);
    }

    [HttpPost("rawevents")]
    public async Task<ActionResult<bool>> TelldusRawEvents(TelldusRawDeviceEventsModel model)
    {
        lock (duplicationRequestLock)
        {
            if (IsDuplicateRequest(model.RawData))
                return Ok(false);
        }

        telldusAPIService.SendRawLogMessage($"RAW: {model?.RawData} (Controller {model?.ControllerID})");
        return Ok(true);
    }

    private bool IsDuplicateRequest(int deviceId, TelldusDeviceMethods command, string parameter)
    {
        string requestKey = $"{deviceId}|{command}";
        if (command == TelldusDeviceMethods.Dim)
            requestKey = $"{deviceId}|{command}|{parameter}";

        return IsDuplicateRequest(requestKey);
    }

    private bool IsDuplicateRequest(string requestKey)
    {
        DateTime now = DateTime.UtcNow;

        int ignoreDuplicateWebhooksInSeconds = configuration.GetValue("Telldus:IgnoreDuplicateWebhooksInSeconds", 10);
        duplicationRequestLog.RemoveAll(dr => now.AddSeconds(-ignoreDuplicateWebhooksInSeconds) > dr.RequestTime);

        var record = duplicationRequestLog.FirstOrDefault(dr => dr.Request.Equals(requestKey, StringComparison.InvariantCultureIgnoreCase));
        if (record == null)
        {
            duplicationRequestLog.Add(new DuplicateRecord(requestKey, now));
            return false;
        }

        return true;
    }

    private record DuplicateRecord(string Request, DateTime RequestTime);
}
