using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Products;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ECommerce.Infrastructure.Tests.Persistence;

public sealed class UnitOfWorkTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task SaveChangesAsync_OnUpdate_SetsUpdatedAtAndRenewsConcurrencyStamp()
    {
        var seeded = await _database.SeedProductAsync("A-1");
        await using var context = _database.CreateContext();
        var product = await context.Products.SingleAsync();
        var originalStamp = product.ConcurrencyStamp;
        _database.Clock.Advance(TimeSpan.FromHours(1));

        product.DecreaseStock(1);
        await new UnitOfWork(context).SaveChangesAsync();

        product.UpdatedAtUtc.ShouldBe(_database.Clock.GetUtcNow().UtcDateTime);
        product.ConcurrencyStamp.ShouldNotBe(originalStamp);
        product.CreatedAtUtc.ShouldBe(seeded.CreatedAtUtc);
    }

    [Fact]
    public async Task SaveChangesAsync_WithStaleProduct_ThrowsConflict()
    {
        await _database.SeedProductAsync("A-1", stock: 5);
        await using var first = _database.CreateContext();
        await using var second = _database.CreateContext();
        var firstCopy = await first.Products.SingleAsync();
        var secondCopy = await second.Products.SingleAsync();

        firstCopy.DecreaseStock(3);
        await new UnitOfWork(first).SaveChangesAsync();
        secondCopy.DecreaseStock(3);

        var ex = await Should.ThrowAsync<ConflictException>(() => new UnitOfWork(second).SaveChangesAsync());

        ex.InnerException.ShouldBeOfType<DbUpdateConcurrencyException>();
        await using var verify = _database.CreateContext();
        (await verify.Products.SingleAsync()).Stock.ShouldBe(2);
    }

    [Fact]
    public async Task SaveChangesAsync_WithDuplicateActiveSku_ThrowsConflict()
    {
        await _database.SeedProductAsync("A-1");
        await using var context = _database.CreateContext();
        context.Products.Add(Product.Create("Duplicate", "A-1", null, TestDatabase.Electronics, 1m, 1, 1m));

        var ex = await Should.ThrowAsync<ConflictException>(() => new UnitOfWork(context).SaveChangesAsync());

        ex.InnerException.ShouldBeOfType<DbUpdateException>();
    }

    [Fact]
    public async Task SaveChangesAsync_SkuOfDeletedProduct_CanBeReused()
    {
        await _database.SeedProductAsync("A-1", deleted: true);
        await using var context = _database.CreateContext();
        context.Products.Add(Product.Create("Replacement", "A-1", null, TestDatabase.Electronics, 1m, 1, 1m));

        (await new UnitOfWork(context).SaveChangesAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task SaveChangesAsync_WithUnknownCategory_ThrowsConflictFromForeignKey()
    {
        await using var context = _database.CreateContext();
        context.Products.Add(Product.Create("Orphan", "O-1", null, 999, 1m, 1, 1m));

        await Should.ThrowAsync<ConflictException>(() => new UnitOfWork(context).SaveChangesAsync());
    }

    [Fact]
    public void SynchronousSaveChanges_AlsoAppliesAuditing()
    {
        using var context = _database.CreateContext();
        var product = Product.Create("Sync", "S-1", null, TestDatabase.Electronics, 1m, 1, 1m);
        context.Products.Add(product);

        context.SaveChanges();

        product.CreatedAtUtc.ShouldBe(_database.Clock.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public void Model_SeedsCategoryCatalog()
    {
        using var context = _database.CreateContext();

        context.Categories.Count().ShouldBe(18);
        context.Categories.Single(category => category.Id == TestDatabase.Electronics).Name.ShouldBe("Electronics");
    }
}
