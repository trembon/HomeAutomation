namespace HomeAutomation.Core.ScheduledJobs.Base;

public class ScheduledJobConfiguration
{
    public bool Enabled { get; init; } = true;
    public List<string> EnabledJobs { get; init; } = [];
}
