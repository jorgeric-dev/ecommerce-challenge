using System.Net.Mail;
using ECommerce.Domain.Common;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Products;

namespace ECommerce.Domain.Orders;

public sealed class Order : AuditableEntity<Guid>, IAggregateRoot
{
    private readonly List<OrderItem> _items = [];

    private Order()
    {
    }

    public string OrderNumber { get; private set; } = string.Empty;

    public string CustomerName { get; private set; } = string.Empty;

    public string CustomerEmail { get; private set; } = string.Empty;

    public string ShippingAddress { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }

    public decimal TotalAmount { get; private set; }

    public string? PaymentReference { get; private set; }

    public string? CardLast4 { get; private set; }

    public DateTime? PaidAtUtc { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public static Order Create(string customerName, string customerEmail, string shippingAddress)
    {
        var id = Guid.CreateVersion7();

        return new Order
        {
            Id = id,
            OrderNumber = GenerateOrderNumber(id),
            CustomerName = Guard.WithoutMarkup(Guard.RequiredText(customerName, "Customer name", OrderConstraints.CustomerNameMaxLength), "Customer name"),
            CustomerEmail = ValidateEmail(customerEmail),
            ShippingAddress = Guard.WithoutMarkup(Guard.RequiredText(shippingAddress, "Shipping address", OrderConstraints.ShippingAddressMaxLength), "Shipping address"),
            Status = OrderStatus.Pending,
        };
    }

    public void AddItem(Product product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);
        EnsureStatus(OrderStatus.Pending, "add items to");

        var existing = _items.Find(item => item.ProductId == product.Id);
        if (existing is null && _items.Count >= OrderConstraints.MaxLinesPerOrder)
        {
            throw new DomainException($"An order cannot contain more than {OrderConstraints.MaxLinesPerOrder} different products.");
        }

        if (existing is not null && existing.Quantity + quantity > OrderConstraints.MaxQuantityPerLine)
        {
            throw new DomainException($"Quantity must be between 1 and {OrderConstraints.MaxQuantityPerLine}.");
        }

        product.DecreaseStock(quantity);

        if (existing is null)
        {
            _items.Add(OrderItem.Create(Id, product, quantity));
        }
        else
        {
            existing.IncreaseQuantity(quantity);
        }

        TotalAmount = _items.Sum(item => item.LineTotal);
    }

    public void MarkAsPaid(string paymentReference, string cardLast4, DateTime paidAtUtc)
    {
        EnsureStatus(OrderStatus.Pending, "pay");

        if (_items.Count == 0)
        {
            throw new DomainException("An order without items cannot be paid.");
        }

        if (cardLast4 is not { Length: OrderConstraints.CardLast4Length } || !cardLast4.All(char.IsAsciiDigit))
        {
            throw new DomainException("Card last 4 digits are invalid.");
        }

        PaymentReference = Guard.RequiredText(paymentReference, "Payment reference", OrderConstraints.PaymentReferenceMaxLength);
        CardLast4 = cardLast4;
        PaidAtUtc = paidAtUtc;
        Status = OrderStatus.Paid;
    }

    public void MarkPaymentFailed()
    {
        EnsureStatus(OrderStatus.Pending, "fail the payment of");
        Status = OrderStatus.PaymentFailed;
    }

    private static string GenerateOrderNumber(Guid id) =>
        $"ORD-{id.ToString("N")[^12..].ToUpperInvariant()}";

    private static string ValidateEmail(string? email)
    {
        var value = Guard.RequiredText(email, "Customer email", OrderConstraints.CustomerEmailMaxLength);

        if (!MailAddress.TryCreate(value, out var address) || address.Address != value)
        {
            throw new DomainException($"'{value}' is not a valid email address.");
        }

        return value.ToLowerInvariant();
    }

    private void EnsureStatus(OrderStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new DomainException($"Cannot {action} order {OrderNumber} because it is {Status}.");
        }
    }
}
