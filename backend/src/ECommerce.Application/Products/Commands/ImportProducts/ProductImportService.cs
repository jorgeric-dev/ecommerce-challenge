using ECommerce.Application.Abstractions.Import;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common.Validation;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Products;
using FluentValidation;

namespace ECommerce.Application.Products.Commands.ImportProducts;

public interface IProductImportService
{
    Task<ProductImportResult> ImportAsync(ImportProductsCommand command, CancellationToken cancellationToken = default);
}

public sealed class ProductImportService(
    IProductCsvReader csvReader,
    IProductRepository productRepository,
    ICategoryCatalog categoryCatalog,
    IValidator<ImportProductsCommand> commandValidator,
    IValidator<ProductImportRow> rowValidator,
    IUnitOfWork unitOfWork) : IProductImportService
{
    public async Task<ProductImportResult> ImportAsync(ImportProductsCommand command, CancellationToken cancellationToken = default)
    {
        await commandValidator.EnsureValidAsync(command, cancellationToken);

        var records = await csvReader.ReadAsync(command.Content, cancellationToken);
        var categoriesByName = await categoryCatalog.GetByNameAsync(cancellationToken);
        var productsBySku = await LoadExistingProductsAsync(records, cancellationToken);

        var errors = new List<ProductImportIssue>();
        var warnings = new List<ProductImportIssue>();
        var lastLineBySku = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int created = 0, updated = 0, skipped = 0;

        foreach (var record in records)
        {
            if (record.IsBlank)
            {
                skipped++;
                continue;
            }

            var (row, parseErrors) = ProductImportRowParser.Parse(record, categoriesByName);
            if (row is null)
            {
                errors.Add(new ProductImportIssue(record.LineNumber, record.Sku?.Trim(), parseErrors));
                continue;
            }

            var validation = await rowValidator.ValidateAsync(row, cancellationToken);
            if (!validation.IsValid)
            {
                errors.Add(new ProductImportIssue(row.LineNumber, row.Sku, validation.Errors.Select(e => e.ErrorMessage).ToList()));
                continue;
            }

            try
            {
                var sku = Product.NormalizeSku(row.Sku);
                if (productsBySku.TryGetValue(sku, out var product))
                {
                    product.Update(row.Name, row.Description, row.CategoryId, row.Price, row.Stock, row.WeightKg);
                    updated++;
                }
                else
                {
                    product = Product.Create(row.Name, sku, row.Description, row.CategoryId, row.Price, row.Stock, row.WeightKg);
                    productRepository.Add(product);
                    productsBySku[sku] = product;
                    created++;
                }

                if (lastLineBySku.TryGetValue(sku, out var previousLine))
                {
                    warnings.Add(new ProductImportIssue(row.LineNumber, sku, [$"Duplicate SKU in file; this row overrides line {previousLine}."]));
                }

                lastLineBySku[sku] = row.LineNumber;
            }
            catch (DomainException ex)
            {
                errors.Add(new ProductImportIssue(row.LineNumber, row.Sku, [ex.Message]));
            }
        }

        if (created + updated > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new ProductImportResult(
            records.Count - skipped,
            created,
            updated,
            skipped,
            errors.Count,
            errors,
            warnings);
    }

    private async Task<Dictionary<string, Product>> LoadExistingProductsAsync(
        IReadOnlyList<ProductCsvRecord> records,
        CancellationToken cancellationToken)
    {
        var skus = records
            .Select(record => record.Sku?.Trim().ToUpperInvariant())
            .Where(sku => !string.IsNullOrEmpty(sku))
            .Select(sku => sku!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var existing = skus.Count == 0
            ? new Dictionary<string, Product>()
            : await productRepository.GetBySkusAsync(skus, cancellationToken);

        return new Dictionary<string, Product>(existing, StringComparer.OrdinalIgnoreCase);
    }
}
