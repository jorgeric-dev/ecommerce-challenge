using ECommerce.Application.Abstractions.Persistence;

namespace ECommerce.Application.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
}

public sealed class CategoryService(ICategoryCatalog categoryCatalog) : ICategoryService
{
    public Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        categoryCatalog.GetAllAsync(cancellationToken);
}
