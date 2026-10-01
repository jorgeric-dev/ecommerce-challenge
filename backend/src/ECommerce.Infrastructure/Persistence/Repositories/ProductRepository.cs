using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(AppDbContext dbContext)
    : Repository<Product, Guid>(dbContext), IProductRepository
{
    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default) =>
        Entities.AnyAsync(product => product.Sku == sku, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, Product>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        await Entities
            .Where(product => ids.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

    public async Task<IReadOnlyDictionary<string, Product>> GetBySkusAsync(
        IReadOnlyCollection<string> skus,
        CancellationToken cancellationToken = default) =>
        await Entities
            .Where(product => skus.Contains(product.Sku))
            .ToDictionaryAsync(product => product.Sku, StringComparer.OrdinalIgnoreCase, cancellationToken);
}
