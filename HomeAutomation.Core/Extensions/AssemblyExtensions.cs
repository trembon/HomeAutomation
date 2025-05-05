using System.Reflection;

namespace HomeAutomation.Core.Extensions;

public static class AssemblyExtensions
{
    public static IEnumerable<Type> GetImplementations<TType>(this IEnumerable<Assembly> assemblies)
    {
        var type = typeof(TType);

        return assemblies
            .SelectMany(s => s.GetTypes())
            .Where(p => p.IsClass && !p.IsAbstract)
            .Where(p => type.IsAssignableFrom(p));
    }

    public static IEnumerable<Type> GetImplementations<TType>(this Assembly assembly)
    {
        return GetImplementations<TType>([assembly]);
    }
}
