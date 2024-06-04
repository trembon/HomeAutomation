using HomeAutomation.Database.Contexts;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeAutomation.Database.Extensions
{
    public static class IApplicationBuilderExtensions
    {
        public static void ApplyDatabaseMigrations(this IApplicationBuilder host)
        {
            host.ApplyDatabaseMigration<DefaultContext>();
            host.ApplyDatabaseMigration<LogContext>();
        }

        private static void ApplyDatabaseMigration<TContext>(this IApplicationBuilder host) where TContext : DbContext
        {
            using var scope = host.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TContext>();
            context.Database.Migrate();
        }
    }
}
