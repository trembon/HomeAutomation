using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace HomeAutomation.Core.Services;

public interface ISynologyNotificationService
{
    bool IsSourceAllowed(IPAddress? remoteIpAddress);

    Task<bool> ForwardToSlack(string message, CancellationToken cancellationToken);
}

public class SynologyNotificationService(
    IConfiguration configuration,
    INotificationService notificationService,
    ILogger<SynologyNotificationService> logger) : ISynologyNotificationService
{
    private const string DefaultSlackChannel = "errors";

    public bool IsSourceAllowed(IPAddress? remoteIpAddress)
    {
        if (remoteIpAddress is null)
            return false;

        var normalizedRemoteIpAddress = Normalize(remoteIpAddress);
        string[] configuredIpAddresses = configuration
            .GetSection("Synology:AllowedIpAddresses")
            .Get<string[]>() ?? [];

        foreach (string configuredIpAddress in configuredIpAddresses)
        {
            if (!IPAddress.TryParse(configuredIpAddress, out var parsedIpAddress))
            {
                logger.LogWarning("Synology.Notification :: Ignoring malformed allowed IP address: {ipAddress}", configuredIpAddress);
                continue;
            }

            if (Normalize(parsedIpAddress).Equals(normalizedRemoteIpAddress))
                return true;
        }

        return false;
    }

    public Task<bool> ForwardToSlack(string message, CancellationToken cancellationToken)
    {
        string channel = configuration.GetValue("Synology:SlackChannel", DefaultSlackChannel);
        if (string.IsNullOrWhiteSpace(channel))
            channel = DefaultSlackChannel;

        string formattedMessage = $":warning: *Synology NAS*\n{message.Trim()}";
        return notificationService.SendToSlack(channel, formattedMessage, cancellationToken);
    }

    private static IPAddress Normalize(IPAddress ipAddress)
    {
        return ipAddress.IsIPv4MappedToIPv6
            ? ipAddress.MapToIPv4()
            : ipAddress;
    }
}
