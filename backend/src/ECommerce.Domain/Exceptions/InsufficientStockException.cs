namespace ECommerce.Domain.Exceptions;

public sealed class InsufficientStockException(string sku, int requested, int available)
    : DomainException($"Insufficient stock for product '{sku}'. Requested {requested}, available {available}.");
