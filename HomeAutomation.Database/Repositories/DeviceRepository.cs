using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeAutomation.Database.Repositories;

public interface IDeviceRepository : IRepository<DeviceEntity>
{
    Task<DeviceEntity?> GetDevice(DeviceSource source, string? sourceId, CancellationToken cancellationToken);

    Task<List<DeviceEntity>> GetDevicesForAction(int actionId, CancellationToken cancellationToken);

    Task<List<DeviceEntity>> GetDevicesOfKind(DeviceKind kind, CancellationToken cancellationToken);
}

public class DeviceRepository(DefaultContext context) : Repository<DeviceEntity>(context), IDeviceRepository
{
    public async Task<DeviceEntity?> GetDevice(DeviceSource source, string? sourceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return null;

        return await Table
            .Where(x => x.Source == source && x.SourceId == sourceId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }
    public async Task<List<DeviceEntity>> GetDevicesForAction(int actionId, CancellationToken cancellationToken)
    {
        return await Context
            .ActionDevices
            .Where(x => x.ActionId == actionId)
            .Select(x => x.Device)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DeviceEntity>> GetDevicesOfKind(DeviceKind kind, CancellationToken cancellationToken)
    {
        return await Table
            .Where(x => x.Kind == kind)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
