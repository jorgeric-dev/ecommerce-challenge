using ECommerce.Application.Categories;

namespace ECommerce.Application.Abstractions.Persistence;

public interface ICategoryCatalog
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
}

public static class CategoryCatalogExtensions
{
    public static async Task<CategoryDto?> FindByIdAsync(this ICategoryCatalog catalog, int id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var categories = await catalog.GetAllAsync(cancellationToken);
        return categories.FirstOrDefault(category => category.Id == id);
    }

    public static async Task<IReadOnlyDictionary<string, CategoryDto>> GetByNameAsync(this ICategoryCatalog catalog, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var categories = await catalog.GetAllAsync(cancellationToken);
        return categories.ToDictionary(category => category.Name, StringComparer.OrdinalIgnoreCase);
    }
}
