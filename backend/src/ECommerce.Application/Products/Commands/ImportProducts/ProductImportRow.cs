using System.Globalization;
using ECommerce.Application.Abstractions.Import;
using ECommerce.Application.Categories;
using ECommerce.Application.Common.Validation;
using FluentValidation;

namespace ECommerce.Application.Products.Commands.ImportProducts;

public sealed record ProductImportRow(
    int LineNumber,
    string Name,
    string Sku,
    string? Description,
    int CategoryId,
    decimal Price,
    int Stock,
    decimal WeightKg) : IProductDetails;

public sealed class ProductImportRowValidator : AbstractValidator<ProductImportRow>
{
    public ProductImportRowValidator(IValidator<IProductDetails> productDetailsValidator)
    {
        Include(productDetailsValidator);

        RuleFor(x => x.Sku).ValidSku();
    }
}

public static class ProductImportRowParser
{
    private static readonly char[] CurrencySymbols = ['$', '€', '£'];

    public static (ProductImportRow? Row, IReadOnlyList<string> Errors) Parse(
        ProductCsvRecord record,
        IReadOnlyDictionary<string, CategoryDto> categoriesByName)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(categoriesByName);

        var errors = new List<string>();

        var categoryId = 0;
        if (string.IsNullOrWhiteSpace(record.Category))
        {
            errors.Add("Category is required.");
        }
        else if (categoriesByName.TryGetValue(record.Category.Trim(), out var category))
        {
            categoryId = category.Id;
        }
        else
        {
            errors.Add($"Category '{record.Category.Trim()}' does not exist.");
        }

        var price = ParseDecimal(record.Price, "Price", errors, allowCurrencySymbol: true);
        var stock = ParseInt(record.Stock, "Stock", errors);
        var weight = ParseDecimal(record.WeightKg, "Weight", errors, allowCurrencySymbol: false);

        if (errors.Count > 0)
        {
            return (null, errors);
        }

        var row = new ProductImportRow(
            record.LineNumber,
            record.Name?.Trim() ?? string.Empty,
            record.Sku?.Trim() ?? string.Empty,
            string.IsNullOrWhiteSpace(record.Description) ? null : record.Description.Trim(),
            categoryId,
            price,
            stock,
            weight);

        return (row, errors);
    }

    private static decimal ParseDecimal(string? raw, string fieldName, List<string> errors, bool allowCurrencySymbol)
    {
        var value = raw?.Trim() ?? string.Empty;
        if (allowCurrencySymbol)
        {
            value = value.TrimStart(CurrencySymbols).Trim();
        }

        if (value.Length == 0)
        {
            errors.Add($"{fieldName} is required.");
            return 0;
        }

        if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result))
        {
            errors.Add($"{fieldName} '{raw}' is not a valid number.");
        }

        return result;
    }

    private static int ParseInt(string? raw, string fieldName, List<string> errors)
    {
        var value = raw?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            errors.Add($"{fieldName} is required.");
            return 0;
        }

        if (!int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result))
        {
            errors.Add($"{fieldName} '{raw}' is not a valid whole number.");
        }

        return result;
    }
}
