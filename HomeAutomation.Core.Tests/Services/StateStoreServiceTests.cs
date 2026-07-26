using HomeAutomation.Core.Services;
using HomeAutomation.Core.Tests.TestSupport;
using HomeAutomation.Database;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace HomeAutomation.Core.Tests.Services;

public class StateStoreServiceTests
{
    [Fact]
    public async Task EnsureInitialized_LoadsPersistedValues()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        context.StateValues.Add(new StateValueEntity
        {
            Key = "custom:mode",
            Value = "Home",
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        using ServiceProvider provider = CreateProvider(context);
        StateStoreService service = CreateService(provider);

        await service.EnsureInitialized(CancellationToken.None);

        Assert.Equal("Home", service.Get("CUSTOM:MODE"));
    }

    [Fact]
    public async Task Set_NewValue_PersistsAndCachesValue()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        using ServiceProvider provider = CreateProvider(context);
        StateStoreService service = CreateService(provider);

        await service.Set("custom:mode", "Away", null, CancellationToken.None);

        Assert.Equal("Away", service.Get("custom:mode"));
        StateValueEntity persisted = await context.StateValues.SingleAsync();
        Assert.Equal("Away", persisted.Value);
    }

    [Fact]
    public async Task Set_WithDifferentKeyCasing_UpdatesExistingRow()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        using ServiceProvider provider = CreateProvider(context);
        StateStoreService service = CreateService(provider);
        await service.Set("custom:mode", "Home", null, CancellationToken.None);

        await service.Set("CUSTOM:MODE", "Away", null, CancellationToken.None);

        Assert.Equal(1, await context.StateValues.CountAsync());
        Assert.Equal("Away", (await context.StateValues.SingleAsync()).Value);
    }

    [Fact]
    public async Task Delete_WithDifferentKeyCasing_RemovesCacheAndPersistedRow()
    {
        await using TestDefaultContext context = TestContextFactory.Create();
        using ServiceProvider provider = CreateProvider(context);
        StateStoreService service = CreateService(provider);
        await service.Set("custom:mode", "Home", null, CancellationToken.None);

        await service.Delete("CUSTOM:MODE", CancellationToken.None);

        Assert.Null(service.Get("custom:mode"));
        Assert.Empty(await context.StateValues.ToListAsync());
    }

    private static ServiceProvider CreateProvider(DefaultContext context)
    {
        ServiceCollection services = new();
        services.AddSingleton(context);
        services.AddScoped<IStateValueRepository, StateValueRepository>();
        return services.BuildServiceProvider();
    }

    private static StateStoreService CreateService(ServiceProvider provider)
    {
        return new StateStoreService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<StateStoreService>.Instance);
    }
}
