using HomeAutomation.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeAutomation.Database.Repositories;

public interface IRepository
{
    DefaultContext Context { get; }
}

public interface IRepository<TEntity> : IRepository where TEntity : BaseEntity
{
    DbSet<TEntity> Table { get; }

    Task<TEntity?> Get(int id, CancellationToken cancellationToken);

    Task AddAndSave(TEntity entity, CancellationToken cancellationToken);

    Task Save(CancellationToken cancellationToken);
}

public class Repository<TEntity>(DefaultContext context) : IRepository<TEntity>, IRepository where TEntity : BaseEntity, new()
{
    public DefaultContext Context => context;

    public DbSet<TEntity> Table => context.Set<TEntity>();

    public async Task AddAndSave(TEntity entity, CancellationToken cancellationToken)
    {
        await Table.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TEntity?> Get(int id, CancellationToken cancellationToken)
    {
        var entity = await Table.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity;
    }

    public async Task Save(CancellationToken cancellationToken)
    {
        await Context.SaveChangesAsync(cancellationToken);
    }
}
