using ECommerce.Domain.Orders;

namespace ECommerce.Application.Orders;

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    string CustomerEmail,
    string ShippingAddress,
    OrderStatus Status,
    decimal TotalAmount,
    string? PaymentReference,
    string? CardLast4,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc,
    IReadOnlyList<OrderItemDto> Items);

public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public sealed record OrderSummaryDto(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    string CustomerEmail,
    OrderStatus Status,
    decimal TotalAmount,
    int ItemCount,
    DateTime CreatedAtUtc);

public static class OrderMappings
{
    public static OrderDto ToDto(this Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.CustomerName,
            order.CustomerEmail,
            order.ShippingAddress,
            order.Status,
            order.TotalAmount,
            order.PaymentReference,
            order.CardLast4,
            order.CreatedAtUtc,
            order.PaidAtUtc,
            order.Items
                .Select(item => new OrderItemDto(item.ProductId, item.ProductName, item.Sku, item.UnitPrice, item.Quantity, item.LineTotal))
                .ToList());
    }
}
