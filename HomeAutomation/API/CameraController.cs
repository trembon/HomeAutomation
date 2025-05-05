using HomeAutomation.Database.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HomeAutomation.API;

[ApiController]
[Route("api/camera")]
public class CameraController(IDeviceRepository deviceRepository, IHttpClientFactory httpClientFactory) : Controller
{
    [Route("capture/{deviceId}/{size}")]
    public async Task<IActionResult> Index(int deviceId, string size, CancellationToken cancellationToken)
    {
        var camera = await deviceRepository.Get(deviceId, cancellationToken);
        if (camera == null)
            return NotFound();

        string? captureUrl = size == "thumbnail" ? camera.ThumbnailUrl : camera.Url;
        try
        {
            var client = httpClientFactory.CreateClient("cameras");
            var stream = await client.GetStreamAsync(captureUrl, cancellationToken);
            return File(stream, "image/png");
        }
        catch
        {
            return NotFound();
        }
    }
}
