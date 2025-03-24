using Newtonsoft.Json.Linq;
using System.Reflection;

namespace HomeAutomation.Base.Converters;

public class BaseTypeConverter<TBaseType> : JsonCreationConverter<TBaseType>
{
    protected override TBaseType Create(Type objectType, JObject jObject)
    {
        // find all implemented field types
        IEnumerable<Type> fieldTypes = Assembly
            .GetAssembly(typeof(TBaseType))
            .GetTypes()
            .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(TBaseType)));

        string baseTypeName = typeof(TBaseType).Name;

        // check if the type from the json is a valid class
        Type fieldType = fieldTypes.FirstOrDefault(t => t.Name.Equals($"{jObject["type"]}{baseTypeName}", StringComparison.OrdinalIgnoreCase));
        if (fieldType != null)
            return (TBaseType)Activator.CreateInstance(fieldType);

        throw new Exception($"Invalid type ({jObject["type"]}) on field for {baseTypeName}.");
    }
}
