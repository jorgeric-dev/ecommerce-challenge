using System.Linq.Expressions;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products;
using ECommerce.Application.Products.Queries.SearchProducts;
using ECommerce.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.ReadServices;

internal sealed class ProductReadService(AppDbContext dbContext) : IProductReadService
{
    private static readonly Expression<Func<Product, ProductDto>> ToDto = product => new ProductDto(
        product.Id,
        product.Name,
        product.Sku,
        product.Description,
        product.CategoryId,
        product.Category!.Name,
        product.Price,
        product.Stock,
        product.WeightKg,
        product.Stock > 0,
        product.CreatedAtUtc,
        product.UpdatedAtUtc);

    public Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PagedResult<ProductDto>> SearchAsync(SearchProductsQuery criteria, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var query = dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            var pattern = criteria.SearchTerm.Trim().ToContainsLikePattern();
            query = query.Where(product =>
                EF.Functions.Like(product.Name, pattern, QueryableExtensions.LikeEscape)
                || EF.Functions.Like(product.Sku, pattern, QueryableExtensions.LikeEscape)
                || (product.Description != null && EF.Functions.Like(product.Description, pattern, QueryableExtensions.LikeEscape)));
        }

        if (criteria.CategoryId is { } categoryId)
        {
            query = query.Where(product => product.CategoryId == categoryId);
        }

        if (criteria.MinPrice is { } minPrice)
        {
            query = query.Where(product => product.Price >= minPrice);
        }

        if (criteria.MaxPrice is { } maxPrice)
        {
            query = query.Where(product => product.Price <= maxPrice);
        }

        if (criteria.InStockOnly)
        {
            query = query.Where(product => product.Stock > 0);
        }

        return Sort(query, criteria.SortBy, criteria.SortDirection)
            .Select(ToDto)
            .ToPagedResultAsync(criteria.Page, criteria.PageSize, cancellationToken);
    }

    private static IQueryable<Product> Sort(IQueryable<Product> query, ProductSortField sortBy, SortDirection direction)
    {
        var ordered = sortBy switch
        {
            ProductSortField.Sku => query.OrderByDirection(product => product.Sku, direction),
            ProductSortField.Price => query.OrderByDirection(product => product.Price, direction),
            ProductSortField.Stock => query.OrderByDirection(product => product.Stock, direction),
            ProductSortField.CreatedAt => query.OrderByDirection(product => product.CreatedAtUtc, direction),
            ProductSortField.Category => query.OrderByDirection(product => product.Category!.Name, direction),
            _ => query.OrderByDirection(product => product.Name, direction),
        };

        return ordered.ThenBy(product => product.Id);
    }
}
