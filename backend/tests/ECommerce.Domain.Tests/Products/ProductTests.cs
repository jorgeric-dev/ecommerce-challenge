using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Products;
using Shouldly;

namespace ECommerce.Domain.Tests.Products;

public class ProductTests
{
    private static Product CreateValidProduct(int stock = 10) =>
        Product.Create("Running Shoes", "rs-001", "Lightweight shoes", 1, 89.99m, stock, 0.35m);

    [Fact]
    public void Create_WithValidData_SetsAllProperties()
    {
        var product = CreateValidProduct();

        product.Id.ShouldNotBe(Guid.Empty);
        product.Name.ShouldBe("Running Shoes");
        product.Sku.ShouldBe("RS-001");
        product.Description.ShouldBe("Lightweight shoes");
        product.CategoryId.ShouldBe(1);
        product.Price.ShouldBe(89.99m);
        product.Stock.ShouldBe(10);
        product.WeightKg.ShouldBe(0.35m);
        product.IsDeleted.ShouldBeFalse();
        product.IsInStock.ShouldBeTrue();
    }

    [Fact]
    public void Create_TrimsTextAndConvertsBlankDescriptionToNull()
    {
        var product = Product.Create("  Mug  ", "  tm-029 ", "   ", 3, 16.99m, 0, 0.3m);

        product.Name.ShouldBe("Mug");
        product.Sku.ShouldBe("TM-029");
        product.Description.ShouldBeNull();
        product.IsInStock.ShouldBeFalse();
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        CreateValidProduct().Id.ShouldNotBe(CreateValidProduct().Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("     ")]
    public void Create_WithMissingName_Throws(string? name)
    {
        var ex = Should.Throw<DomainException>(() => Product.Create(name!, "SKU-1", null, 1, 1m, 1, 1m));
        ex.Message.ShouldBe("Product name is required.");
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("Bold <b>name</b>")]
    public void Create_WithMarkupInName_Throws(string name)
    {
        var ex = Should.Throw<DomainException>(() => Product.Create(name, "SKU-1", null, 1, 1m, 1, 1m));
        ex.Message.ShouldContain("markup");
    }

    [Fact]
    public void Create_WithSqlLikeName_IsAcceptedAsPlainText()
    {
        var product = Product.Create("Robert'); DROP TABLE products;--", "SQL-001", null, 1, 9.99m, 50, 0.5m);

        product.Name.ShouldBe("Robert'); DROP TABLE products;--");
    }

    [Fact]
    public void Create_WithTooLongName_Throws()
    {
        var name = new string('a', ProductConstraints.NameMaxLength + 1);

        Should.Throw<DomainException>(() => Product.Create(name, "SKU-1", null, 1, 1m, 1, 1m))
            .Message.ShouldContain("must not exceed");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("SKU 001")]
    [InlineData("-SKU")]
    [InlineData("SKU#1")]
    public void Create_WithInvalidSku_Throws(string sku)
    {
        Should.Throw<DomainException>(() => Product.Create("Name", sku, null, 1, 1m, 1, 1m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidCategory_Throws(int categoryId)
    {
        Should.Throw<DomainException>(() => Product.Create("Name", "SKU-1", null, categoryId, 1m, 1, 1m));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("1000000.01")]
    [InlineData("10.999")]
    public void Create_WithInvalidPrice_Throws(string price)
    {
        Should.Throw<DomainException>(() => Product.Create("Name", "SKU-1", null, 1, decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture), 1, 1m));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(ProductConstraints.MaxStock + 1)]
    public void Create_WithInvalidStock_Throws(int stock)
    {
        Should.Throw<DomainException>(() => Product.Create("Name", "SKU-1", null, 1, 1m, stock, 1m));
    }

    [Theory]
    [InlineData("-0.1")]
    [InlineData("10000.001")]
    [InlineData("1.2345")]
    public void Create_WithInvalidWeight_Throws(string weight)
    {
        Should.Throw<DomainException>(() => Product.Create("Name", "SKU-1", null, 1, 1m, 1, decimal.Parse(weight, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Create_WithZeroWeight_IsAllowed()
    {
        Product.Create("Gift Card", "GC-025", null, 1, 25m, 1, 0m).WeightKg.ShouldBe(0m);
    }

    [Fact]
    public void Update_WithValidData_ChangesDetailsButKeepsSku()
    {
        var product = CreateValidProduct();

        product.Update("Trail Shoes", "Updated", 2, 99.50m, 5, 0.4m);

        product.Name.ShouldBe("Trail Shoes");
        product.Description.ShouldBe("Updated");
        product.CategoryId.ShouldBe(2);
        product.Price.ShouldBe(99.50m);
        product.Stock.ShouldBe(5);
        product.WeightKg.ShouldBe(0.4m);
        product.Sku.ShouldBe("RS-001");
    }

    [Fact]
    public void Update_WithInvalidData_LeavesProductUnchanged()
    {
        var product = CreateValidProduct();

        Should.Throw<DomainException>(() => product.Update("New name", null, 2, -1m, 5, 1m));

        product.Name.ShouldBe("Running Shoes");
        product.CategoryId.ShouldBe(1);
    }

    [Fact]
    public void Update_WhenDeleted_Throws()
    {
        var product = CreateValidProduct();
        product.Delete();

        Should.Throw<DomainException>(() => product.Update("Name", null, 1, 1m, 1, 1m))
            .Message.ShouldContain("has been deleted");
    }

    [Fact]
    public void DecreaseStock_WithAvailableStock_ReducesStock()
    {
        var product = CreateValidProduct(stock: 10);

        product.DecreaseStock(4);

        product.Stock.ShouldBe(6);
    }

    [Fact]
    public void DecreaseStock_ToZero_MarksOutOfStock()
    {
        var product = CreateValidProduct(stock: 3);

        product.DecreaseStock(3);

        product.IsInStock.ShouldBeFalse();
    }

    [Fact]
    public void DecreaseStock_BeyondAvailable_ThrowsInsufficientStock()
    {
        var product = CreateValidProduct(stock: 2);

        var ex = Should.Throw<InsufficientStockException>(() => product.DecreaseStock(3));

        ex.Message.ShouldBe("Insufficient stock for product 'RS-001'. Requested 3, available 2.");
        product.Stock.ShouldBe(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void DecreaseStock_WithNonPositiveQuantity_Throws(int quantity)
    {
        Should.Throw<DomainException>(() => CreateValidProduct().DecreaseStock(quantity));
    }

    [Fact]
    public void IncreaseStock_AddsQuantity()
    {
        var product = CreateValidProduct(stock: 1);

        product.IncreaseStock(4);

        product.Stock.ShouldBe(5);
    }

    [Fact]
    public void IncreaseStock_AboveMaximum_Throws()
    {
        var product = CreateValidProduct(stock: ProductConstraints.MaxStock);

        Should.Throw<DomainException>(() => product.IncreaseStock(1));
    }

    [Fact]
    public void Delete_MarksProductAsDeleted()
    {
        var product = CreateValidProduct();

        product.Delete();

        product.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void RenewConcurrencyStamp_ChangesStamp()
    {
        var product = CreateValidProduct();
        var original = product.ConcurrencyStamp;

        product.RenewConcurrencyStamp();

        product.ConcurrencyStamp.ShouldNotBe(original);
    }

    [Fact]
    public void MarkCreatedAndUpdated_SetAuditFields()
    {
        var product = CreateValidProduct();
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        product.MarkCreated(now);
        product.MarkUpdated(now.AddHours(1));

        product.CreatedAtUtc.ShouldBe(now);
        product.UpdatedAtUtc.ShouldBe(now.AddHours(1));
    }

    [Fact]
    public void Equality_IsBasedOnId()
    {
        var product = CreateValidProduct();

        product.Equals(product).ShouldBeTrue();
        product.Equals(CreateValidProduct()).ShouldBeFalse();
        product.Equals((object?)null).ShouldBeFalse();
        product.GetHashCode().ShouldBe(product.GetHashCode());
    }
}
