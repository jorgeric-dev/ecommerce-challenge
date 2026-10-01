using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Categories;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Common.Validation;
using ECommerce.Application.Products;
using ECommerce.Domain.Products;
using FluentValidation;

namespace ECommerce.Application.Tests.TestDoubles;

internal static class TestData
{
    public static readonly CategoryDto Electronics = new(5, "Electronics");
    public static readonly CategoryDto Footwear = new(7, "Footwear");

    public static StubCategoryCatalog Catalog() => new(Electronics, Footwear);

    public static IValidator<IProductDetails> ProductDetailsValidator(ICategoryCatalog? catalog = null) =>
        new ProductDetailsValidator(catalog ?? Catalog());

    public static IValidator<IPagedRequest> PagedRequestValidator() => new PagedRequestValidator();

    public static Product Product(string sku = "SKU-1", decimal price = 10m, int stock = 10, int categoryId = 5) =>
        Domain.Products.Product.Create($"Product {sku}", sku, "Description", categoryId, price, stock, 1m);
}

internal sealed class StubCategoryCatalog(params CategoryDto[] categories) : ICategoryCatalog
{
    public Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CategoryDto>>(categories);
}
