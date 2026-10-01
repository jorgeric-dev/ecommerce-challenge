using ECommerce.Domain.Orders;

namespace ECommerce.Application.Abstractions.Persistence;

public interface IOrderRepository : IRepository<Order, Guid>;
