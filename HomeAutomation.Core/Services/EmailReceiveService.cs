using HomeAutomation.Database;
using HomeAutomation.Database.Entities;
using HomeAutomation.Entities.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using System.Buffers;

namespace HomeAutomation.Core.Services;

public class EmailReceiveService : MessageStore
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<EmailReceiveService> logger;

    public EmailReceiveService(IServiceScopeFactory serviceScopeFactory, ILogger<EmailReceiveService> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    public override async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
    {
        logger.LogInformation("Mail.Receive :: begin");

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            await using var stream = new MemoryStream();

            var position = buffer.GetPosition(0);
            while (buffer.TryGet(ref position, out var memory))
                await stream.WriteAsync(memory, cancellationToken);

            stream.Position = 0;
            var message = await MimeMessage.LoadAsync(stream, cancellationToken);
            logger.LogInformation($"Mail.Receive :: subject:{message.Subject}, sender:{message.From}");

            bool saved = false;
            if (message.Subject.Equals("motion", StringComparison.OrdinalIgnoreCase))
            {
                string? sourceId = message.From.FirstOrDefault()?.ToString();
                if (sourceId is not null)
                {
                    sourceId = sourceId[..sourceId.IndexOf('@')];

                    var deviceService = scope.ServiceProvider.GetRequiredService<IDeviceService>();

                    var device = await deviceService.GetDevice(Database.Enums.DeviceSource.ONVIF, sourceId, cancellationToken);
                    if (device != null)
                    {
                        await scope.ServiceProvider.GetRequiredService<ITriggerService>().FireTriggersFromDevice(device, DeviceEvent.Motion);
                        await SaveToEml(scope, message.MessageId, stream.ToArray(), device.Id, cancellationToken);
                        saved = true;
                    }
                }
            }

            if (!saved)
                await SaveToEml(scope, message.MessageId, stream.ToArray(), null, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Mail.Receive :: unknown error");
        }

        logger.LogInformation("Mail.Receive :: done");
        return SmtpResponse.Ok;
    }

    private async Task SaveToEml(IServiceScope scope, string messageId, byte[] emlData, int? deviceId, CancellationToken cancellationToken)
    {
        // TODO: fix to read real device here
        MailMessage mailMessage = new()
        {
            DeviceId = null,
            MessageId = messageId,
            EmlData = emlData
        };

        var context = scope.ServiceProvider.GetRequiredService<DefaultContext>();

        await context.MailMessages.AddAsync(mailMessage, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
