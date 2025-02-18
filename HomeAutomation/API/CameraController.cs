using HomeAutomation.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomeAutomation.API;

[ApiController]
[Route("api/camera")]
public class CameraController(IJsonDatabaseService jsonDatabaseService, IHttpClientFactory httpClientFactory) : Controller
{
    [Route("capture/{deviceId}/{size}")]
    public async Task<IActionResult> Index(int deviceId, string size)
    {
        var camera = jsonDatabaseService.Cameras.FirstOrDefault(x => x.ID == deviceId);
        if (camera == null)
            return NotFound();

        string captureUrl = size == "thumbnail" ? camera.ThumbnailURL : camera.URL;
        try
        {
            var webClient = httpClientFactory.CreateClient("cameras");
            var stream = await webClient.GetStreamAsync(captureUrl);
            return File(stream, "image/png");
        }
        catch
        {
            return NotFound();
        }
    }
}
