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
    private static readonly Lock duplicationRequestLock = new();
    private static readonly List<DuplicateRecord> duplicationRequestLog = [];

    private readonly ILogger<TelldusController> logger;

    private readonly ISensorValueService sensorValueService;
    private readonly IDeviceService deviceService;
    private readonly ITelldusAPIService telldusAPIService;
    private readonly ITriggerService triggerService;
    private readonly IConfiguration configuration;

    public TelldusController(ISensorValueService sensorValueService, IDeviceService deviceService, ITelldusAPIService telldusAPIService, ITriggerService triggerService, IConfiguration configuration, ILogger<TelldusController> logger)
    {
        this.logger = logger;

        this.sensorValueService = sensorValueService;
        this.deviceService = deviceService;
        this.telldusAPIService = telldusAPIService;
        this.triggerService = triggerService;
        this.configuration = configuration;
    }

    [HttpPost("sensorupdates")]
    public async Task<ActionResult<bool>> TelldusSensorUpdate(SensorUpdatesModel model, CancellationToken cancellationToken)
    {
        lock (duplicationRequestLock)
        {
            if (IsDuplicateRequest($"{model.SensorID}|{model.Type}|{model.Value}"))
                return Ok(false);
        }

        telldusAPIService.SendLogMessage($"DEVICE {model?.SensorID}: {model?.Type.ToString()} - {model?.Value}", model?.Timestamp ?? DateTime.Now);

        var sensor = await deviceService.GetDevice(DeviceSource.Telldus, model?.SensorID.ToString() ?? string.Empty, cancellationToken);
        if (model is not null && sensor is not null)
        {
            logger.LogInformation("Received sensor update from device '{sensor}'.", sensor);
            await sensorValueService.AddValue(sensor.Id, model.Type, model.Value, model.Timestamp, cancellationToken);
        }
        else
        {
            logger.LogInformation("Received sensor update from telldus sensor with ID '{sensorId}'.", model?.SensorID);
        }

        return Ok(true);
    }

    [HttpPost("deviceevents")]
    public async Task<ActionResult<bool>> TelldusDeviceEvents(DeviceEventsModel model, CancellationToken cancellationToken)
    {
        lock (duplicationRequestLock)
        {
            if (IsDuplicateRequest(model.DeviceID, model.Command, model.Parameter))
                return Ok(false);
        }

        telldusAPIService.SendLogMessage($"DEVICEID {model?.DeviceID}: {model?.Command.ToString()} ({model?.Parameter})");

        var device = await deviceService.GetDevice(DeviceSource.Telldus, model?.DeviceID.ToString() ?? string.Empty, cancellationToken);
        if (model is not null && device is not null)
        {
            var state = telldusAPIService.ConvertCommandToEvent(model.Command);

            logger.LogInformation($"Telldus.DeviceEvent :: {device.Id} :: DeviceId:{model?.DeviceID}, Command:{model?.Command.ToString()}, Parameter:{model?.Parameter}, MappedState:{state}");
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
