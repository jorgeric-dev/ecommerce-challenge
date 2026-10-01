using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Categories;
using ECommerce.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Caching;

internal sealed class CachedCategoryCatalog(
    ICategoryCatalog inner,
    IMemoryCache cache,
    IOptions<CatalogCacheOptions> options) : ICategoryCatalog
{
    internal const string CacheKey = "catalog:categories";

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<CategoryDto>? cached) && cached is not null)
        {
            return cached;
        }

        var categories = await inner.GetAllAsync(cancellationToken);
        cache.Set(CacheKey, categories, options.Value.CategoriesTimeToLive);
        return categories;
    }
}
