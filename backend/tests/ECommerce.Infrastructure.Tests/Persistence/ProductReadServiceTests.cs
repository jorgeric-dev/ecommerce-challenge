using ECommerce.Application.Common.Models;
using ECommerce.Application.Products;
using ECommerce.Application.Products.Queries.SearchProducts;
using ECommerce.Infrastructure.Persistence.ReadServices;
using Shouldly;
using SortDirection = ECommerce.Application.Common.Models.SortDirection;

namespace ECommerce.Infrastructure.Tests.Persistence;

public sealed class ProductReadServiceTests : IAsyncLifetime
{
    private readonly TestDatabase _database = new();

    public async Task InitializeAsync()
    {
        await _database.SeedProductAsync("BS-021", "Bluetooth Speaker", 59.99m, 110, TestDatabase.Electronics, "Portable waterproof speaker");
        await _database.SeedProductAsync("RS-001", "Running Shoes", 89.99m, 150, TestDatabase.Footwear, "Lightweight shoes");
        await _database.SeedProductAsync("WM-042", "Wireless Mouse", 29.99m, 0, TestDatabase.Electronics, "100% ergonomic");
        await _database.SeedProductAsync("HB-002", "Hiking Boots", 159.99m, 40, TestDatabase.Footwear);
        await _database.SeedProductAsync("OLD-1", "Old Speaker", 5m, 10, TestDatabase.Electronics, deleted: true);
    }

    public Task DisposeAsync()
    {
        _database.Dispose();
        return Task.CompletedTask;
    }

    private async Task<PagedResult<ProductDto>> Search(SearchProductsQuery query)
    {
        await using var context = _database.CreateContext();
        return await new ProductReadService(context).SearchAsync(query);
    }

    [Fact]
    public async Task SearchAsync_WithoutFilters_ReturnsActiveProductsSortedByName()
    {
        var result = await Search(new SearchProductsQuery());

        result.TotalCount.ShouldBe(4);
        result.Items.Select(p => p.Name).ShouldBe(["Bluetooth Speaker", "Hiking Boots", "Running Shoes", "Wireless Mouse"]);
    }

    [Theory]
    [InlineData("speaker", "BS-021")]
    [InlineData("SHOES", "RS-001")]
    [InlineData("wm-0", "WM-042")]
    [InlineData("waterproof", "BS-021")]
    [InlineData("100%", "WM-042")]
    public async Task SearchAsync_ByTerm_MatchesNameSkuOrDescriptionCaseInsensitively(string term, string expectedSku)
    {
        var result = await Search(new SearchProductsQuery(SearchTerm: term));

        result.Items.ShouldHaveSingleItem().Sku.ShouldBe(expectedSku);
    }

    [Fact]
    public async Task SearchAsync_WildcardCharactersAreTreatedLiterally()
    {
        (await Search(new SearchProductsQuery(SearchTerm: "%"))).Items.ShouldHaveSingleItem().Sku.ShouldBe("WM-042");
        (await Search(new SearchProductsQuery(SearchTerm: "_"))).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task SearchAsync_ByCategory_FiltersAndProjectsCategoryName()
    {
        var result = await Search(new SearchProductsQuery(CategoryId: TestDatabase.Footwear));

        result.Items.Count.ShouldBe(2);
        result.Items.ShouldAllBe(p => p.CategoryName == "Footwear");
    }

    [Fact]
    public async Task SearchAsync_ByPriceRange_IsInclusive()
    {
        var result = await Search(new SearchProductsQuery(MinPrice: 29.99m, MaxPrice: 89.99m));

        result.Items.Select(p => p.Sku).ShouldBe(["BS-021", "RS-001", "WM-042"], ignoreOrder: true);
    }

    [Fact]
    public async Task SearchAsync_InStockOnly_ExcludesOutOfStock()
    {
        var result = await Search(new SearchProductsQuery(InStockOnly: true));

        result.Items.ShouldNotContain(p => p.Sku == "WM-042");
        result.Items.ShouldAllBe(p => p.IsInStock);
    }

    [Theory]
    [InlineData(ProductSortField.Price, SortDirection.Asc, "WM-042")]
    [InlineData(ProductSortField.Price, SortDirection.Desc, "HB-002")]
    [InlineData(ProductSortField.Stock, SortDirection.Desc, "RS-001")]
    [InlineData(ProductSortField.Sku, SortDirection.Asc, "BS-021")]
    [InlineData(ProductSortField.Name, SortDirection.Desc, "WM-042")]
    [InlineData(ProductSortField.CreatedAt, SortDirection.Asc, "BS-021")]
    public async Task SearchAsync_Sorting_ReturnsExpectedFirstItem(ProductSortField sortBy, SortDirection direction, string expectedFirstSku)
    {
        var result = await Search(new SearchProductsQuery(SortBy: sortBy, SortDirection: direction));

        result.Items[0].Sku.ShouldBe(expectedFirstSku);
    }

    [Theory]
    [InlineData(SortDirection.Asc, new[] { "Electronics", "Electronics", "Footwear", "Footwear" })]
    [InlineData(SortDirection.Desc, new[] { "Footwear", "Footwear", "Electronics", "Electronics" })]
    public async Task SearchAsync_SortingByCategory_OrdersByCategoryName(SortDirection direction, string[] expectedCategories)
    {
        var result = await Search(new SearchProductsQuery(SortBy: ProductSortField.Category, SortDirection: direction));

        result.Items.Select(p => p.CategoryName).ShouldBe(expectedCategories);
    }

    [Fact]
    public async Task SearchAsync_Paging_ReturnsRequestedSlice()
    {
        var result = await Search(new SearchProductsQuery(Page: 2, PageSize: 3));

        result.Items.ShouldHaveSingleItem().Name.ShouldBe("Wireless Mouse");
        result.TotalCount.ShouldBe(4);
        result.TotalPages.ShouldBe(2);
        result.HasNextPage.ShouldBeFalse();
    }

    [Fact]
    public async Task SearchAsync_NoMatches_ReturnsEmptyPage()
    {
        var result = await Search(new SearchProductsQuery(SearchTerm: "does-not-exist"));

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDtoWithoutTracking()
    {
        var seeded = await _database.SeedProductAsync("NEW-1", "New", 1m, 1, TestDatabase.Footwear);
        await using var context = _database.CreateContext();

        var dto = await new ProductReadService(context).GetByIdAsync(seeded.Id);

        dto.ShouldNotBeNull().CategoryName.ShouldBe("Footwear");
        context.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        await using var context = _database.CreateContext();

        (await new ProductReadService(context).GetByIdAsync(Guid.NewGuid())).ShouldBeNull();
    }
}
