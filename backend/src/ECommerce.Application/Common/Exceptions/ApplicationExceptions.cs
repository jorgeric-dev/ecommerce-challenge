namespace ECommerce.Application.Common.Exceptions;

public abstract class AppException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class NotFoundException(string resourceName, object key)
    : AppException($"{resourceName} with id '{key}' was not found.");

public sealed class ConflictException(string message, Exception? innerException = null)
    : AppException(message, innerException);

public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : AppException(DefaultMessage)
{
    public const string DefaultMessage = "One or more validation errors occurred.";

    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class InvalidFileException(string message, Exception? innerException = null)
    : AppException(message, innerException);

public sealed class PaymentDeclinedException(string reason, string orderNumber)
    : AppException($"Payment for order {orderNumber} was declined: {reason}")
{
    public string Reason { get; } = reason;

    public string OrderNumber { get; } = orderNumber;
}
