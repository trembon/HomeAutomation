using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace HomeAutomation.Database;

public static class Startup
{
    public static void AddDefaultDatabaseContext(this IServiceCollection services, string connectionString)
    {
        var assembly = Assembly.GetExecutingAssembly();
        services.AddDbContext<DefaultContext>(options => options.UseNpgsql(connectionString, x => x.MigrationsAssembly(assembly)), ServiceLifetime.Transient);
    }

    public static void ApplyDatabaseMigrations(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DefaultContext>();
        context.Database.Migrate();
    }
}
