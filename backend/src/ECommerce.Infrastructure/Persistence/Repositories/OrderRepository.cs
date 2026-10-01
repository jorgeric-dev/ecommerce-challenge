using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Domain.Orders;

namespace ECommerce.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository(AppDbContext dbContext)
    : Repository<Order, Guid>(dbContext), IOrderRepository;
