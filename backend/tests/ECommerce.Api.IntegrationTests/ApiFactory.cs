using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ECommerce.Application.Products;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace ECommerce.Api.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _connectionString = $"DataSource=api-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;

    public ApiFactory()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ECommerce", _connectionString);
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("Database:SeedSampleProducts", "false");
        builder.UseSetting("Swagger:Enabled", "false");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAlive.Dispose();
        }
    }

    public async Task<ProductDto> CreateProductAsync(HttpClient client, string sku, int stock = 10, decimal price = 25m, string name = "Test Product")
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/products",
            new { name, sku, description = "Integration test product", categoryId = 5, price, stock, weightKg = 0.5m },
            Json);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>(Json))!;
    }
}
