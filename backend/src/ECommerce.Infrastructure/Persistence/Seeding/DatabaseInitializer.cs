using ECommerce.Application.Products.Commands.ImportProducts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Persistence.Seeding;

public sealed partial class DatabaseInitializer(
    AppDbContext dbContext,
    IProductImportService productImportService,
    IOptions<DatabaseOptions> options,
    ILogger<DatabaseInitializer> logger)
{
    internal const string SampleProductsResourceName = "ECommerce.Infrastructure.SampleProducts.csv";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (options.Value.ApplyMigrationsOnStartup)
        {
            await ApplySchemaAsync(cancellationToken);
        }

        if (options.Value.SeedSampleProducts)
        {
            await SeedSampleProductsAsync(cancellationToken);
        }
    }

    private async Task ApplySchemaAsync(CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsSqlServer())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
            LogMigrationsApplied();
        }
        else
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }
    }

    private async Task SeedSampleProductsAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Products.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        await using var stream = typeof(DatabaseInitializer).Assembly.GetManifestResourceStream(SampleProductsResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{SampleProductsResourceName}' was not found.");

        var result = await productImportService.ImportAsync(new ImportProductsCommand(stream, "sample-products.csv"), cancellationToken);
        LogSampleProductsSeeded(result.Created, result.Updated, result.Failed);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Database migrations applied")]
    private partial void LogMigrationsApplied();

    [LoggerMessage(Level = LogLevel.Information, Message = "Sample products seeded: {Created} created, {Updated} updated, {Failed} rejected")]
    private partial void LogSampleProductsSeeded(int created, int updated, int failed);
}
