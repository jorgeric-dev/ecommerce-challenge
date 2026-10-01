using System.Text.Json;
using ECommerce.Api.Middleware;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ECommerce.Api.IntegrationTests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    public static TheoryData<Exception, int, string> Mappings() => new()
    {
        { new ValidationException(new Dictionary<string, string[]>()), 400, "Validation failed" },
        { new InvalidFileException("bad"), 400, "Invalid file" },
        { new NotFoundException("Product", 1), 404, "Resource not found" },
        { new InsufficientStockException("A", 2, 1), 409, "Insufficient stock" },
        { new ConflictException("conflict"), 409, "Conflict" },
        { new PaymentDeclinedException("no", "ORD-1"), 402, "Payment declined" },
        { new DomainException("rule"), 422, "Business rule violation" },
        { new BadHttpRequestException("too large", 413), 413, "Bad request" },
        { new OperationCanceledException(), ExceptionMapper.ClientClosedRequest, "Request cancelled" },
        { new InvalidOperationException("secret"), 500, "Internal server error" },
    };

    [Theory]
    [MemberData(nameof(Mappings))]
    public void ExceptionMapper_MapsExceptionsToStatusCodes(Exception exception, int expectedStatus, string expectedTitle)
    {
        var descriptor = ExceptionMapper.Map(exception);

        descriptor.StatusCode.ShouldBe(expectedStatus);
        descriptor.Title.ShouldBe(expectedTitle);
    }

    private static async Task<(HttpContext Context, JsonElement Body)> Invoke(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/test";
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(_ => throw exception, NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await JsonDocument.ParseAsync(context.Response.Body);
        return (context, body.RootElement);
    }

    [Fact]
    public async Task UnexpectedException_HidesInternalDetails()
    {
        var (context, body) = await Invoke(new InvalidOperationException("connection string leaked"));

        context.Response.StatusCode.ShouldBe(500);
        context.Response.ContentType.ShouldBe("application/problem+json");
        body.GetProperty("detail").GetString().ShouldBe(ExceptionHandlingMiddleware.UnexpectedErrorMessage);
        body.GetProperty("instance").GetString().ShouldBe("POST /api/v1/test");
        body.TryGetProperty("traceId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task ValidationException_IncludesErrors()
    {
        var (context, body) = await Invoke(new ValidationException(new Dictionary<string, string[]> { ["Name"] = ["Name is required."] }));

        context.Response.StatusCode.ShouldBe(400);
        body.GetProperty("errors").GetProperty("Name")[0].GetString().ShouldBe("Name is required.");
    }

    [Fact]
    public async Task PaymentDeclined_IncludesOrderNumber()
    {
        var (_, body) = await Invoke(new PaymentDeclinedException("Card declined", "ORD-42"));

        body.GetProperty("orderNumber").GetString().ShouldBe("ORD-42");
        body.GetProperty("detail").GetString().ShouldNotBeNull().ShouldContain("Card declined");
    }

    [Fact]
    public async Task WithoutException_PassesThrough()
    {
        var context = new DefaultHttpContext();
        var middleware = new ExceptionHandlingMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 204;
            return Task.CompletedTask;
        }, NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(204);
    }
}
