namespace ECommerce.Infrastructure.Persistence;

public enum DatabaseProvider
{
    SqlServer,
    Sqlite,
}

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public const string ConnectionStringName = "ECommerce";

    public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;

    public bool ApplyMigrationsOnStartup { get; set; } = true;

    public bool SeedSampleProducts { get; set; } = true;

    public int MaxRetryCount { get; set; } = 5;
}

public sealed class CatalogCacheOptions
{
    public const string SectionName = "CatalogCache";

    public TimeSpan CategoriesTimeToLive { get; set; } = TimeSpan.FromMinutes(30);
}
