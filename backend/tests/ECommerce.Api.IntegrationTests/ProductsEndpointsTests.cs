using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products;
using ECommerce.Application.Products.Commands.ImportProducts;
using Shouldly;

namespace ECommerce.Api.IntegrationTests;

public sealed class ProductsEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ProductsEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CrudLifecycle_CreateReadUpdateDelete()
    {
        var created = await _factory.CreateProductAsync(_client, "crud-1");
        created.Sku.ShouldBe("CRUD-1");
        created.CategoryName.ShouldBe("Electronics");

        var fetched = await _client.GetFromJsonAsync<ProductDto>($"/api/v1/products/{created.Id}", ApiFactory.Json);
        fetched.ShouldNotBeNull().Name.ShouldBe("Test Product");

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/v1/products/{created.Id}",
            new { name = "Renamed", description = (string?)null, categoryId = 7, price = 30.5m, stock = 3, weightKg = 1.25m },
            ApiFactory.Json);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProductDto>(ApiFactory.Json);
        updated.ShouldNotBeNull().Name.ShouldBe("Renamed");
        updated.CategoryName.ShouldBe("Footwear");
        updated.UpdatedAtUtc.ShouldNotBeNull();

        (await _client.DeleteAsync($"/api/v1/products/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _client.GetAsync($"/api/v1/products/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ReturnsCreatedWithLocationHeader()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/products",
            new { name = "Located", sku = "LOC-1", categoryId = 5, price = 1m, stock = 1, weightKg = 0m },
            ApiFactory.Json);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>(ApiFactory.Json);
        response.Headers.Location.ShouldNotBeNull().ToString().ShouldEndWith($"/api/v1/products/{product!.Id}");
    }

    [Fact]
    public async Task Create_InvalidPayload_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/products",
            new { name = "<b>bad</b>", sku = "bad sku", categoryId = 999, price = -1m, stock = -1, weightKg = 1m },
            ApiFactory.Json);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(ApiFactory.Json);
        problem.ShouldNotBeNull().Title.ShouldBe("Validation failed");
        problem.TraceId.ShouldNotBeNullOrEmpty();
        problem.Errors.ShouldNotBeNull().Keys.ShouldBe(["Name", "Sku", "CategoryId", "Price", "Stock"], ignoreOrder: true);
    }

    [Fact]
    public async Task Create_MalformedJson_ReturnsConsistentValidationProblem()
    {
        using var content = new StringContent("{\"name\":", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1/products", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(ApiFactory.Json);
        problem.ShouldNotBeNull().Title.ShouldBe("Validation failed");
        problem.Errors.ShouldNotBeNull().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Create_DuplicateSku_ReturnsConflict()
    {
        await _factory.CreateProductAsync(_client, "DUP-1");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/products",
            new { name = "Again", sku = "dup-1", categoryId = 5, price = 1m, stock = 1, weightKg = 0m },
            ApiFactory.Json);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync<ProblemResponse>(ApiFactory.Json))!.Detail.ShouldBe("A product with SKU 'DUP-1' already exists.");
    }

    [Fact]
    public async Task Update_UnknownProduct_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/v1/products/{Guid.NewGuid()}",
            new { name = "X", categoryId = 5, price = 1m, stock = 1, weightKg = 0m },
            ApiFactory.Json);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_UnknownProduct_ReturnsNotFound()
    {
        (await _client.DeleteAsync($"/api/v1/products/{Guid.NewGuid()}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_WithCamelCaseQueryParameters_FiltersAndPages()
    {
        await _factory.CreateProductAsync(_client, "SRCH-1", price: 5m, name: "Searchable Lamp");
        await _factory.CreateProductAsync(_client, "SRCH-2", price: 50m, name: "Searchable Lamp Deluxe");
        await _factory.CreateProductAsync(_client, "SRCH-3", stock: 0, price: 7m, name: "Searchable Lamp Mini");

        var result = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            "/api/v1/products?search=searchable&maxPrice=10&inStockOnly=true&sortBy=price&sortDirection=desc&page=1&pageSize=5",
            ApiFactory.Json);

        result.ShouldNotBeNull().Items.ShouldHaveSingleItem().Sku.ShouldBe("SRCH-1");
    }

    [Fact]
    public async Task Search_InvalidPaging_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/v1/products?pageSize=1000");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ProblemResponse>(ApiFactory.Json))!.Errors!.ShouldContainKey("PageSize");
    }

    [Fact]
    public async Task Import_ValidCsv_ReturnsSummary()
    {
        const string csv = "name,sku,description,category,price,stock,weight_kg\n" +
                           "Imported Mouse,IMP-1,Mouse,Electronics,$19.99,10,0.1\n" +
                           "Broken Mat,IMP-2,Mat,Sports,free,10,1\n" +
                           ",,,,,,\n";

        var response = await _client.PostAsync("/api/v1/products/import", CsvUpload(csv, "products.csv"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProductImportResult>(ApiFactory.Json);
        result.ShouldNotBeNull();
        result.Created.ShouldBe(1);
        result.Failed.ShouldBe(1);
        result.Skipped.ShouldBe(1);
        result.Errors.ShouldHaveSingleItem().LineNumber.ShouldBe(3);
    }

    [Fact]
    public async Task Import_NonCsvFile_ReturnsBadRequest()
    {
        var response = await _client.PostAsync("/api/v1/products/import", CsvUpload("a,b", "products.txt"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Import_MissingColumns_ReturnsInvalidFileProblem()
    {
        var response = await _client.PostAsync("/api/v1/products/import", CsvUpload("name,sku\nA,B\n", "products.csv"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(ApiFactory.Json);
        problem!.Title.ShouldBe("Invalid file");
        problem.Detail.ShouldNotBeNull().ShouldContain("missing required columns");
    }

    [Fact]
    public async Task Import_WithoutFile_ReturnsBadRequest()
    {
        using var form = new MultipartFormDataContent();

        var response = await _client.PostAsync("/api/v1/products/import", form);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static MultipartFormDataContent CsvUpload(string content, string fileName)
    {
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        return new MultipartFormDataContent { { file, "file", fileName } };
    }
}
