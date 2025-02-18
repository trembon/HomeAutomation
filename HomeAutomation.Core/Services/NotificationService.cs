using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SlackNet;
using SlackNet.WebApi;

namespace HomeAutomation.Core.Services;

public interface INotificationService
{
    Task<bool> SendToSlack(string channel, string message);

    Task<bool> SendToSlack(string channel, string message, byte[] file, string fileName, string fileType);
}

public class NotificationService(ISlackApiClient slackApiClient, IHttpClientFactory httpClientFactory, ILogger<NotificationService> logger) : INotificationService
{
    public async Task<bool> SendToSlack(string channel, string message)
    {
        try
        {
            var response = await slackApiClient.Chat.PostMessage(new Message
            {
                Channel = channel,
                Text = message,
            });

            if (response is not null)
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
            var channels = await slackApiClient.Conversations.List(true);

            var response = await slackApiClient.Files.GetUploadUrlExternal(fileName, file.Length);
            if (response is not null)
            {
                var client = httpClientFactory.CreateClient(nameof(NotificationService));
                var uploadResult = await client.PostAsync(response.UploadUrl, new ByteArrayContent(file));

                uploadResult.EnsureSuccessStatusCode();

                string? channelId = channels?.Channels?.FirstOrDefault(x => x.Name == channel)?.Id;
                var completeResponse = await slackApiClient.Files.CompleteUploadExternal([new ExternalFileReference { Id = response.FileId, Title = fileName }], channelId, message);
                if (completeResponse.Any())
                    return true;
            }

            logger.LogError($"Failed to send file notification to slack.");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to send file notification to slack. ({ex.GetType().Name}: {ex.Message})");
            return false;
        }
    }
}
