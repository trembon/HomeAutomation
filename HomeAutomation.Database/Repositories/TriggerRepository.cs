using HomeAutomation.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeAutomation.Database.Repositories;

public interface ITriggerRepository : IRepository<TriggerEntity>
{
    Task<List<TriggerEntity>> GetScheduledTriggers(CancellationToken cancellationToken);

    Task<List<ActionEntity>> GetActionsForTrigger(int triggerId, CancellationToken cancellationToken);
}

public class TriggerRepository(DefaultContext context) : Repository<TriggerEntity>(context), ITriggerRepository
{
    public async Task<List<ActionEntity>> GetActionsForTrigger(int triggerId, CancellationToken cancellationToken)
    {
        return await Context
            .TriggerActions
            .Where(x => x.TriggerId == triggerId)
            .Select(x => x.Action)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TriggerEntity>> GetScheduledTriggers(CancellationToken cancellationToken)
    {
        return await Table
            .Where(x => x.Kind == Enums.TriggerKind.Scheduled)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
