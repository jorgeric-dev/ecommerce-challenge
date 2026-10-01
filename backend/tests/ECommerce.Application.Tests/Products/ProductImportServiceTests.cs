using ECommerce.Application.Abstractions.Import;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Products.Commands.ImportProducts;
using ECommerce.Application.Tests.TestDoubles;
using ECommerce.Domain.Products;
using NSubstitute;
using Shouldly;
using ValidationException = ECommerce.Application.Common.Exceptions.ValidationException;

namespace ECommerce.Application.Tests.Products;

public class ProductImportServiceTests
{
    private readonly IProductCsvReader _reader = Substitute.For<IProductCsvReader>();
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly List<Product> _added = [];

    public ProductImportServiceTests()
    {
        _repository.GetBySkusAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, Product>());
        _repository.When(repository => repository.Add(Arg.Any<Product>()))
            .Do(call => _added.Add(call.Arg<Product>()));
    }

    private ProductImportService CreateService()
    {
        var catalog = TestData.Catalog();
        return new ProductImportService(
            _reader,
            _repository,
            catalog,
            new ImportProductsCommandValidator(),
            new ProductImportRowValidator(TestData.ProductDetailsValidator(catalog)),
            _unitOfWork);
    }

    private void GivenRecords(params ProductCsvRecord[] records) =>
        _reader.ReadAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(records);

    private static ProductCsvRecord Row(int line, string? name, string? sku, string price = "10.00", string stock = "5", string category = "Electronics") =>
        new(line, name, sku, "Description", category, price, stock, "0.5");

    private static Task<ProductImportResult> Import(ProductImportService service) =>
        service.ImportAsync(new ImportProductsCommand(new MemoryStream([1]), "products.csv"));

    [Fact]
    public async Task Import_ValidNewRows_CreatesProductsAndSavesOnce()
    {
        GivenRecords(Row(2, "Mouse", "wm-1"), Row(3, "Keyboard", "KB-1"));

        var result = await Import(CreateService());

        result.ShouldBe(result with { TotalRows = 2, Created = 2, Updated = 0, Skipped = 0, Failed = 0 });
        _added.Select(product => product.Sku).ShouldBe(["WM-1", "KB-1"]);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Import_ExistingSku_UpdatesInsteadOfCreating()
    {
        var existing = TestData.Product("WM-1", price: 1m);
        _repository.GetBySkusAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, Product> { ["WM-1"] = existing });
        GivenRecords(Row(2, "Mouse v2", "wm-1", price: "15.50"));

        var result = await Import(CreateService());

        result.Updated.ShouldBe(1);
        result.Created.ShouldBe(0);
        existing.Name.ShouldBe("Mouse v2");
        existing.Price.ShouldBe(15.50m);
        _added.ShouldBeEmpty();
    }

    [Fact]
    public async Task Import_LoadsExistingProductsWithSingleQueryUsingNormalizedSkus()
    {
        GivenRecords(Row(2, "A", "a-1"), Row(3, "B", " b-2 "), Row(4, "A again", "A-1"), Row(5, "No sku", null));

        await Import(CreateService());

        await _repository.Received(1).GetBySkusAsync(
            Arg.Is<IReadOnlyCollection<string>>(skus => skus.OrderBy(s => s).SequenceEqual(new[] { "A-1", "B-2" })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Import_DuplicateSkuInFile_LastRowWinsWithWarning()
    {
        GivenRecords(Row(2, "Shoes", "RS-001", price: "89.99"), Row(9, "Shoes v2", "RS-001", price: "94.99"));

        var result = await Import(CreateService());

        result.Created.ShouldBe(1);
        result.Updated.ShouldBe(1);
        _added.ShouldHaveSingleItem().Price.ShouldBe(94.99m);
        var warning = result.Warnings.ShouldHaveSingleItem();
        warning.LineNumber.ShouldBe(9);
        warning.Messages.ShouldHaveSingleItem().ShouldContain("overrides line 2");
    }

    [Fact]
    public async Task Import_InvalidRows_AreReportedWithoutAbortingImport()
    {
        GivenRecords(
            Row(2, "Good", "OK-1"),
            Row(3, "Yoga Mat", "YM-015", price: "free"),
            Row(4, "Desk Lamp", "DL-007", stock: "-5"),
            Row(5, "<script>alert('xss')</script>", "XS-001"),
            Row(6, "   ", "WS-001"),
            Row(7, "Gift Card", "GC-025", category: ""));

        var result = await Import(CreateService());

        result.Created.ShouldBe(1);
        result.Failed.ShouldBe(5);
        result.Errors.Select(error => error.LineNumber).ShouldBe([3, 4, 5, 6, 7]);
        result.Errors.Single(error => error.LineNumber == 6).Messages.ShouldContain("Name is required.");
    }

    [Fact]
    public async Task Import_BlankRows_AreSkippedAndNotCounted()
    {
        GivenRecords(Row(2, "Good", "OK-1"), new ProductCsvRecord(3, "", "", "", "", "", "", ""));

        var result = await Import(CreateService());

        result.TotalRows.ShouldBe(1);
        result.Skipped.ShouldBe(1);
        result.Failed.ShouldBe(0);
    }

    [Fact]
    public async Task Import_InvalidSkuFormat_IsReportedAsRowError()
    {
        GivenRecords(Row(2, "Bad sku", "BAD SKU"));

        var result = await Import(CreateService());

        result.Errors.ShouldHaveSingleItem().Messages.ShouldContain("SKU may only contain letters, digits, hyphens and underscores.");
    }

    [Fact]
    public async Task Import_NothingToPersist_DoesNotSave()
    {
        GivenRecords(Row(2, "", "X-1"));

        var result = await Import(CreateService());

        result.Failed.ShouldBe(1);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Import_EmptyFile_ReturnsEmptyResult()
    {
        GivenRecords();

        var result = await Import(CreateService());

        result.ShouldBe(new ProductImportResult(0, 0, 0, 0, 0, [], []), new ResultComparer());
        await _repository.DidNotReceive().GetBySkusAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Import_WithInvalidFile_ThrowsValidationWithoutReading()
    {
        var ex = await Should.ThrowAsync<ValidationException>(() =>
            CreateService().ImportAsync(new ImportProductsCommand(new MemoryStream(), "products.txt")));

        ex.Errors.Keys.ShouldBe(["FileName", "Content"], ignoreOrder: true);
        await _reader.DidNotReceive().ReadAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    private sealed class ResultComparer : IEqualityComparer<ProductImportResult>
    {
        public bool Equals(ProductImportResult? x, ProductImportResult? y) =>
            x is not null && y is not null
            && x.TotalRows == y.TotalRows && x.Created == y.Created && x.Updated == y.Updated
            && x.Skipped == y.Skipped && x.Failed == y.Failed
            && x.Errors.Count == y.Errors.Count && x.Warnings.Count == y.Warnings.Count;

        public int GetHashCode(ProductImportResult obj) => obj.TotalRows;
    }
}
