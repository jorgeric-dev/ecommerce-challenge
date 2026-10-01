namespace ECommerce.Domain.Common;

public interface IConcurrencyAware
{
    Guid ConcurrencyStamp { get; }

    void RenewConcurrencyStamp();
}
