using ECommerce.Application.Common.Models;
using FluentValidation;

namespace ECommerce.Application.Products.Queries.SearchProducts;

public enum ProductSortField
{
    Name,
    Sku,
    Price,
    Stock,
    CreatedAt,
    Category,
}

public sealed record SearchProductsQuery(
    string? SearchTerm = null,
    int? CategoryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool InStockOnly = false,
    ProductSortField SortBy = ProductSortField.Name,
    SortDirection SortDirection = SortDirection.Asc,
    int Page = PagingDefaults.Page,
    int PageSize = PagingDefaults.PageSize) : IPagedRequest
{
    public const int SearchTermMaxLength = 100;
}

public sealed class SearchProductsQueryValidator : AbstractValidator<SearchProductsQuery>
{
    public SearchProductsQueryValidator(IValidator<IPagedRequest> pagedRequestValidator)
    {
        Include(pagedRequestValidator);

        RuleFor(x => x.SearchTerm).MaximumLength(SearchProductsQuery.SearchTermMaxLength);
        RuleFor(x => x.CategoryId).GreaterThan(0).When(x => x.CategoryId.HasValue);
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);
        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(x => x.MinPrice!.Value)
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue)
            .WithMessage("Max price must be greater than or equal to min price.");
        RuleFor(x => x.SortBy).IsInEnum();
        RuleFor(x => x.SortDirection).IsInEnum();
    }
}
