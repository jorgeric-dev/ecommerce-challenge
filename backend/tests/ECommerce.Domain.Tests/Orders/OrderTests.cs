using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Orders;
using ECommerce.Domain.Products;
using Shouldly;

namespace ECommerce.Domain.Tests.Orders;

public class OrderTests
{
    private static readonly DateTime PaidAt = new(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);

    private static Order CreateOrder() => Order.Create("Jane Doe", "Jane.Doe@Example.com", "1 Main Street, Springfield");

    private static Product CreateProduct(decimal price = 10m, int stock = 20, string sku = "SKU-1") =>
        Product.Create("Product " + sku, sku, null, 1, price, stock, 1m);

    [Fact]
    public void Create_WithValidData_CreatesPendingOrder()
    {
        var order = CreateOrder();

        order.Id.ShouldNotBe(Guid.Empty);
        order.OrderNumber.ShouldStartWith("ORD-");
        order.OrderNumber.Length.ShouldBe(16);
        order.CustomerName.ShouldBe("Jane Doe");
        order.CustomerEmail.ShouldBe("jane.doe@example.com");
        order.ShippingAddress.ShouldBe("1 Main Street, Springfield");
        order.Status.ShouldBe(OrderStatus.Pending);
        order.TotalAmount.ShouldBe(0m);
        order.Items.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("", "a@b.com", "Address")]
    [InlineData("Name", "", "Address")]
    [InlineData("Name", "not-an-email", "Address")]
    [InlineData("Name", "Jane <jane@x.com>", "Address")]
    [InlineData("Name", "a@b.com", "  ")]
    [InlineData("<b>Name</b>", "a@b.com", "Address")]
    public void Create_WithInvalidData_Throws(string name, string email, string address)
    {
        Should.Throw<DomainException>(() => Order.Create(name, email, address));
    }

    [Fact]
    public void AddItem_AddsLineDecreasesStockAndUpdatesTotal()
    {
        var order = CreateOrder();
        var product = CreateProduct(price: 12.50m, stock: 10);

        order.AddItem(product, 3);

        var item = order.Items.ShouldHaveSingleItem();
        item.OrderId.ShouldBe(order.Id);
        item.ProductId.ShouldBe(product.Id);
        item.ProductName.ShouldBe(product.Name);
        item.Sku.ShouldBe(product.Sku);
        item.UnitPrice.ShouldBe(12.50m);
        item.Quantity.ShouldBe(3);
        item.LineTotal.ShouldBe(37.50m);
        order.TotalAmount.ShouldBe(37.50m);
        product.Stock.ShouldBe(7);
    }

    [Fact]
    public void AddItem_SameProductTwice_MergesLines()
    {
        var order = CreateOrder();
        var product = CreateProduct(price: 5m, stock: 10);

        order.AddItem(product, 2);
        order.AddItem(product, 3);

        order.Items.ShouldHaveSingleItem().Quantity.ShouldBe(5);
        order.TotalAmount.ShouldBe(25m);
        product.Stock.ShouldBe(5);
    }

    [Fact]
    public void AddItem_MultipleProducts_SumsTotal()
    {
        var order = CreateOrder();

        order.AddItem(CreateProduct(price: 5m, sku: "A-1"), 2);
        order.AddItem(CreateProduct(price: 7.25m, sku: "B-1"), 4);

        order.Items.Count.ShouldBe(2);
        order.TotalAmount.ShouldBe(39m);
    }

    [Fact]
    public void AddItem_WithInsufficientStock_ThrowsAndDoesNotAddLine()
    {
        var order = CreateOrder();
        var product = CreateProduct(stock: 1);

        Should.Throw<InsufficientStockException>(() => order.AddItem(product, 2));

        order.Items.ShouldBeEmpty();
        product.Stock.ShouldBe(1);
    }

    [Fact]
    public void AddItem_ExceedingQuantityPerLine_Throws()
    {
        var order = CreateOrder();
        var product = CreateProduct(stock: 500);
        order.AddItem(product, OrderConstraints.MaxQuantityPerLine);

        Should.Throw<DomainException>(() => order.AddItem(product, 1));

        product.Stock.ShouldBe(500 - OrderConstraints.MaxQuantityPerLine);
    }

    [Fact]
    public void AddItem_ExceedingMaxLines_Throws()
    {
        var order = CreateOrder();
        for (var i = 0; i < OrderConstraints.MaxLinesPerOrder; i++)
        {
            order.AddItem(CreateProduct(sku: $"SKU-{i}"), 1);
        }

        Should.Throw<DomainException>(() => order.AddItem(CreateProduct(sku: "EXTRA"), 1));
    }

    [Fact]
    public void AddItem_WithNullProduct_Throws()
    {
        Should.Throw<ArgumentNullException>(() => CreateOrder().AddItem(null!, 1));
    }

    [Fact]
    public void MarkAsPaid_WithItems_SetsPaymentData()
    {
        var order = CreateOrder();
        order.AddItem(CreateProduct(), 1);

        order.MarkAsPaid("PAY-123", "4242", PaidAt);

        order.Status.ShouldBe(OrderStatus.Paid);
        order.PaymentReference.ShouldBe("PAY-123");
        order.CardLast4.ShouldBe("4242");
        order.PaidAtUtc.ShouldBe(PaidAt);
    }

    [Fact]
    public void MarkAsPaid_WithoutItems_Throws()
    {
        Should.Throw<DomainException>(() => CreateOrder().MarkAsPaid("PAY-1", "4242", PaidAt))
            .Message.ShouldContain("without items");
    }

    [Theory]
    [InlineData("424")]
    [InlineData("42a2")]
    [InlineData("")]
    public void MarkAsPaid_WithInvalidCardDigits_Throws(string last4)
    {
        var order = CreateOrder();
        order.AddItem(CreateProduct(), 1);

        Should.Throw<DomainException>(() => order.MarkAsPaid("PAY-1", last4, PaidAt));
    }

    [Fact]
    public void MarkAsPaid_Twice_Throws()
    {
        var order = CreateOrder();
        order.AddItem(CreateProduct(), 1);
        order.MarkAsPaid("PAY-1", "4242", PaidAt);

        Should.Throw<DomainException>(() => order.MarkAsPaid("PAY-2", "4242", PaidAt))
            .Message.ShouldContain("because it is Paid");
    }

    [Fact]
    public void MarkPaymentFailed_FromPending_SetsStatus()
    {
        var order = CreateOrder();

        order.MarkPaymentFailed();

        order.Status.ShouldBe(OrderStatus.PaymentFailed);
    }

    [Fact]
    public void AddItem_AfterPaymentFailed_Throws()
    {
        var order = CreateOrder();
        order.MarkPaymentFailed();

        Should.Throw<DomainException>(() => order.AddItem(CreateProduct(), 1));
    }
}
