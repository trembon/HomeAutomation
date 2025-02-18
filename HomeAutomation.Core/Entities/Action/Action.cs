using HomeAutomation.Base.Converters;
using HomeAutomation.Core.Entities;
using HomeAutomation.Entities.Conditions;
using Newtonsoft.Json;

namespace HomeAutomation.Entities.Action;

public abstract class Action : IEntity
{
    public int ID { get; set; }

    public string UniqueID => $"{nameof(Action)}_{ID}";

    public bool Disabled { get; set; }

    public int[] Devices { get; set; }

    [JsonProperty(ItemConverterType = typeof(BaseTypeConverter<Condition>))]
    public Condition[] Conditions { get; set; }

    public abstract Task Execute(IActionExecutionArguments arguments);

    public virtual string ToSourceString()
    {
        return null;
    }
}
