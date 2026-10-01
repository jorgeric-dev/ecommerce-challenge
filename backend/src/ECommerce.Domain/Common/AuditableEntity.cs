namespace ECommerce.Domain.Common;

public abstract class AuditableEntity<TId> : Entity<TId>, IAuditable
    where TId : notnull
{
    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public void MarkCreated(DateTime utcNow) => CreatedAtUtc = utcNow;

    public void MarkUpdated(DateTime utcNow) => UpdatedAtUtc = utcNow;
}
