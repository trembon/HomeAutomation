using HomeAutomation.Core.Services;
using HomeAutomation.Database.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace HomeAutomation.API;

[ApiController]
[Route("api/sensors")]
public class SensorController(IDeviceRepository deviceRepository, ISensorValueService sensorValueService) : Controller
{
    [HttpGet("current")]
    public async Task<IActionResult> Current([FromQuery] string? deviceIds, CancellationToken cancellationToken)
    {
        if (!TryParseDeviceIds(deviceIds, out int[] requestedDeviceIds))
            return BadRequest(new { message = "deviceIds must be a comma-separated list of positive integers." });

        List<HomeAutomation.Database.Entities.DeviceEntity> sensors = await deviceRepository
            .GetSensorsByIds(requestedDeviceIds, cancellationToken);
        HashSet<int> foundDeviceIds = [.. sensors.Select(sensor => sensor.Id)];
        int[] missingDeviceIds = [.. requestedDeviceIds.Where(deviceId => !foundDeviceIds.Contains(deviceId))];
        if (missingDeviceIds.Length > 0)
        {
            return NotFound(new
            {
                message = "One or more device IDs are unknown or are not sensors.",
                deviceIds = missingDeviceIds
            });
        }

        return Ok(sensorValueService.GetCurrentValues(requestedDeviceIds));
    }

    private static bool TryParseDeviceIds(string? deviceIds, out int[] parsedDeviceIds)
    {
        parsedDeviceIds = [];
        if (string.IsNullOrWhiteSpace(deviceIds))
            return false;

        List<int> values = [];
        foreach (string part in deviceIds.Split(','))
        {
            if (!int.TryParse(part.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int deviceId) || deviceId <= 0)
                return false;

            values.Add(deviceId);
        }

        parsedDeviceIds = [.. values.Distinct()];
        return true;
    }
}
