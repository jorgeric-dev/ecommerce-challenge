using ECommerce.Domain.Common;

namespace ECommerce.Application.Abstractions.Persistence;

public interface IRepository<TEntity, in TId>
    where TEntity : Entity<TId>, IAggregateRoot
    where TId : notnull
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    void Add(TEntity entity);
}
