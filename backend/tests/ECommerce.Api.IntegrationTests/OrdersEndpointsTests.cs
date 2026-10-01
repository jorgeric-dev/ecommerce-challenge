using System.Net;
using System.Net.Http.Json;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders;
using ECommerce.Application.Products;
using ECommerce.Domain.Orders;
using Shouldly;

namespace ECommerce.Api.IntegrationTests;

public sealed class OrdersEndpointsTests : IClassFixture<ApiFactory>
{
    private const string ApprovedCard = "4242424242424242";
    private const string DeclinedCard = "4000000000000002";

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public OrdersEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private Task<HttpResponseMessage> PlaceOrder(Guid productId, int quantity, string cardNumber = ApprovedCard, string email = "buyer@example.com") =>
        _client.PostAsJsonAsync(
            "/api/v1/orders",
            new
            {
                customer = new { name = "Jane Buyer", email, shippingAddress = "1 Main Street" },
                items = new[] { new { productId, quantity } },
                payment = new { cardHolderName = "Jane Buyer", cardNumber, expiryMonth = 12, expiryYear = 2035, cvv = "123" },
            },
            ApiFactory.Json);

    private async Task<int> StockOf(Guid productId) =>
        (await _client.GetFromJsonAsync<ProductDto>($"/api/v1/products/{productId}", ApiFactory.Json))!.Stock;

    [Fact]
    public async Task PlaceOrder_WithApprovedCard_CreatesPaidOrderAndDecreasesStock()
    {
        var product = await _factory.CreateProductAsync(_client, "ORD-OK", stock: 5, price: 12.50m);

        var response = await PlaceOrder(product.Id, 2);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>(ApiFactory.Json);
        order.ShouldNotBeNull().Status.ShouldBe(OrderStatus.Paid);
        order.TotalAmount.ShouldBe(25m);
        order.CardLast4.ShouldBe("4242");
        response.Headers.Location!.ToString().ShouldEndWith($"/api/v1/orders/{order.Id}");
        (await StockOf(product.Id)).ShouldBe(3);

        var fetched = await _client.GetFromJsonAsync<OrderDto>($"/api/v1/orders/{order.Id}", ApiFactory.Json);
        fetched.ShouldNotBeNull().Items.ShouldHaveSingleItem().Quantity.ShouldBe(2);
    }

    [Fact]
    public async Task PlaceOrder_WithDeclinedCard_Returns402AndRestoresStock()
    {
        var product = await _factory.CreateProductAsync(_client, "ORD-DECL", stock: 5);

        var response = await PlaceOrder(product.Id, 2, DeclinedCard);

        response.StatusCode.ShouldBe(HttpStatusCode.PaymentRequired);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(ApiFactory.Json);
        problem!.Title.ShouldBe("Payment declined");
        problem.OrderNumber.ShouldNotBeNull().ShouldStartWith("ORD-");
        (await StockOf(product.Id)).ShouldBe(5);
    }

    [Fact]
    public async Task PlaceOrder_ExceedingStock_ReturnsConflict()
    {
        var product = await _factory.CreateProductAsync(_client, "ORD-LOW", stock: 1);

        var response = await PlaceOrder(product.Id, 2);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(ApiFactory.Json);
        problem!.Title.ShouldBe("Insufficient stock");
        problem.Detail.ShouldBe("Insufficient stock for product 'ORD-LOW'. Requested 2, available 1.");
    }

    [Fact]
    public async Task PlaceOrder_UnknownProduct_ReturnsNotFound()
    {
        (await PlaceOrder(Guid.NewGuid(), 1)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PlaceOrder_InvalidRequest_ReturnsValidationErrorsForNestedFields()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/orders",
            new
            {
                customer = new { name = "", email = "nope", shippingAddress = "x" },
                items = Array.Empty<object>(),
                payment = new { cardHolderName = "J", cardNumber = "123", expiryMonth = 1, expiryYear = 2020, cvv = "1" },
            },
            ApiFactory.Json);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(ApiFactory.Json);
        problem!.Errors!.Keys.ShouldBeSubsetOf(["Customer.Name", "Customer.Email", "Items", "Payment.CardNumber", "Payment.Expiry", "Payment.Cvv"]);
        problem.Errors.Count.ShouldBe(6);
    }

    [Fact]
    public async Task GetOrders_FiltersByCustomerEmail()
    {
        var product = await _factory.CreateProductAsync(_client, "ORD-LIST", stock: 10);
        await PlaceOrder(product.Id, 1, email: "list-owner@example.com");
        await PlaceOrder(product.Id, 1, email: "someone-else@example.com");

        var result = await _client.GetFromJsonAsync<PagedResult<OrderSummaryDto>>(
            "/api/v1/orders?customerEmail=list-owner@example.com",
            ApiFactory.Json);

        result.ShouldNotBeNull().Items.ShouldHaveSingleItem().CustomerEmail.ShouldBe("list-owner@example.com");
    }

    [Fact]
    public async Task GetOrder_Unknown_ReturnsNotFound()
    {
        (await _client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
