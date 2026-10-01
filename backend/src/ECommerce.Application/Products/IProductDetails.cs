namespace ECommerce.Application.Products;

public interface IProductDetails
{
    string Name { get; }

    string? Description { get; }

    int CategoryId { get; }

    decimal Price { get; }

    int Stock { get; }

    decimal WeightKg { get; }
}
