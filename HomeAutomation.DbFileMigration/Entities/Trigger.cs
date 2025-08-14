using HomeAutomation.Base.Converters;
using HomeAutomation.Entities.Conditions;
using Newtonsoft.Json;

namespace HomeAutomation.Entities.Triggers;

public abstract class Trigger : IEntity
{
    public int ID { get; set; }

    public string UniqueID => $"{nameof(Trigger)}_{ID}";

    public bool Disabled { get; set; }

    public int[] Actions { get; set; }

    [JsonProperty(ItemConverterType = typeof(BaseTypeConverter<Condition>))]
    public Condition[] Conditions { get; set; }

    public virtual string ToSourceString()
    {
        return null;
    }
}
