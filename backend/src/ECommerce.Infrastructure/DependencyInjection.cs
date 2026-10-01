using ECommerce.Application.Abstractions.Import;
using ECommerce.Application.Abstractions.Payments;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Infrastructure.Caching;
using ECommerce.Infrastructure.Import;
using ECommerce.Infrastructure.Payments;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.ReadServices;
using ECommerce.Infrastructure.Persistence.Repositories;
using ECommerce.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<CatalogCacheOptions>(configuration.GetSection(CatalogCacheOptions.SectionName));

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(DatabaseOptions.ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{DatabaseOptions.ConnectionStringName}' is not configured.");

            options.ConfigureProvider(databaseOptions, connectionString);
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IProductReadService, ProductReadService>();
        services.AddScoped<IOrderReadService, OrderReadService>();

        services.AddMemoryCache();
        services.AddScoped<CategoryCatalog>();
        services.AddScoped<ICategoryCatalog>(serviceProvider => new CachedCategoryCatalog(
            serviceProvider.GetRequiredService<CategoryCatalog>(),
            serviceProvider.GetRequiredService<IMemoryCache>(),
            serviceProvider.GetRequiredService<IOptions<CatalogCacheOptions>>()));

        services.AddSingleton<IProductCsvReader, CsvProductReader>();
        services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
        services.AddScoped<DatabaseInitializer>();

        return services;
    }

    internal static DbContextOptionsBuilder ConfigureProvider(
        this DbContextOptionsBuilder options,
        DatabaseOptions databaseOptions,
        string connectionString) =>
        databaseOptions.Provider switch
        {
            DatabaseProvider.Sqlite => options.UseSqlite(connectionString),
            _ => options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(databaseOptions.MaxRetryCount)),
        };
}
