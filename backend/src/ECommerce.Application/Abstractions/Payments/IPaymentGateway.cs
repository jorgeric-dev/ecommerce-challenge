namespace ECommerce.Application.Abstractions.Payments;

public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken = default);
}

public sealed record PaymentRequest(
    string OrderNumber,
    decimal Amount,
    string Currency,
    string CardHolderName,
    string CardNumber,
    int ExpiryMonth,
    int ExpiryYear,
    string Cvv);

public sealed record PaymentResult(bool IsApproved, string? TransactionId, string CardLast4, string? FailureReason)
{
    public static PaymentResult Approved(string transactionId, string cardLast4) => new(true, transactionId, cardLast4, null);

    public static PaymentResult Declined(string reason, string cardLast4) => new(false, null, cardLast4, reason);
}
