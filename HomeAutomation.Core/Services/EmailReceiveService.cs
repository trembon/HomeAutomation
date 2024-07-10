using HomeAutomation.Database;
using HomeAutomation.Database.Contexts;
using HomeAutomation.Database.Entities;
using HomeAutomation.Entities.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HomeAutomation.Core.Services
{
    public class EmailReceiveService : MessageStore
    {
        private readonly IJsonDatabaseService jsonDatabaseService;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<EmailReceiveService> logger;

        public EmailReceiveService(IJsonDatabaseService jsonDatabaseService, IServiceScopeFactory serviceScopeFactory, ILogger<EmailReceiveService> logger)
        {
            this.jsonDatabaseService = jsonDatabaseService;
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
                    string sourceId = message.From.FirstOrDefault().ToString();
                    sourceId = sourceId.Substring(0, sourceId.IndexOf("@"));

                    var device = jsonDatabaseService.Cameras.FirstOrDefault(c => c.SourceID == sourceId);
                    if (device != null)
                    {
                        await scope.ServiceProvider.GetService<ITriggerService>().FireTriggersFromDevice(device, DeviceEvent.Motion);
                        await SaveToEml(scope, message.MessageId, stream.ToArray(), device.Source.ToString(), device.SourceID);
                        saved = true;
                    }
                }

                if (!saved)
                    await SaveToEml(scope, message.MessageId, stream.ToArray(), "raw", "unknown");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Mail.Receive :: unknown error");
            }

            logger.LogInformation("Mail.Receive :: done");
            return SmtpResponse.Ok;
        }

        private async Task SaveToEml(IServiceScope scope, string messageId, byte[] emlData, string deviceSource, string deviceSourceId)
        {
            MailMessage mailMessage = new()
            {
                DeviceSource = deviceSource,
                DeviceSourceID = deviceSourceId,
                MessageID = messageId,
                EmlData = emlData
            };

            var context = scope.ServiceProvider.GetService<LogContext>();

            context.Add(mailMessage);
            await context.SaveChangesAsync();
        }
    }
}
