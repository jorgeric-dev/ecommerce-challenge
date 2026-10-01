using ECommerce.Application;
using ECommerce.Application.Products;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Products.Queries.SearchProducts;
using ECommerce.Infrastructure.Caching;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Seeding;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ECommerce.Infrastructure.Tests.Services;

public sealed class DependencyInjectionTests : IAsyncLifetime
{
    private const string ConnectionString = "DataSource=di-tests;Mode=Memory;Cache=Shared";

    private readonly SqliteConnection _keepAlive = new(ConnectionString);
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        await _keepAlive.OpenAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ECommerce"] = ConnectionString,
                ["Database:Provider"] = "Sqlite",
                ["Database:SeedSampleProducts"] = "true",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _keepAlive.DisposeAsync();
    }

    [Fact]
    public async Task DatabaseInitializer_CreatesSchemaAndSeedsSampleProductsOnce()
    {
        await using (var scope = _provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
        }

        await using (var scope = _provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            (await context.Products.CountAsync()).ShouldBe(84);
            (await context.Products.AnyAsync(product => product.Sku == "XS-001")).ShouldBeFalse();
            (await context.Products.SingleAsync(product => product.Sku == "RS-001")).Price.ShouldBe(94.99m);
            (await context.Products.SingleAsync(product => product.Sku == "WM-042")).Price.ShouldBe(29.99m);
        }
    }

    [Fact]
    public async Task ProductService_ResolvesEndToEndAgainstDatabase()
    {
        await using var scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
        var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

        var result = await productService.SearchAsync(new SearchProductsQuery(SearchTerm: "speaker"));

        result.TotalCount.ShouldBe(3);
    }

    [Fact]
    public void CategoryCatalog_IsDecoratedWithCache()
    {
        using var scope = _provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ICategoryCatalog>().ShouldBeOfType<CachedCategoryCatalog>();
    }

    [Fact]
    public void MissingConnectionString_ThrowsDescriptiveError()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(TimeProvider.System);
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var ex = Should.Throw<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<AppDbContext>());

        ex.Message.ShouldContain("ECommerce");
    }
}
