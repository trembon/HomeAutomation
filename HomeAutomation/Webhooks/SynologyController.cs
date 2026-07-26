using HomeAutomation.Core.Services;
using HomeAutomation.Webhooks.Models.Synology;
using Microsoft.AspNetCore.Mvc;

namespace HomeAutomation.Webhooks;

[ApiController]
[Route("webhooks/synology")]
public class SynologyController(
    ISynologyNotificationService synologyNotificationService,
    ILogger<SynologyController> logger) : ControllerBase
{
    [HttpPost("notification")]
    public async Task<ActionResult> Notification(NotificationModel model, CancellationToken cancellationToken)
    {
        var remoteIpAddress = HttpContext.Connection.RemoteIpAddress;
        if (!synologyNotificationService.IsSourceAllowed(remoteIpAddress))
        {
            logger.LogWarning("Synology.Notification :: Rejected request from {ipAddress}", remoteIpAddress?.ToString() ?? "unknown");
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (string.IsNullOrWhiteSpace(model.Text))
            return BadRequest();

        bool sent = await synologyNotificationService.ForwardToSlack(model.Text, cancellationToken);
        return sent
            ? Ok()
            : StatusCode(StatusCodes.Status502BadGateway);
    }
}
