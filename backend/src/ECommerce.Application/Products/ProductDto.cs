using ECommerce.Domain.Products;

namespace ECommerce.Application.Products;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Sku,
    string? Description,
    int CategoryId,
    string CategoryName,
    decimal Price,
    int Stock,
    decimal WeightKg,
    bool IsInStock,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public static class ProductMappings
{
    public static ProductDto ToDto(this Product product, string categoryName)
    {
        ArgumentNullException.ThrowIfNull(product);

        return new ProductDto(
            product.Id,
            product.Name,
            product.Sku,
            product.Description,
            product.CategoryId,
            categoryName,
            product.Price,
            product.Stock,
            product.WeightKg,
            product.IsInStock,
            product.CreatedAtUtc,
            product.UpdatedAtUtc);
    }
}
