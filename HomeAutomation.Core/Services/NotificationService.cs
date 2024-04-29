using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SlackAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace HomeAutomation.Core.Services
{
    public interface INotificationService
    {
        Task<bool> SendToSlack(string channel, string message);

        Task<bool> SendToSlack(string channel, string message, byte[] file, string fileName, string fileType);
    }

    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> logger;

        private readonly IConfiguration configuration;

        public NotificationService(IConfiguration configuration, ILogger<NotificationService> logger)
        {
            this.logger = logger;

            this.configuration = configuration;
        }

        public async Task<bool> SendToSlack(string channel, string message)
        {
            try
            {
                SlackTaskClient slackClient = new SlackTaskClient(configuration["Slack:Token"]);
                var response = await slackClient.PostMessageAsync(channel, message);

                if (response.ok)
                {
                    return true;
                }
                else
                {
                    logger.LogError($"Failed to send notification to slack.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to send notification to slack. ({ex.GetType().Name}: {ex.Message})");
                return false;
            }
        }
        
        public async Task<bool> SendToSlack(string channel, string message, byte[] file, string fileName, string fileType)
        {
            try
            {
                SlackTaskClient slackClient = new SlackTaskClient(configuration["Slack:Token"]);
                var response = await slackClient.UploadFileAsync(file, fileName, new string[] { channel }, initialComment: message, fileType: fileType);

                if (response.ok)
                {
                    return true;
                }
                else
                {
                    logger.LogError($"Failed to send file notification to slack.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to send file notification to slack. ({ex.GetType().Name}: {ex.Message})");
                return false;
            }
        }
    }
}
