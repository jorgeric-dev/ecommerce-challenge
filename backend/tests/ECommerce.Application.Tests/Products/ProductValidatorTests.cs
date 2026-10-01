using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Commands.CreateProduct;
using ECommerce.Application.Products.Commands.ImportProducts;
using ECommerce.Application.Products.Commands.UpdateProduct;
using ECommerce.Application.Products.Queries.SearchProducts;
using ECommerce.Application.Tests.TestDoubles;
using ECommerce.Domain.Products;
using Shouldly;
using SortDirection = ECommerce.Application.Common.Models.SortDirection;

namespace ECommerce.Application.Tests.Products;

public class ProductValidatorTests
{
    private static CreateProductCommand ValidCreate() => new("Speaker", "BS-021", "Portable", 5, 59.99m, 10, 0.8m);

    private static readonly CreateProductCommandValidator CreateValidator = new(TestData.ProductDetailsValidator());

    [Fact]
    public async Task Create_ValidCommand_Passes()
    {
        (await CreateValidator.ValidateAsync(ValidCreate())).IsValid.ShouldBeTrue();
    }

    public static TheoryData<CreateProductCommand, string> InvalidCreateCommands() => new()
    {
        { ValidCreate() with { Name = "" }, "Name" },
        { ValidCreate() with { Name = "   " }, "Name" },
        { ValidCreate() with { Name = null! }, "Name" },
        { ValidCreate() with { Name = new string('a', ProductConstraints.NameMaxLength + 1) }, "Name" },
        { ValidCreate() with { Name = "<script>alert(1)</script>" }, "Name" },
        { ValidCreate() with { Description = "<img src=x>" }, "Description" },
        { ValidCreate() with { Description = new string('a', ProductConstraints.DescriptionMaxLength + 1) }, "Description" },
        { ValidCreate() with { Sku = "" }, "Sku" },
        { ValidCreate() with { Sku = "has space" }, "Sku" },
        { ValidCreate() with { Sku = new string('A', ProductConstraints.SkuMaxLength + 1) }, "Sku" },
        { ValidCreate() with { CategoryId = 0 }, "CategoryId" },
        { ValidCreate() with { CategoryId = 404 }, "CategoryId" },
        { ValidCreate() with { Price = 0m }, "Price" },
        { ValidCreate() with { Price = 1.234m }, "Price" },
        { ValidCreate() with { Price = 2_000_000m }, "Price" },
        { ValidCreate() with { Stock = -1 }, "Stock" },
        { ValidCreate() with { WeightKg = -0.5m }, "WeightKg" },
        { ValidCreate() with { WeightKg = 0.12345m }, "WeightKg" },
    };

    [Theory]
    [MemberData(nameof(InvalidCreateCommands))]
    public async Task Create_InvalidCommand_FailsOnExpectedProperty(CreateProductCommand command, string property)
    {
        var result = await CreateValidator.ValidateAsync(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == property);
    }

    [Fact]
    public async Task Update_ValidCommand_Passes()
    {
        var validator = new UpdateProductCommandValidator(TestData.ProductDetailsValidator());

        (await validator.ValidateAsync(new UpdateProductCommand(Guid.NewGuid(), "Name", null, 7, 1m, 0, 0m))).IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Update_EmptyIdAndInvalidDetails_Fails()
    {
        var validator = new UpdateProductCommandValidator(TestData.ProductDetailsValidator());

        var result = await validator.ValidateAsync(new UpdateProductCommand(Guid.Empty, "", null, 7, 0m, 0, 0m));

        result.Errors.Select(error => error.PropertyName).ShouldBe(["Id", "Name", "Price"], ignoreOrder: true);
    }
    [Fact]
    public void Search_DefaultQuery_IsValid()
    {
        var validator = new SearchProductsQueryValidator(TestData.PagedRequestValidator());

        validator.Validate(new SearchProductsQuery()).IsValid.ShouldBeTrue();
    }

    public static TheoryData<SearchProductsQuery, string> InvalidSearches() => new()
    {
        { new SearchProductsQuery(Page: 0), "Page" },
        { new SearchProductsQuery(PageSize: 0), "PageSize" },
        { new SearchProductsQuery(PageSize: PagingDefaults.MaxPageSize + 1), "PageSize" },
        { new SearchProductsQuery(SearchTerm: new string('x', SearchProductsQuery.SearchTermMaxLength + 1)), "SearchTerm" },
        { new SearchProductsQuery(CategoryId: 0), "CategoryId" },
        { new SearchProductsQuery(MinPrice: -1), "MinPrice" },
        { new SearchProductsQuery(MaxPrice: -1), "MaxPrice" },
        { new SearchProductsQuery(MinPrice: 50, MaxPrice: 10), "MaxPrice" },
        { new SearchProductsQuery(SortBy: (ProductSortField)99), "SortBy" },
        { new SearchProductsQuery(SortDirection: (SortDirection)99), "SortDirection" },
    };

    [Theory]
    [MemberData(nameof(InvalidSearches))]
    public void Search_InvalidQuery_FailsOnExpectedProperty(SearchProductsQuery query, string property)
    {
        var result = new SearchProductsQueryValidator(TestData.PagedRequestValidator()).Validate(query);

        result.Errors.ShouldContain(error => error.PropertyName == property);
    }

    [Fact]
    public void Import_ValidCsvFile_Passes()
    {
        using var content = new MemoryStream([1, 2, 3]);

        new ImportProductsCommandValidator().Validate(new ImportProductsCommand(content, "products.CSV")).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("products.xlsx", 10)]
    [InlineData("", 10)]
    [InlineData("products.csv", 0)]
    public void Import_InvalidFile_Fails(string fileName, int size)
    {
        using var content = new MemoryStream(new byte[size]);

        new ImportProductsCommandValidator().Validate(new ImportProductsCommand(content, fileName)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Import_NullContent_Fails()
    {
        var result = new ImportProductsCommandValidator().Validate(new ImportProductsCommand(null!, "a.csv"));

        result.Errors.ShouldContain(error => error.ErrorMessage == "A CSV file is required.");
    }
}
