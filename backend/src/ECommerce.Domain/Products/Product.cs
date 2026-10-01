using System.Text.RegularExpressions;
using ECommerce.Domain.Categories;
using ECommerce.Domain.Common;
using ECommerce.Domain.Exceptions;

namespace ECommerce.Domain.Products;

public sealed partial class Product : AuditableEntity<Guid>, IAggregateRoot, IConcurrencyAware
{
    private Product()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string Sku { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int CategoryId { get; private set; }

    public Category? Category { get; private set; }

    public decimal Price { get; private set; }

    public int Stock { get; private set; }

    public decimal WeightKg { get; private set; }

    public bool IsDeleted { get; private set; }

    public Guid ConcurrencyStamp { get; private set; } = Guid.NewGuid();

    public bool IsInStock => Stock > 0;

    public static Product Create(
        string name,
        string sku,
        string? description,
        int categoryId,
        decimal price,
        int stock,
        decimal weightKg)
    {
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Sku = NormalizeSku(sku),
        };

        product.Update(name, description, categoryId, price, stock, weightKg);
        return product;
    }

    public static string NormalizeSku(string? sku)
    {
        var normalized = Guard.RequiredText(sku, "SKU", ProductConstraints.SkuMaxLength);

        if (!SkuRegex().IsMatch(normalized))
        {
            throw new DomainException("SKU may only contain letters, digits, hyphens and underscores.");
        }

        return normalized.ToUpperInvariant();
    }

    public void Update(
        string name,
        string? description,
        int categoryId,
        decimal price,
        int stock,
        decimal weightKg)
    {
        EnsureNotDeleted();

        var validName = Guard.WithoutMarkup(Guard.RequiredText(name, "Product name", ProductConstraints.NameMaxLength), "Product name");
        var validDescription = Guard.OptionalText(description, "Description", ProductConstraints.DescriptionMaxLength);
        var validCategoryId = Guard.Positive(categoryId, "Category id");
        var validPrice = Guard.Range(price, "Price", ProductConstraints.MinPrice, ProductConstraints.MaxPrice, ProductConstraints.PriceScale);
        var validStock = Guard.Range(stock, "Stock", ProductConstraints.MinStock, ProductConstraints.MaxStock);
        var validWeight = Guard.Range(weightKg, "Weight", ProductConstraints.MinWeightKg, ProductConstraints.MaxWeightKg, ProductConstraints.WeightScale);

        Name = validName;
        Description = validDescription is null ? null : Guard.WithoutMarkup(validDescription, "Description");
        CategoryId = validCategoryId;
        Price = validPrice;
        Stock = validStock;
        WeightKg = validWeight;
    }

    public void DecreaseStock(int quantity)
    {
        EnsureNotDeleted();
        Guard.Positive(quantity, "Quantity");

        if (quantity > Stock)
        {
            throw new InsufficientStockException(Sku, quantity, Stock);
        }

        Stock -= quantity;
    }

    public void IncreaseStock(int quantity)
    {
        Guard.Positive(quantity, "Quantity");
        Stock = Guard.Range(Stock + quantity, "Stock", ProductConstraints.MinStock, ProductConstraints.MaxStock);
    }

    public void Delete() => IsDeleted = true;

    public void RenewConcurrencyStamp() => ConcurrencyStamp = Guid.NewGuid();

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new DomainException($"Product '{Sku}' has been deleted and can no longer be modified.");
        }
    }

    [GeneratedRegex(ProductConstraints.SkuPattern, RegexOptions.CultureInvariant)]
    private static partial Regex SkuRegex();
}
