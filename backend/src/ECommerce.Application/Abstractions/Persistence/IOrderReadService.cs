using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders;
using ECommerce.Application.Orders.Queries.GetOrders;

namespace ECommerce.Application.Abstractions.Persistence;

public interface IOrderReadService
{
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<OrderSummaryDto>> GetOrdersAsync(GetOrdersQuery criteria, CancellationToken cancellationToken = default);
}
