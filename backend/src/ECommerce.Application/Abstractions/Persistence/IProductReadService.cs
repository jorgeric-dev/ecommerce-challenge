using ECommerce.Application.Common.Models;
using ECommerce.Application.Products;
using ECommerce.Application.Products.Queries.SearchProducts;

namespace ECommerce.Application.Abstractions.Persistence;

public interface IProductReadService
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<ProductDto>> SearchAsync(SearchProductsQuery criteria, CancellationToken cancellationToken = default);
}
