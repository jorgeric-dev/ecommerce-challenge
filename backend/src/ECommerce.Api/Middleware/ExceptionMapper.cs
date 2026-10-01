using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Exceptions;

namespace ECommerce.Api.Middleware;

public sealed record ErrorDescriptor(int StatusCode, string Title, bool ExposeMessage);

public static class ExceptionMapper
{
    public const int ClientClosedRequest = 499;

    public static ErrorDescriptor Map(Exception exception) => exception switch
    {
        ValidationException => new(StatusCodes.Status400BadRequest, "Validation failed", true),
        InvalidFileException => new(StatusCodes.Status400BadRequest, "Invalid file", true),
        NotFoundException => new(StatusCodes.Status404NotFound, "Resource not found", true),
        InsufficientStockException => new(StatusCodes.Status409Conflict, "Insufficient stock", true),
        ConflictException => new(StatusCodes.Status409Conflict, "Conflict", true),
        PaymentDeclinedException => new(StatusCodes.Status402PaymentRequired, "Payment declined", true),
        DomainException => new(StatusCodes.Status422UnprocessableEntity, "Business rule violation", true),
        BadHttpRequestException badRequest => new(badRequest.StatusCode, "Bad request", true),
        OperationCanceledException => new(ClientClosedRequest, "Request cancelled", false),
        _ => new(StatusCodes.Status500InternalServerError, "Internal server error", false),
    };
}
