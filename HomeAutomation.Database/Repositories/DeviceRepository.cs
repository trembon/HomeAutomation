using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeAutomation.Database.Repositories;

public interface IDeviceRepository : IRepository<DeviceEntity>
{
    Task Add(string name, DeviceSource source, string sourceId, DeviceKind deviceKind, CancellationToken cancellationToken);

    Task<DeviceEntity?> Get(DeviceSource source, string? sourceId, CancellationToken cancellationToken);

    Task<List<DeviceEntity>> GetForAction(int actionId, CancellationToken cancellationToken);

    Task<List<DeviceEntity>> GetOfKind(DeviceKind kind, CancellationToken cancellationToken);

    Task<List<DeviceEntity>> GetOfSource(DeviceSource source, CancellationToken cancellationToken);
}

public class DeviceRepository(DefaultContext context) : Repository<DeviceEntity>(context), IDeviceRepository
{
    public async Task Add(string name, DeviceSource source, string sourceId, DeviceKind deviceKind, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        ArgumentNullException.ThrowIfNull(sourceId, nameof(sourceId));

        await AddAndSave(new DeviceEntity
        {
            Name = name,
            Source = source,
            SourceId = sourceId,
            Kind = deviceKind,
            Disabled = false
        }, cancellationToken);
    }

    public async Task<DeviceEntity?> Get(DeviceSource source, string? sourceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return null;

        return await Table
            .Where(x => x.Source == source && x.SourceId == sourceId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }
    public async Task<List<DeviceEntity>> GetForAction(int actionId, CancellationToken cancellationToken)
    {
        return await Context
            .ActionDevices
            .Where(x => x.ActionId == actionId)
            .Select(x => x.Device)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DeviceEntity>> GetOfKind(DeviceKind kind, CancellationToken cancellationToken)
    {
        return await Table
            .Where(x => x.Kind == kind)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DeviceEntity>> GetOfSource(DeviceSource source, CancellationToken cancellationToken)
    {
        return await Table
            .Where(x => x.Source == source)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
