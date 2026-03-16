namespace HomeAutomation.Core.ScheduledJobs.Base;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ScheduledJobAttribute(int intervalInSeconds) : Attribute
{
    public int IntervalInSeconds { get; } = intervalInSeconds;
}
