using FluentValidation;

namespace ECommerce.Application.Products.Commands.ImportProducts;

public sealed record ImportProductsCommand(Stream Content, string FileName)
{
    public const long MaxFileSizeInBytes = 5 * 1024 * 1024;
}

public sealed class ImportProductsCommandValidator : AbstractValidator<ImportProductsCommand>
{
    public ImportProductsCommandValidator()
    {
        RuleFor(x => x.Content).NotNull().WithMessage("A CSV file is required.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(fileName => string.Equals(Path.GetExtension(fileName), ".csv", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .csv files can be imported.");

        RuleFor(x => x.Content)
            .Must(content => content.Length is > 0 and <= ImportProductsCommand.MaxFileSizeInBytes)
            .When(x => x.Content is { CanSeek: true })
            .WithMessage($"The file must not be empty and must not exceed {ImportProductsCommand.MaxFileSizeInBytes / (1024 * 1024)} MB.");
    }
}
