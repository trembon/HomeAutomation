using HomeAutomation.Core.Extensions;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SlackNet.Extensions.DependencyInjection;

namespace HomeAutomation.Core;

public static class Startup
{
    public static void AddSlackClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSlackNet(c => c.UseApiToken(configuration["Slack:Token"]));
    }

    public static void AddRepositories(this IServiceCollection services)
    {
        var assemblies = AppDomain.CurrentDomain.GetLocalAssemblies();

        var repositoryTypes = assemblies
            .GetImplementations<IRepository>()
            .Where(x => !x.IsGenericType)
            .ToList();

        // add all specified repositories
        foreach (var repositoryType in repositoryTypes)
        {
            var interfaceType = repositoryType.GetInterface($"I{repositoryType.Name}", true);
            if (interfaceType is not null)
                services.AddTransient(interfaceType, repositoryType);
        }

        // add the base repository for all entity types
        var entityTypes = assemblies
            .GetImplementations<BaseEntity>()
            .ToList();

        foreach (var entityType in entityTypes)
        {
            Type interfaceType = typeof(IRepository<>).MakeGenericType(entityType);
            Type implementationType = typeof(Repository<>).MakeGenericType(entityType);
            services.AddTransient(interfaceType, implementationType);
        }
    }
}
