using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common.Validation;
using ECommerce.Domain.Products;
using FluentValidation;

namespace ECommerce.Application.Products;

public sealed class ProductDetailsValidator : AbstractValidator<IProductDetails>
{
    public ProductDetailsValidator(ICategoryCatalog categoryCatalog)
    {
        RuleFor(x => x.Name)
            .RequiredPlainText(ProductConstraints.NameMaxLength);

        RuleFor(x => x.Description!)
            .MaximumLength(ProductConstraints.DescriptionMaxLength)
            .PlainText()
            .When(x => x.Description is not null);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .MustAsync(async (categoryId, cancellationToken) =>
                await categoryCatalog.FindByIdAsync(categoryId, cancellationToken) is not null)
            .WithMessage("Category '{PropertyValue}' does not exist.");

        RuleFor(x => x.Price)
            .InclusiveBetween(ProductConstraints.MinPrice, ProductConstraints.MaxPrice)
            .MaxDecimals(ProductConstraints.PriceScale);

        RuleFor(x => x.Stock)
            .InclusiveBetween(ProductConstraints.MinStock, ProductConstraints.MaxStock);

        RuleFor(x => x.WeightKg)
            .InclusiveBetween(ProductConstraints.MinWeightKg, ProductConstraints.MaxWeightKg)
            .MaxDecimals(ProductConstraints.WeightScale);
    }
}
