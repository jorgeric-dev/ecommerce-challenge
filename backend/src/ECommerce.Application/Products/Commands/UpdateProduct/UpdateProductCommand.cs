using FluentValidation;

namespace ECommerce.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Description,
    int CategoryId,
    decimal Price,
    int Stock,
    decimal WeightKg) : IProductDetails;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator(IValidator<IProductDetails> productDetailsValidator)
    {
        RuleFor(x => x.Id).NotEmpty();

        Include(productDetailsValidator);
    }
}
