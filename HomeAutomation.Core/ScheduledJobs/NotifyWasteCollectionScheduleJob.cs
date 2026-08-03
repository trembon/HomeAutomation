using HomeAutomation.Core.ScheduledJobs.Base;
using HomeAutomation.Core.Services;
using HomeAutomation.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace HomeAutomation.Core.ScheduledJobs;

[ScheduledJob(300, DelayInSeconds = 120)]
public class NotifyWasteCollectionScheduleJob(
    DefaultContext context,
    INotificationService notificationService,
    IConfiguration configuration,
    ILogger<NotifyWasteCollectionScheduleJob> logger) : IScheduledJob
{
    private const int DefaultNotificationHour = 10;
    private const string DefaultSlackChannel = "event";

    public async Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken cancellationToken)
    {
        int notificationHour = configuration.GetValue("WasteCollection:NotificationHour", DefaultNotificationHour);
        if (notificationHour is < 0 or > 23)
        {
            logger.LogWarning("Schedule.NotifyWasteCollection :: skipped, WasteCollection:NotificationHour must be between 0 and 23");
            return;
        }

        if (currentExecution.Hour < notificationHour)
            return;

        var tomorrow = DateOnly.FromDateTime(currentExecution.AddDays(1));
        var collections = await context.WasteCollections
            .Where(x => x.IsActive
                     && x.CollectionDate == tomorrow
                     && x.NotificationSentAt == null)
            .OrderBy(x => x.Type)
            .ToListAsync(cancellationToken);

        if (collections.Count == 0)
            return;

        string channel = configuration.GetValue("WasteCollection:SlackChannel", DefaultSlackChannel);
        if (string.IsNullOrWhiteSpace(channel))
            channel = DefaultSlackChannel;

        string formattedDate = tomorrow.ToString("dddd d MMMM", CultureInfo.GetCultureInfo("sv-SE"));
        string items = string.Join("\n", collections.Select(x => $"• *{x.Type}* – {x.Description}"));
        string message = $"*Sophämtning i morgon ({formattedDate})*\nStäll fram:\n{items}";

        bool sent = await notificationService.SendToSlack(channel, message, cancellationToken);
        if (!sent)
        {
            logger.LogWarning("Schedule.NotifyWasteCollection :: failed to notify {channel}; notification will be retried", channel);
            return;
        }

        foreach (var collection in collections)
            collection.NotificationSentAt = currentExecution;

        await context.SaveChangesAsync(cancellationToken);
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Schedule.NotifyWasteCollection :: notified {channel} about {count} collection(s)", channel, collections.Count);
    }
}
