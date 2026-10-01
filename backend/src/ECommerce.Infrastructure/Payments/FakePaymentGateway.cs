using ECommerce.Application.Abstractions.Payments;

namespace ECommerce.Infrastructure.Payments;

internal sealed class FakePaymentGateway : IPaymentGateway
{
    public const string DeclinedCardNumber = "4000000000000002";
    public const string InsufficientFundsCardNumber = "4000000000009995";

    public Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var cardLast4 = request.CardNumber.Length >= 4 ? request.CardNumber[^4..] : request.CardNumber;

        var result = request.CardNumber switch
        {
            DeclinedCardNumber => PaymentResult.Declined("The card was declined by the issuer.", cardLast4),
            InsufficientFundsCardNumber => PaymentResult.Declined("The card has insufficient funds.", cardLast4),
            _ when request.Amount <= 0 => PaymentResult.Declined("The payment amount must be greater than zero.", cardLast4),
            _ => PaymentResult.Approved($"PAY-{Guid.NewGuid():N}", cardLast4),
        };

        return Task.FromResult(result);
    }
}
