using ECommerce.Application.Common.Models;
using ECommerce.Domain.Products;
using FluentValidation;

namespace ECommerce.Application.Common.Validation;

public static class ValidationRules
{
    public static IRuleBuilderOptions<T, string> ValidSku<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MaximumLength(ProductConstraints.SkuMaxLength)
            .Matches(ProductConstraints.SkuPattern)
            .WithMessage("SKU may only contain letters, digits, hyphens and underscores.");

    public static IRuleBuilderOptions<T, string> PlainText<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(value => value is null || value.IndexOfAny(['<', '>']) < 0)
            .WithMessage("{PropertyName} must not contain HTML or script markup.");

    public static IRuleBuilderOptions<T, string> RequiredPlainText<T>(this IRuleBuilder<T, string> rule, int maxLength) =>
        rule.Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("{PropertyName} is required.")
            .MaximumLength(maxLength)
            .PlainText();

    public static IRuleBuilderOptions<T, decimal> MaxDecimals<T>(this IRuleBuilder<T, decimal> rule, int decimals) =>
        rule.Must(value => decimal.Round(value, decimals) == value)
            .WithMessage($"{{PropertyName}} must not have more than {decimals} decimal places.");
}

public sealed class PagedRequestValidator : AbstractValidator<IPagedRequest>
{
    public PagedRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagingDefaults.MaxPageSize);
    }
}
