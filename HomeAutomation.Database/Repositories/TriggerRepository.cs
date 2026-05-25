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
        var actions = await Context
            .Actions
            .Include(x => x.Conditions)
            .Where(x => x.Triggers.Any(t => t.TriggerId == triggerId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return actions;
    }

    public async Task<List<TriggerEntity>> GetScheduledTriggers(CancellationToken cancellationToken)
    {
        return await Table
            .Include(x => x.Conditions)
            .Where(x => x.Kind == Enums.TriggerKind.Scheduled)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
