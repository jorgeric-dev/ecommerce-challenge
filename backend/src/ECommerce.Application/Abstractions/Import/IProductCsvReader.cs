namespace ECommerce.Application.Abstractions.Import;

public interface IProductCsvReader
{
    Task<IReadOnlyList<ProductCsvRecord>> ReadAsync(Stream content, CancellationToken cancellationToken = default);
}

public sealed record ProductCsvRecord(
    int LineNumber,
    string? Name,
    string? Sku,
    string? Description,
    string? Category,
    string? Price,
    string? Stock,
    string? WeightKg)
{
    public bool IsBlank =>
        string.IsNullOrWhiteSpace(Name)
        && string.IsNullOrWhiteSpace(Sku)
        && string.IsNullOrWhiteSpace(Description)
        && string.IsNullOrWhiteSpace(Category)
        && string.IsNullOrWhiteSpace(Price)
        && string.IsNullOrWhiteSpace(Stock)
        && string.IsNullOrWhiteSpace(WeightKg);
}
