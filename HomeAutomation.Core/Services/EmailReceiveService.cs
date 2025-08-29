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
            var message = await MimeMessage.LoadAsync(stream, cancellationToken);
            logger.LogInformation("Mail.Receive :: subject:{subject}, sender:{from}", message.Subject, message.From);

            bool saved = false;
            if (message.Subject.Equals("motion", StringComparison.OrdinalIgnoreCase))
            {
                string? sourceId = message.From.FirstOrDefault()?.ToString();
                if (sourceId is not null)
                {
                    sourceId = sourceId[..sourceId.IndexOf('@')];

                    var deviceService = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();

                    var device = await deviceService.Get(DeviceSource.ONVIF, sourceId, cancellationToken);
                    if (device != null)
                    {
                        await scope.ServiceProvider.GetRequiredService<ITriggerService>().FireTriggersFromDevice(device, DeviceEvent.Motion, cancellationToken);
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
        MailMessageEntity mailMessage = new()
        {
            DeviceId = deviceId,
            MessageId = messageId,
            EmlData = emlData
        };

        var repository = scope.ServiceProvider.GetRequiredService<IRepository<MailMessageEntity>>();
        await repository.AddAndSave(mailMessage, cancellationToken);
    }
}
