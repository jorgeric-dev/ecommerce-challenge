using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders;
using ECommerce.Application.Orders.Queries.GetOrders;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.ReadServices;

internal sealed class OrderReadService(AppDbContext dbContext) : IOrderReadService
{
    public Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Id == id)
            .Select(order => new OrderDto(
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
                    .OrderBy(item => item.ProductName)
                    .Select(item => new OrderItemDto(
                        item.ProductId,
                        item.ProductName,
                        item.Sku,
                        item.UnitPrice,
                        item.Quantity,
                        item.UnitPrice * item.Quantity))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PagedResult<OrderSummaryDto>> GetOrdersAsync(GetOrdersQuery criteria, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var query = dbContext.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(criteria.CustomerEmail))
        {
            var email = criteria.CustomerEmail.Trim().ToLowerInvariant();
            query = query.Where(order => order.CustomerEmail == email);
        }

        return query
            .OrderByDescending(order => order.CreatedAtUtc)
            .ThenBy(order => order.Id)
            .Select(order => new OrderSummaryDto(
                order.Id,
                order.OrderNumber,
                order.CustomerName,
                order.CustomerEmail,
                order.Status,
                order.TotalAmount,
                order.Items.Count,
                order.CreatedAtUtc))
            .ToPagedResultAsync(criteria.Page, criteria.PageSize, cancellationToken);
    }
}
