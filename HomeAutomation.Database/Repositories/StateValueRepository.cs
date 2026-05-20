using HomeAutomation.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeAutomation.Database.Repositories;

public interface IStateValueRepository : IRepository<StateValueEntity>
{
    Task<List<StateValueEntity>> GetAll(CancellationToken cancellationToken);

    Task<StateValueEntity?> GetByKey(string key, CancellationToken cancellationToken);
}

public class StateValueRepository(DefaultContext context) : Repository<StateValueEntity>(context), IStateValueRepository
{
    public async Task<List<StateValueEntity>> GetAll(CancellationToken cancellationToken)
    {
        return await Table
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<StateValueEntity?> GetByKey(string key, CancellationToken cancellationToken)
    {
        return await Table
            .SingleOrDefaultAsync(x => x.Key == key, cancellationToken);
    }
}
