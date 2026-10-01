using ECommerce.Domain.Products;

namespace ECommerce.Application.Abstractions.Persistence;

public interface IProductRepository : IRepository<Product, Guid>
{
    Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, Product>> GetBySkusAsync(IReadOnlyCollection<string> skus, CancellationToken cancellationToken = default);
}
