using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

internal abstract class Repository<TEntity, TId>(AppDbContext dbContext) : IRepository<TEntity, TId>
    where TEntity : Entity<TId>, IAggregateRoot
    where TId : notnull
{
    protected AppDbContext DbContext { get; } = dbContext;

    protected DbSet<TEntity> Entities => DbContext.Set<TEntity>();

    public virtual Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        Entities.FirstOrDefaultAsync(entity => entity.Id.Equals(id), cancellationToken);

    public void Add(TEntity entity) => Entities.Add(entity);
}
