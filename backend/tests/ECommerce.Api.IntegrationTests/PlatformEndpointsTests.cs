using System.Net;
using System.Net.Http.Json;
using ECommerce.Api.Middleware;
using ECommerce.Application.Categories;
using Shouldly;

namespace ECommerce.Api.IntegrationTests;

public sealed class PlatformEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetCategories_ReturnsSeededCatalog()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryDto>>("/api/v1/categories", ApiFactory.Json);

        categories.ShouldNotBeNull().Count.ShouldBe(18);
        categories.ShouldContain(new CategoryDto(11, "Home & Office"));
    }

    [Fact]
    public async Task Health_ReportsHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Fact]
    public async Task OpenApiDocument_DescribesVersionedEndpoints()
    {
        var document = await _client.GetStringAsync("/openapi/v1.json");

        document.ShouldContain("\"/api/v1/products\"");
        document.ShouldContain("\"/api/v1/orders\"");
        document.ShouldContain("ECommerce API");
    }

    [Fact]
    public async Task UnsupportedApiVersion_IsRejected()
    {
        (await _client.GetAsync("/api/v2/products")).IsSuccessStatusCode.ShouldBeFalse();
    }

    [Fact]
    public async Task Responses_IncludeApiVersionAndCorrelationHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/categories");
        request.Headers.Add(RequestLoggingMiddleware.CorrelationIdHeader, "client-correlation-123");

        var response = await _client.SendAsync(request);

        response.Headers.GetValues(RequestLoggingMiddleware.CorrelationIdHeader).ShouldBe(["client-correlation-123"]);
        response.Headers.GetValues("api-supported-versions").ShouldContain("1.0");
    }

    [Fact]
    public async Task UnsafeCorrelationId_IsReplaced()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/categories");
        request.Headers.Add(RequestLoggingMiddleware.CorrelationIdHeader, "bad value with spaces");

        var response = await _client.SendAsync(request);

        response.Headers.GetValues(RequestLoggingMiddleware.CorrelationIdHeader).Single().ShouldNotBe("bad value with spaces");
    }
}
