namespace ECommerce.Application.Products.Commands.ImportProducts;

public sealed record ProductImportResult(
    int TotalRows,
    int Created,
    int Updated,
    int Skipped,
    int Failed,
    IReadOnlyList<ProductImportIssue> Errors,
    IReadOnlyList<ProductImportIssue> Warnings);

public sealed record ProductImportIssue(int LineNumber, string? Sku, IReadOnlyList<string> Messages);
