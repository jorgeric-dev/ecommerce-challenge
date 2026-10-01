using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using ECommerce.Application.Abstractions.Import;
using ECommerce.Application.Common.Exceptions;

namespace ECommerce.Infrastructure.Import;

internal sealed class CsvProductReader : IProductCsvReader
{
    public const int MaxRows = 10_000;

    private static readonly string[] RequiredColumns = ["name", "sku", "description", "category", "price", "stock", "weight_kg"];

    private static readonly CsvConfiguration Configuration = new(CultureInfo.InvariantCulture)
    {
        PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
        MissingFieldFound = null,
        BadDataFound = null,
        HeaderValidated = null,
        IgnoreBlankLines = true,
        TrimOptions = TrimOptions.None,
    };

    public async Task<IReadOnlyList<ProductCsvRecord>> ReadAsync(Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        using var streamReader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        using var csv = new CsvReader(streamReader, Configuration);

        try
        {
            if (!await csv.ReadAsync())
            {
                throw new InvalidFileException("The CSV file is empty.");
            }

            csv.ReadHeader();
            EnsureRequiredColumns(csv.HeaderRecord);

            var records = new List<ProductCsvRecord>();
            while (await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (records.Count >= MaxRows)
                {
                    throw new InvalidFileException($"The CSV file exceeds the maximum of {MaxRows} rows.");
                }

                records.Add(new ProductCsvRecord(
                    csv.Parser.Row,
                    csv.GetField("name"),
                    csv.GetField("sku"),
                    csv.GetField("description"),
                    csv.GetField("category"),
                    csv.GetField("price"),
                    csv.GetField("stock"),
                    csv.GetField("weight_kg")));
            }

            return records;
        }
        catch (CsvHelperException ex)
        {
            throw new InvalidFileException("The CSV file could not be parsed. Please verify its format.", ex);
        }
    }

    private static void EnsureRequiredColumns(string[]? header)
    {
        var columns = (header ?? []).Select(column => column.Trim().ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);
        var missing = RequiredColumns.Where(column => !columns.Contains(column)).ToList();

        if (missing.Count > 0)
        {
            throw new InvalidFileException($"The CSV file is missing required columns: {string.Join(", ", missing)}.");
        }
    }
}
