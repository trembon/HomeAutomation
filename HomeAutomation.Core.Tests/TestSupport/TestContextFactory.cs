using HomeAutomation.Database;
using HomeAutomation.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeAutomation.Core.Tests.TestSupport;

internal static class TestContextFactory
{
    public static TestDefaultContext Create()
    {
        DbContextOptions<DefaultContext> options = new DbContextOptionsBuilder<DefaultContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDefaultContext(options);
    }
}

internal sealed class TestDefaultContext(DbContextOptions<DefaultContext> options) : DefaultContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ActionEntity>().Ignore(x => x.DeviceEventProperties);
    }
}
