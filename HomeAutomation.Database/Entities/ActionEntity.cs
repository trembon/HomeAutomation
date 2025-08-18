using HomeAutomation.Database.Enums;
using HomeAutomation.Database.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeAutomation.Database.Entities;

public class ActionEntity : BaseEntity, IConditionedEntity
{
    public string Name { get; set; } = null!;

    public ActionKind Kind { get; set; }

    public bool Disabled { get; set; } = false;

    public DeviceEvent? DeviceEventToSend { get; set; }

    public Dictionary<string, string>? DeviceEventProperties { get; set; }

    public string? MessageChannel { get; set; }

    public string? MessageToSend { get; set; }

    public List<ActionDeviceEntity> Devices { get; set; } = [];

    public List<TriggerActionEntity> Triggers { get; set; } = [];

    public List<ConditionEntity> Conditions { get; set; } = [];
}

public class ActionConfiguration : IEntityTypeConfiguration<ActionEntity>
{
    public void Configure(EntityTypeBuilder<ActionEntity> builder)
    {
        builder.Property(e => e.DeviceEventProperties).HasColumnType("jsonb");
    }
}
