using ECommerce.Application.Abstractions.Import;
using ECommerce.Application.Abstractions.Payments;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Tests.TestDoubles;
using Shouldly;

namespace ECommerce.Application.Tests.Common;

public class ModelTests
{
    [Theory]
    [InlineData(1, 10, 0, 0, false, false)]
    [InlineData(1, 10, 25, 3, false, true)]
    [InlineData(2, 10, 25, 3, true, true)]
    [InlineData(3, 10, 25, 3, true, false)]
    [InlineData(1, 0, 25, 0, false, false)]
    public void PagedResult_ComputesNavigation(int page, int pageSize, int total, int expectedPages, bool hasPrevious, bool hasNext)
    {
        var result = new PagedResult<int>([], page, pageSize, total);

        result.TotalPages.ShouldBe(expectedPages);
        result.HasPreviousPage.ShouldBe(hasPrevious);
        result.HasNextPage.ShouldBe(hasNext);
    }

    [Fact]
    public void NotFoundException_BuildsReadableMessage()
    {
        var ex = new NotFoundException("Product", 42);

        ex.Message.ShouldBe("Product with id '42' was not found.");
    }

    [Fact]
    public void PaymentDeclinedException_ExposesReasonAndOrderNumber()
    {
        var ex = new PaymentDeclinedException("Card declined", "ORD-1");

        ex.Message.ShouldContain("ORD-1");
        ex.Reason.ShouldBe("Card declined");
        ex.OrderNumber.ShouldBe("ORD-1");
    }

    [Fact]
    public void PaymentResult_FactoryMethods_SetApprovalState()
    {
        PaymentResult.Approved("TX", "4242").ShouldBe(new PaymentResult(true, "TX", "4242", null));
        PaymentResult.Declined("No funds", "0002").ShouldBe(new PaymentResult(false, null, "0002", "No funds"));
    }

    [Theory]
    [InlineData(null, null, null, null, null, null, null, true)]
    [InlineData("", " ", "", "", "", "", "", true)]
    [InlineData("Name", null, null, null, null, null, null, false)]
    [InlineData(null, null, null, null, null, null, "1", false)]
    public void ProductCsvRecord_IsBlank_DetectsEmptyRows(
        string? name, string? sku, string? description, string? category, string? price, string? stock, string? weight, bool expected)
    {
        new ProductCsvRecord(2, name, sku, description, category, price, stock, weight).IsBlank.ShouldBe(expected);
    }

    [Fact]
    public async Task CategoryCatalogExtensions_FindById_ReturnsMatchOrNull()
    {
        var catalog = TestData.Catalog();

        (await catalog.FindByIdAsync(5)).ShouldBe(TestData.Electronics);
        (await catalog.FindByIdAsync(999)).ShouldBeNull();
    }

    [Fact]
    public async Task CategoryCatalogExtensions_GetByName_IsCaseInsensitive()
    {
        var byName = await TestData.Catalog().GetByNameAsync();

        byName["electronics"].ShouldBe(TestData.Electronics);
        byName["FOOTWEAR"].ShouldBe(TestData.Footwear);
    }
}
