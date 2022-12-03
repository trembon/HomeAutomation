using HomeAutomation.Entities.Devices;
using HomeAutomation.Models.Actions;
using HomeAutomation.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace HomeAutomation.Entities.Action
{
    public class SnapshotAction : MessageAction
    {
        public override async Task Execute(IActionExecutionArguments arguments)
        {
            using (WebClient webClient = new WebClient())
            {
                foreach (var device in arguments.Devices.OfType<CameraDevice>())
                {
                    try
                    {
                        byte[] imageBytes = await webClient.DownloadDataTaskAsync(device.URL);
                        await arguments.GetService<INotificationService>().SendToSlack(Channel, GetProcessedMessage(arguments), imageBytes, $"snapshot_{DateTime.Now.ToString("yyyy-MM-dd HHmm")}.jpg", "jpg");
                    }
                    catch(Exception ex)
                    {
                        arguments.GetService<ILogger<SnapshotAction>>().LogError(ex, $"Failed to take snapshot for device ID {device.ID}.");
                    }
                }
            }
        }
    }
}
