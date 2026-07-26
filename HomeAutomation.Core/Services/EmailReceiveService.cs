using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using System.Buffers;

namespace HomeAutomation.Core.Services;

public class EmailReceiveService(IServiceScopeFactory serviceScopeFactory, ILogger<EmailReceiveService> logger) : MessageStore
{
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
            MimeMessage message = await MimeMessage.LoadAsync(stream, cancellationToken);
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Mail.Receive :: subject:{subject}, sender:{from}", message.Subject, message.From);

            bool saved = false;
            byte[] emlData = stream.ToArray();
            string messageId = message.MessageId ?? Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);
            if (string.Equals(message.Subject, "motion", StringComparison.OrdinalIgnoreCase))
            {
                string? senderAddress = message.From.Mailboxes.FirstOrDefault()?.Address;
                int separatorIndex = senderAddress?.IndexOf('@', StringComparison.Ordinal) ?? -1;
                if (separatorIndex > 0)
                {
                    string sourceId = senderAddress![..separatorIndex];
                    IDeviceRepository deviceService = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                    DeviceEntity? device = await deviceService.GetBySource(DeviceSource.ONVIF, sourceId, cancellationToken);
                    if (device != null)
                    {
                        await scope.ServiceProvider.GetRequiredService<ITriggerService>().FireTriggersFromDevice(device, DeviceEvent.Motion, cancellationToken);
                        await SaveToEml(scope, messageId, emlData, device.Id, cancellationToken);
                        saved = true;
                    }
                }
            }

            if (!saved)
                await SaveToEml(scope, messageId, emlData, null, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Mail.Receive :: unknown error");
        }

        logger.LogInformation("Mail.Receive :: done");
        return SmtpResponse.Ok;
    }

    private static async Task SaveToEml(IServiceScope scope, string messageId, byte[] emlData, int? deviceId, CancellationToken cancellationToken)
    {
        MailMessageEntity mailMessage = new()
        {
            DeviceId = deviceId,
            MessageId = messageId,
            EmlData = emlData
        };

        IRepository<MailMessageEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<MailMessageEntity>>();
        await repository.AddAndSave(mailMessage, cancellationToken);
    }
}
