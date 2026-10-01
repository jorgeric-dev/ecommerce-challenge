using ECommerce.Application.Common.Validation;
using FluentValidation;
using Shouldly;
using ValidationException = ECommerce.Application.Common.Exceptions.ValidationException;

namespace ECommerce.Application.Tests.Common;

public class ValidatorExtensionsTests
{
    public sealed record SampleInput(string Name, int Quantity);

    private sealed class SampleValidator : AbstractValidator<SampleInput>
    {
        public SampleValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.Name).MaximumLength(3).WithMessage("Name is too long.");
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be positive.");
        }
    }

    [Fact]
    public async Task EnsureValidAsync_WithValidInstance_DoesNotThrow()
    {
        await new SampleValidator().EnsureValidAsync(new SampleInput("abc", 1));
    }

    [Fact]
    public async Task EnsureValidAsync_WithFailures_ThrowsErrorsGroupedByProperty()
    {
        var ex = await Should.ThrowAsync<ValidationException>(() =>
            new SampleValidator().EnsureValidAsync(new SampleInput("", 0)));

        ex.Errors["Name"].ShouldBe(["Name is required."]);
        ex.Errors["Quantity"].ShouldBe(["Quantity must be positive."]);
        ex.Message.ShouldBe(ValidationException.DefaultMessage);
    }

    [Fact]
    public async Task EnsureValidAsync_GroupsMultipleMessagesForSameProperty()
    {
        var ex = await Should.ThrowAsync<ValidationException>(() =>
            new SampleValidator().EnsureValidAsync(new SampleInput("abcdef", 1)));

        ex.Errors.Keys.ShouldBe(["Name"]);
        ex.Errors["Name"].ShouldBe(["Name is too long."]);
    }

    [Fact]
    public async Task EnsureValidAsync_WithNullValidator_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(() =>
            ValidatorExtensions.EnsureValidAsync<SampleInput>(null!, new SampleInput("a", 1)));
    }
}
