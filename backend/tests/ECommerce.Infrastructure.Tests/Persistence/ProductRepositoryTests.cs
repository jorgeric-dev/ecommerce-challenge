using ECommerce.Infrastructure.Persistence.Repositories;
using Shouldly;

namespace ECommerce.Infrastructure.Tests.Persistence;

public sealed class ProductRepositoryTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task GetByIdAsync_ReturnsTrackedProduct()
    {
        var seeded = await _database.SeedProductAsync("A-1");
        await using var context = _database.CreateContext();

        var product = await new ProductRepository(context).GetByIdAsync(seeded.Id);

        product.ShouldNotBeNull().Sku.ShouldBe("A-1");
        context.ChangeTracker.Entries().ShouldHaveSingleItem();
    }

    [Fact]
    public async Task GetByIdAsync_SoftDeletedProduct_ReturnsNull()
    {
        var seeded = await _database.SeedProductAsync("A-1", deleted: true);
        await using var context = _database.CreateContext();

        (await new ProductRepository(context).GetByIdAsync(seeded.Id)).ShouldBeNull();
    }

    [Theory]
    [InlineData("A-1", true)]
    [InlineData("B-1", false)]
    [InlineData("D-1", false)]
    public async Task SkuExistsAsync_IgnoresDeletedProducts(string sku, bool expected)
    {
        await _database.SeedProductAsync("A-1");
        await _database.SeedProductAsync("D-1", deleted: true);
        await using var context = _database.CreateContext();

        (await new ProductRepository(context).SkuExistsAsync(sku)).ShouldBe(expected);
    }

    [Fact]
    public async Task GetByIdsAsync_ReturnsOnlyMatchingProducts()
    {
        var a = await _database.SeedProductAsync("A-1");
        var b = await _database.SeedProductAsync("B-1");
        await _database.SeedProductAsync("C-1");
        await using var context = _database.CreateContext();

        var products = await new ProductRepository(context).GetByIdsAsync([a.Id, b.Id, Guid.NewGuid()]);

        products.Keys.ShouldBe([a.Id, b.Id], ignoreOrder: true);
    }

    [Fact]
    public async Task GetBySkusAsync_ReturnsDictionaryWithCaseInsensitiveKeys()
    {
        await _database.SeedProductAsync("A-1");
        await _database.SeedProductAsync("B-1");
        await using var context = _database.CreateContext();

        var products = await new ProductRepository(context).GetBySkusAsync(["A-1", "Z-9"]);

        products.Count.ShouldBe(1);
        products["a-1"].Sku.ShouldBe("A-1");
    }

    [Fact]
    public async Task Add_PersistsProductWithAuditTimestamp()
    {
        await using (var context = _database.CreateContext())
        {
            new ProductRepository(context).Add(Domain.Products.Product.Create("New", "N-1", null, TestDatabase.Footwear, 5m, 1, 0m));
            await context.SaveChangesAsync();
        }

        await using var verify = _database.CreateContext();
        var saved = verify.Products.Single();
        saved.CreatedAtUtc.ShouldBe(_database.Clock.GetUtcNow().UtcDateTime);
        saved.CreatedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
        saved.UpdatedAtUtc.ShouldBeNull();
    }
}
