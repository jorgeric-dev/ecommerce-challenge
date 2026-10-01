using ECommerce.Application.Orders.Queries.GetOrders;
using ECommerce.Domain.Orders;
using ECommerce.Infrastructure.Persistence.ReadServices;
using ECommerce.Infrastructure.Persistence.Repositories;
using Shouldly;

namespace ECommerce.Infrastructure.Tests.Persistence;

public sealed class OrderPersistenceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task<Order> PlaceOrderAsync(string email, params (string Sku, int Quantity)[] lines)
    {
        await using var context = _database.CreateContext();
        var order = Order.Create("Jane Doe", email, "1 Main St");
        foreach (var (sku, quantity) in lines)
        {
            var product = context.Products.Single(p => p.Sku == sku);
            order.AddItem(product, quantity);
        }

        order.MarkAsPaid("PAY-1", "4242", _database.Clock.GetUtcNow().UtcDateTime);
        new OrderRepository(context).Add(order);
        await context.SaveChangesAsync();
        return order;
    }

    [Fact]
    public async Task OrderReadService_GetByIdAsync_ProjectsItemsWithoutTracking()
    {
        await _database.SeedProductAsync("A-1", "Alpha", price: 2.50m, stock: 10);
        var order = await PlaceOrderAsync("jane@example.com", ("A-1", 4));
        await using var context = _database.CreateContext();

        var dto = await new OrderReadService(context).GetByIdAsync(order.Id);

        dto.ShouldNotBeNull();
        dto.OrderNumber.ShouldBe(order.OrderNumber);
        dto.CardLast4.ShouldBe("4242");
        var item = dto.Items.ShouldHaveSingleItem();
        item.ProductName.ShouldBe("Alpha");
        item.LineTotal.ShouldBe(10m);
        context.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public async Task OrderReadService_GetByIdAsync_UnknownId_ReturnsNull()
    {
        await using var context = _database.CreateContext();

        (await new OrderReadService(context).GetByIdAsync(Guid.NewGuid())).ShouldBeNull();
    }

    [Fact]
    public async Task OrderReadService_GetOrdersAsync_FiltersByEmailAndOrdersNewestFirst()
    {
        await _database.SeedProductAsync("A-1", stock: 50);
        var first = await PlaceOrderAsync("jane@example.com", ("A-1", 1));
        _database.Clock.Advance(TimeSpan.FromMinutes(5));
        var second = await PlaceOrderAsync("jane@example.com", ("A-1", 2));
        await PlaceOrderAsync("john@example.com", ("A-1", 1));
        await using var context = _database.CreateContext();

        var result = await new OrderReadService(context).GetOrdersAsync(new GetOrdersQuery(" JANE@example.com "));

        result.TotalCount.ShouldBe(2);
        result.Items.Select(o => o.Id).ShouldBe([second.Id, first.Id]);
        result.Items[0].ItemCount.ShouldBe(1);
    }

    [Fact]
    public async Task OrderReadService_GetOrdersAsync_WithoutFilter_ReturnsAllOrdersPaged()
    {
        await _database.SeedProductAsync("A-1", stock: 50);
        for (var i = 0; i < 3; i++)
        {
            await PlaceOrderAsync($"user{i}@example.com", ("A-1", 1));
        }

        await using var context = _database.CreateContext();

        var result = await new OrderReadService(context).GetOrdersAsync(new GetOrdersQuery(Page: 1, PageSize: 2));

        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(3);
    }
}
