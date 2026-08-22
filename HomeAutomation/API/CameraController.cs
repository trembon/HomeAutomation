using HomeAutomation.Core.Services;
using HomeAutomation.Database.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HomeAutomation.API;

[ApiController]
[Route("api/camera")]
public class CameraController(IDeviceRepository deviceRepository, ICameraSnapshotService cameraSnapshotService) : Controller
{
    [Route("capture/{deviceId}/{size}")]
    public async Task<IActionResult> Index(int deviceId, string size, CancellationToken cancellationToken)
    {
        var camera = await deviceRepository.GetById(deviceId, cancellationToken);
        if (camera == null)
            return NotFound();

        try
        {
            string? captureUrl = size == "thumbnail" ? camera.ThumbnailUrl : camera.Url;
            CameraSnapshotSize snapshotSize = size == "thumbnail" ? CameraSnapshotSize.Thumbnail : CameraSnapshotSize.Full;
            CameraSnapshot snapshot = await cameraSnapshotService.Capture(captureUrl, snapshotSize, cancellationToken);
            return File(snapshot.Content, snapshot.ContentType);
        }
        catch
        {
            return NotFound();
        }
    }
}
