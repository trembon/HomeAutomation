using HomeAutomation.Entities.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

namespace HomeAutomation.Services
{
    public class EmailReceiveService : MessageStore
    {
        private readonly IConfiguration configuration;
        private readonly IJsonDatabaseService jsonDatabaseService;
        private readonly IServiceScopeFactory serviceScopeFactory;

        public EmailReceiveService(IConfiguration configuration, IJsonDatabaseService jsonDatabaseService, IServiceScopeFactory serviceScopeFactory)
        {
            this.configuration = configuration;
            this.jsonDatabaseService = jsonDatabaseService;
            this.serviceScopeFactory = serviceScopeFactory;
        }

        public override async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            await using var stream = new MemoryStream();

            var position = buffer.GetPosition(0);
            while (buffer.TryGet(ref position, out var memory))
                await stream.WriteAsync(memory, cancellationToken);

            stream.Position = 0;
            var message = await MimeMessage.LoadAsync(stream, cancellationToken);

            bool saved = false;
            if(message.Subject.Equals("motion", StringComparison.OrdinalIgnoreCase))
            {
                string sourceId = message.From.FirstOrDefault().ToString();
                sourceId = sourceId.Substring(0, sourceId.IndexOf("@"));

                var device = jsonDatabaseService.Cameras.FirstOrDefault(c => c.SourceID == sourceId);
                if(device != null)
                {
                    using (var scope = serviceScopeFactory.CreateScope())
                    {
                        _ = scope.ServiceProvider.GetService<ITriggerService>().FireTriggersFromDevice(device, DeviceEvent.Motion);
                    }

                    await SaveToEml(message, $"{device.ID}_{device.SourceID}");
                    saved = true;
                }
            }

            if (!saved)
                await SaveToEml(message, "unknown");

            return SmtpResponse.Ok;
        }

        private async Task SaveToEml(MimeMessage message, string path)
        {
            string basePath = configuration["SMTP:Output"];
            if (!string.IsNullOrWhiteSpace(basePath))
            {
                basePath = Path.Combine(basePath, path);
                if (!Directory.Exists(basePath))
                    Directory.CreateDirectory(basePath);

                string file = Path.Combine(basePath, $"{message.MessageId}.eml");
                await message.WriteToAsync(file);
            }
        }
    }
}
