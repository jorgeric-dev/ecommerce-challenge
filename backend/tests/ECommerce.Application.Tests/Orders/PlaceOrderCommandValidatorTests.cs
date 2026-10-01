using ECommerce.Application.Orders.Commands.PlaceOrder;
using ECommerce.Domain.Orders;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace ECommerce.Application.Tests.Orders;

public class PlaceOrderCommandValidatorTests
{
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 9, 30, 0, 0, 0, TimeSpan.Zero));
    private static readonly PlaceOrderCommandValidator Validator = new(Clock);
    private static readonly Guid ProductId = Guid.NewGuid();

    private static PlaceOrderCommand Valid() => new(
        new CustomerDetails("Jane Doe", "jane@example.com", "1 Main St"),
        [new OrderLine(ProductId, 2)],
        new PaymentCard("Jane Doe", "4242424242424242", 12, 2030, "123"));

    [Fact]
    public void ValidCommand_Passes()
    {
        Validator.Validate(Valid()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void CardExpiringThisMonth_IsStillValid()
    {
        var command = Valid() with { Payment = Valid().Payment with { ExpiryMonth = 9, ExpiryYear = 2026 } };

        Validator.Validate(command).IsValid.ShouldBeTrue();
    }

    public static TheoryData<PlaceOrderCommand, string> InvalidCommands()
    {
        var valid = Valid();
        return new TheoryData<PlaceOrderCommand, string>
        {
            { valid with { Customer = null! }, "Customer" },
            { valid with { Customer = valid.Customer with { Name = "" } }, "Customer.Name" },
            { valid with { Customer = valid.Customer with { Email = "not-an-email" } }, "Customer.Email" },
            { valid with { Customer = valid.Customer with { ShippingAddress = "<b>x</b>" } }, "Customer.ShippingAddress" },
            { valid with { Items = [] }, "Items" },
            { valid with { Items = null! }, "Items" },
            { valid with { Items = [new OrderLine(ProductId, 1), new OrderLine(ProductId, 2)] }, "Items" },
            { valid with { Items = Enumerable.Range(0, OrderConstraints.MaxLinesPerOrder + 1).Select(_ => new OrderLine(Guid.NewGuid(), 1)).ToList() }, "Items" },
            { valid with { Items = [new OrderLine(Guid.Empty, 1)] }, "Items[0].ProductId" },
            { valid with { Items = [new OrderLine(ProductId, 0)] }, "Items[0].Quantity" },
            { valid with { Items = [new OrderLine(ProductId, OrderConstraints.MaxQuantityPerLine + 1)] }, "Items[0].Quantity" },
            { valid with { Payment = null! }, "Payment" },
            { valid with { Payment = valid.Payment with { CardHolderName = " " } }, "Payment.CardHolderName" },
            { valid with { Payment = valid.Payment with { CardNumber = "4242-4242" } }, "Payment.CardNumber" },
            { valid with { Payment = valid.Payment with { CardNumber = "4242424242424241" } }, "Payment.CardNumber" },
            { valid with { Payment = valid.Payment with { ExpiryMonth = 13 } }, "Payment.ExpiryMonth" },
            { valid with { Payment = valid.Payment with { ExpiryMonth = 8, ExpiryYear = 2026 } }, "Payment.Expiry" },
            { valid with { Payment = valid.Payment with { ExpiryYear = 2025 } }, "Payment.Expiry" },
            { valid with { Payment = valid.Payment with { Cvv = "12" } }, "Payment.Cvv" },
            { valid with { Payment = valid.Payment with { Cvv = "12a4" } }, "Payment.Cvv" },
        };
    }

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public void InvalidCommand_FailsOnExpectedProperty(PlaceOrderCommand command, string property)
    {
        var result = Validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(error => error.PropertyName).ShouldContain(property);
    }

    [Theory]
    [InlineData("4242424242424242", true)]
    [InlineData("4000000000000002", true)]
    [InlineData("5555555555554444", true)]
    [InlineData("4242424242424241", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("42424242424242a2", false)]
    public void LuhnCheck_ValidatesCardNumbers(string? cardNumber, bool expected)
    {
        PaymentCardValidator.PassesLuhnCheck(cardNumber).ShouldBe(expected);
    }
}
