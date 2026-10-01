using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Commands.UpdateProduct;
using ECommerce.Application.Products.Queries.SearchProducts;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Contracts;

public sealed record UpdateProductRequest(
    string Name,
    string? Description,
    int CategoryId,
    decimal Price,
    int Stock,
    decimal WeightKg)
{
    public UpdateProductCommand ToCommand(Guid id) =>
        new(id, Name, Description, CategoryId, Price, Stock, WeightKg);
}

public sealed class SearchProductsRequest
{
    [FromQuery(Name = "search")]
    public string? Search { get; init; }

    [FromQuery(Name = "categoryId")]
    public int? CategoryId { get; init; }

    [FromQuery(Name = "minPrice")]
    public decimal? MinPrice { get; init; }

    [FromQuery(Name = "maxPrice")]
    public decimal? MaxPrice { get; init; }

    [FromQuery(Name = "inStockOnly")]
    public bool InStockOnly { get; init; }

    [FromQuery(Name = "sortBy")]
    public ProductSortField SortBy { get; init; } = ProductSortField.Name;

    [FromQuery(Name = "sortDirection")]
    public SortDirection SortDirection { get; init; } = SortDirection.Asc;

    [FromQuery(Name = "page")]
    public int Page { get; init; } = PagingDefaults.Page;

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; init; } = PagingDefaults.PageSize;

    public SearchProductsQuery ToQuery() =>
        new(Search, CategoryId, MinPrice, MaxPrice, InStockOnly, SortBy, SortDirection, Page, PageSize);
}
