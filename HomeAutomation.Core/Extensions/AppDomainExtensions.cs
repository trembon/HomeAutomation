using System.Reflection;

namespace HomeAutomation.Core.Extensions;

public static class AppDomainExtensions
{
    public static IEnumerable<Assembly> GetLocalAssemblies(this AppDomain domain)
    {
        string? entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
        if (string.IsNullOrWhiteSpace(entryAssemblyName))
            throw new InvalidOperationException("Invalid project setup");

        return domain
            .GetAssemblies()
            .Where(a => a.FullName?.StartsWith(entryAssemblyName) ?? false);
    }
}
