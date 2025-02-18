using HomeAutomation.Database.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HomeAutomation.Database.Extensions;

public static class IServiceCollectionExtensions
{
    public static void AddDefaultDatabaseContext(this IServiceCollection services, string postgressConnectionString)
    {
        services.AddDatabaseContext<DefaultContext>(postgressConnectionString);
    }

    private static void AddDatabaseContext<TContext>(this IServiceCollection services, string postgressConnectionString) where TContext : DbContext
    {
        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name!;
        services.AddDbContext<TContext>(options => options.UseNpgsql(postgressConnectionString, x => x.MigrationsAssembly(assemblyName)), ServiceLifetime.Transient);
    }
}
