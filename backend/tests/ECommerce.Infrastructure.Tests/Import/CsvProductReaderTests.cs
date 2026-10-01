using System.Text;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Infrastructure.Import;
using ECommerce.Infrastructure.Persistence.Seeding;
using Shouldly;

namespace ECommerce.Infrastructure.Tests.Import;

public class CsvProductReaderTests
{
    private const string Header = "name,sku,description,category,price,stock,weight_kg";

    private readonly CsvProductReader _reader = new();

    private static MemoryStream Csv(string content) => new(Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task ReadAsync_SampleFile_ReadsAllRowsIncludingEdgeCases()
    {
        await using var stream = typeof(DatabaseInitializer).Assembly.GetManifestResourceStream(DatabaseInitializer.SampleProductsResourceName)!;

        var records = await _reader.ReadAsync(stream);

        records.Count.ShouldBe(97);
        records[0].ShouldBe(new Application.Abstractions.Import.ProductCsvRecord(2, "Running Shoes", "RS-001", "Lightweight running shoes for daily training", "Footwear", "89.99", "150", "0.35"));
        records.ShouldContain(record => record.Name == "Comma, In Product Name");
        records.ShouldContain(record => record.Name == "Quote \"Inside\" Name");
        records.Count(record => record.IsBlank).ShouldBe(2);
    }

    [Fact]
    public async Task ReadAsync_QuotedFieldsWithCommas_AreParsedAsSingleValue()
    {
        var records = await _reader.ReadAsync(Csv($"{Header}\nMug,TM-1,\"Insulated, 350ml\",Kitchen,16.99,320,0.3\n"));

        records.ShouldHaveSingleItem().Description.ShouldBe("Insulated, 350ml");
    }

    [Fact]
    public async Task ReadAsync_HeaderIsCaseAndWhitespaceInsensitiveAndColumnOrderFree()
    {
        var records = await _reader.ReadAsync(Csv(" SKU , Name,Category,Description,Price,Stock,Weight_KG\nA-1,Alpha,Books,Desc,1.00,2,0.5\n"));

        var record = records.ShouldHaveSingleItem();
        record.Sku.ShouldBe("A-1");
        record.Name.ShouldBe("Alpha");
        record.WeightKg.ShouldBe("0.5");
    }

    [Fact]
    public async Task ReadAsync_RowsWithMissingTrailingFields_ReturnNulls()
    {
        var records = await _reader.ReadAsync(Csv($"{Header}\nKeyboard,GK-088,RGB,Electronics,129.99\n"));

        var record = records.ShouldHaveSingleItem();
        record.Stock.ShouldBeNull();
        record.WeightKg.ShouldBeNull();
    }

    [Fact]
    public async Task ReadAsync_ReportsSpreadsheetLineNumbers()
    {
        var records = await _reader.ReadAsync(Csv($"{Header}\nA,A-1,,Books,1,1,1\nB,B-1,,Books,1,1,1\n"));

        records.Select(record => record.LineNumber).ShouldBe([2, 3]);
    }

    [Fact]
    public async Task ReadAsync_WithUtf8Bom_ReadsHeaderCorrectly()
    {
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes($"{Header}\nA,A-1,,Books,1,1,1\n")).ToArray();

        var records = await _reader.ReadAsync(new MemoryStream(bytes));

        records.ShouldHaveSingleItem().Name.ShouldBe("A");
    }

    [Fact]
    public async Task ReadAsync_HeaderOnly_ReturnsNoRecords()
    {
        (await _reader.ReadAsync(Csv(Header))).ShouldBeEmpty();
    }

    [Fact]
    public async Task ReadAsync_EmptyFile_ThrowsInvalidFile()
    {
        var ex = await Should.ThrowAsync<InvalidFileException>(() => _reader.ReadAsync(Csv(string.Empty)));

        ex.Message.ShouldBe("The CSV file is empty.");
    }

    [Fact]
    public async Task ReadAsync_MissingColumns_ThrowsWithColumnNames()
    {
        var ex = await Should.ThrowAsync<InvalidFileException>(() => _reader.ReadAsync(Csv("name,sku,price\nA,A-1,1\n")));

        ex.Message.ShouldContain("description, category, stock, weight_kg");
    }

    [Fact]
    public async Task ReadAsync_NullStream_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => _reader.ReadAsync(null!));
    }

    [Fact]
    public async Task ReadAsync_CancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => _reader.ReadAsync(Csv($"{Header}\nA,A-1,,Books,1,1,1\n"), cts.Token));
    }

    [Fact]
    public async Task ReadAsync_TooManyRows_ThrowsInvalidFile()
    {
        var content = new StringBuilder(Header).Append('\n');
        for (var i = 0; i <= CsvProductReader.MaxRows; i++)
        {
            content.Append("A,A-").Append(i).Append(",,Books,1,1,1\n");
        }

        var ex = await Should.ThrowAsync<InvalidFileException>(() => _reader.ReadAsync(Csv(content.ToString())));

        ex.Message.ShouldContain(CsvProductReader.MaxRows.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
