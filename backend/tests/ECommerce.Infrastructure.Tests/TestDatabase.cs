using ECommerce.Domain.Products;
using ECommerce.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace ECommerce.Infrastructure.Tests;

public sealed class TestDatabase : IDisposable
{
    public const int Electronics = 5;
    public const int Footwear = 7;

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestDatabase()
    {
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero));

    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, Clock);

    public async Task<Product> SeedProductAsync(
        string sku,
        string name = "Product",
        decimal price = 10m,
        int stock = 5,
        int categoryId = Electronics,
        string? description = null,
        bool deleted = false)
    {
        await using var context = CreateContext();
        var product = Product.Create(name, sku, description, categoryId, price, stock, 1m);
        if (deleted)
        {
            product.Delete();
        }

        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    public void Dispose() => _connection.Dispose();
}
