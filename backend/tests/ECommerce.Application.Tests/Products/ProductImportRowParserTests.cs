using ECommerce.Application.Abstractions.Import;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Categories;
using ECommerce.Application.Products.Commands.ImportProducts;
using ECommerce.Application.Tests.TestDoubles;
using Shouldly;

namespace ECommerce.Application.Tests.Products;

public class ProductImportRowParserTests
{
    private readonly IReadOnlyDictionary<string, CategoryDto> _categories = TestData.Catalog().GetByNameAsync().Result;

    private static ProductCsvRecord Record(
        string? name = "Wireless Mouse",
        string? sku = "WM-042",
        string? description = "Ergonomic",
        string? category = "Electronics",
        string? price = "29.99",
        string? stock = "75",
        string? weight = "0.12") =>
        new(4, name, sku, description, category, price, stock, weight);

    [Fact]
    public void Parse_ValidRecord_ReturnsTypedRow()
    {
        var (row, errors) = ProductImportRowParser.Parse(Record(), _categories);

        errors.ShouldBeEmpty();
        row.ShouldBe(new ProductImportRow(4, "Wireless Mouse", "WM-042", "Ergonomic", 5, 29.99m, 75, 0.12m));
    }

    [Theory]
    [InlineData("$29.99", 29.99)]
    [InlineData(" €15.5 ", 15.5)]
    [InlineData("£7", 7)]
    [InlineData("1000", 1000)]
    public void Parse_PriceWithCurrencySymbol_IsNormalized(string price, decimal expected)
    {
        var (row, _) = ProductImportRowParser.Parse(Record(price: price), _categories);

        row!.Price.ShouldBe(expected);
    }

    [Fact]
    public void Parse_CategoryMatch_IsCaseInsensitiveAndTrimmed()
    {
        var (row, _) = ProductImportRowParser.Parse(Record(category: "  electronics "), _categories);

        row!.CategoryId.ShouldBe(5);
    }

    [Fact]
    public void Parse_TrimsTextAndConvertsBlankDescriptionToNull()
    {
        var (row, _) = ProductImportRowParser.Parse(Record(name: "  Mouse ", sku: " wm-1 ", description: "  "), _categories);

        row!.Name.ShouldBe("Mouse");
        row.Sku.ShouldBe("wm-1");
        row.Description.ShouldBeNull();
    }

    [Theory]
    [InlineData("free", "Price 'free' is not a valid number.")]
    [InlineData("", "Price is required.")]
    [InlineData("1,000.50", "Price '1,000.50' is not a valid number.")]
    public void Parse_InvalidPrice_ReportsError(string price, string expectedError)
    {
        var (row, errors) = ProductImportRowParser.Parse(Record(price: price), _categories);

        row.ShouldBeNull();
        errors.ShouldContain(expectedError);
    }

    [Theory]
    [InlineData("many", "Stock 'many' is not a valid whole number.")]
    [InlineData("1.5", "Stock '1.5' is not a valid whole number.")]
    [InlineData(" ", "Stock is required.")]
    public void Parse_InvalidStock_ReportsError(string stock, string expectedError)
    {
        var (_, errors) = ProductImportRowParser.Parse(Record(stock: stock), _categories);

        errors.ShouldContain(expectedError);
    }

    [Fact]
    public void Parse_NegativeStock_IsParsedAndLeftToValidation()
    {
        var (row, errors) = ProductImportRowParser.Parse(Record(stock: "-5"), _categories);

        errors.ShouldBeEmpty();
        row!.Stock.ShouldBe(-5);
    }

    [Theory]
    [InlineData(null, "Weight is required.")]
    [InlineData("heavy", "Weight 'heavy' is not a valid number.")]
    [InlineData("$1", "Weight '$1' is not a valid number.")]
    public void Parse_InvalidWeight_ReportsError(string? weight, string expectedError)
    {
        var (_, errors) = ProductImportRowParser.Parse(Record(weight: weight), _categories);

        errors.ShouldContain(expectedError);
    }

    [Theory]
    [InlineData(null, "Category is required.")]
    [InlineData("", "Category is required.")]
    [InlineData("Gadgets", "Category 'Gadgets' does not exist.")]
    public void Parse_InvalidCategory_ReportsError(string? category, string expectedError)
    {
        var (_, errors) = ProductImportRowParser.Parse(Record(category: category), _categories);

        errors.ShouldContain(expectedError);
    }

    [Fact]
    public void Parse_MultipleProblems_ReportsAllOfThem()
    {
        var (_, errors) = ProductImportRowParser.Parse(Record(category: "", price: "free", stock: "x", weight: ""), _categories);

        errors.Count.ShouldBe(4);
    }
}
