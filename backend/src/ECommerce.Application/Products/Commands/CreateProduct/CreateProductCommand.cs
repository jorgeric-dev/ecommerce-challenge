using ECommerce.Application.Common.Validation;
using FluentValidation;

namespace ECommerce.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Sku,
    string? Description,
    int CategoryId,
    decimal Price,
    int Stock,
    decimal WeightKg) : IProductDetails;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator(IValidator<IProductDetails> productDetailsValidator)
    {
        Include(productDetailsValidator);

        RuleFor(x => x.Sku).ValidSku();
    }
}
