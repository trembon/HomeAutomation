using HomeAutomation.Database;
using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeAutomation.Core.Services;

public interface IDeviceService
{
    Task<Device?> GetDevice(int id, CancellationToken cancellationToken);

    Task<Device?> GetDevice(DeviceSource source, string sourceId, CancellationToken cancellationToken);
}

public class DeviceService(DefaultContext context) : IDeviceService
{
    public async Task<Device?> GetDevice(int id, CancellationToken cancellationToken)
    {
        return await context
            .Devices
            .Where(x => x.Id == id)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Device?> GetDevice(DeviceSource source, string sourceId, CancellationToken cancellationToken)
    {
        return await context
            .Devices
            .Where(x => x.Source == source && x.SourceId == sourceId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }
}
