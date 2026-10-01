using ECommerce.Application.Common.Validation;
using ECommerce.Domain.Orders;
using FluentValidation;

namespace ECommerce.Application.Orders.Commands.PlaceOrder;

public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator(TimeProvider timeProvider)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Customer).NotNull().SetValidator(new CustomerDetailsValidator());

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("The order must contain at least one item.")
            .Must(items => items.Count <= OrderConstraints.MaxLinesPerOrder)
            .WithMessage($"An order cannot contain more than {OrderConstraints.MaxLinesPerOrder} lines.")
            .Must(items => items.Select(item => item.ProductId).Distinct().Count() == items.Count)
            .WithMessage("Each product may only appear once in the order.");

        RuleForEach(x => x.Items).SetValidator(new OrderLineValidator());

        RuleFor(x => x.Payment).NotNull().SetValidator(new PaymentCardValidator(timeProvider));
    }
}

public sealed class CustomerDetailsValidator : AbstractValidator<CustomerDetails>
{
    public CustomerDetailsValidator()
    {
        RuleFor(x => x.Name).RequiredPlainText(OrderConstraints.CustomerNameMaxLength);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(OrderConstraints.CustomerEmailMaxLength).EmailAddress();
        RuleFor(x => x.ShippingAddress).RequiredPlainText(OrderConstraints.ShippingAddressMaxLength);
    }
}

public sealed class OrderLineValidator : AbstractValidator<OrderLine>
{
    public OrderLineValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(1, OrderConstraints.MaxQuantityPerLine);
    }
}

public sealed class PaymentCardValidator : AbstractValidator<PaymentCard>
{
    public PaymentCardValidator(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        RuleFor(x => x.CardHolderName).RequiredPlainText(OrderConstraints.CustomerNameMaxLength);

        RuleFor(x => x.CardNumber)
            .NotEmpty()
            .Matches("^[0-9]{12,19}$").WithMessage("Card number must contain between 12 and 19 digits.")
            .Must(PassesLuhnCheck).WithMessage("Card number is invalid.");

        RuleFor(x => x.ExpiryMonth).InclusiveBetween(1, 12);

        RuleFor(x => x)
            .Must(card => !IsExpired(card, timeProvider.GetUtcNow()))
            .WithName("Expiry")
            .WithMessage("The card has expired.")
            .When(card => card.ExpiryMonth is >= 1 and <= 12);

        RuleFor(x => x.Cvv).NotEmpty().Matches("^[0-9]{3,4}$").WithMessage("CVV must contain 3 or 4 digits.");
    }

    public static bool PassesLuhnCheck(string? cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber) || !cardNumber.All(char.IsAsciiDigit))
        {
            return false;
        }

        var sum = 0;
        var doubleDigit = false;
        for (var i = cardNumber.Length - 1; i >= 0; i--)
        {
            var digit = cardNumber[i] - '0';
            if (doubleDigit)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
            doubleDigit = !doubleDigit;
        }

        return sum % 10 == 0;
    }

    private static bool IsExpired(PaymentCard card, DateTimeOffset now) =>
        card.ExpiryYear < now.Year || (card.ExpiryYear == now.Year && card.ExpiryMonth < now.Month);
}
