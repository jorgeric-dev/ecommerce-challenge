using ECommerce.Domain.Common;
using ECommerce.Domain.Products;

namespace ECommerce.Domain.Orders;

public sealed class OrderItem : Entity<Guid>
{
    private OrderItem()
    {
    }

    public Guid OrderId { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public string Sku { get; private set; } = string.Empty;

    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;

    internal static OrderItem Create(Guid orderId, Product product, int quantity) => new()
    {
        Id = Guid.CreateVersion7(),
        OrderId = orderId,
        ProductId = product.Id,
        ProductName = product.Name,
        Sku = product.Sku,
        UnitPrice = product.Price,
        Quantity = Guard.Range(quantity, "Quantity", 1, OrderConstraints.MaxQuantityPerLine),
    };

    internal void IncreaseQuantity(int quantity)
    {
        Quantity = Guard.Range(Quantity + quantity, "Quantity", 1, OrderConstraints.MaxQuantityPerLine);
    }
}
