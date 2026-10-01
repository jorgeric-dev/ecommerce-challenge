using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products;
using ECommerce.Application.Products.Commands.CreateProduct;
using ECommerce.Application.Products.Commands.UpdateProduct;
using ECommerce.Application.Products.Queries.SearchProducts;
using ECommerce.Application.Tests.TestDoubles;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Products;
using NSubstitute;
using Shouldly;
using SortDirection = ECommerce.Application.Common.Models.SortDirection;
using ValidationException = ECommerce.Application.Common.Exceptions.ValidationException;

namespace ECommerce.Application.Tests.Products;

public class ProductServiceTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IProductReadService _readService = Substitute.For<IProductReadService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly StubCategoryCatalog _catalog = TestData.Catalog();

    private ProductService CreateService() => new(
        _repository,
        _readService,
        _catalog,
        _unitOfWork,
        new SearchProductsQueryValidator(TestData.PagedRequestValidator()),
        new CreateProductCommandValidator(TestData.ProductDetailsValidator(_catalog)),
        new UpdateProductCommandValidator(TestData.ProductDetailsValidator(_catalog)));

    private static ProductDto Dto(Guid id) =>
        new(id, "Speaker", "BS-021", null, 5, "Electronics", 10m, 1, 0.5m, true, DateTime.UtcNow, null);

    [Fact]
    public async Task Search_DelegatesCriteriaToReadService()
    {
        var query = new SearchProductsQuery("speaker", 5, 1m, 100m, true, ProductSortField.Price, SortDirection.Desc, 2, 5);
        var page = new PagedResult<ProductDto>([Dto(Guid.NewGuid())], 2, 5, 6);
        _readService.SearchAsync(query, Arg.Any<CancellationToken>()).Returns(page);

        var result = await CreateService().SearchAsync(query);

        result.ShouldBeSameAs(page);
    }

    [Fact]
    public async Task Search_WithInvalidCriteria_ThrowsValidationWithoutQuerying()
    {
        var ex = await Should.ThrowAsync<ValidationException>(() => CreateService().SearchAsync(new SearchProductsQuery(Page: 0)));

        ex.Errors.ShouldContainKey("Page");
        await _readService.DidNotReceive().SearchAsync(Arg.Any<SearchProductsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_Existing_ReturnsDto()
    {
        var id = Guid.NewGuid();
        _readService.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(Dto(id));

        var result = await CreateService().GetByIdAsync(id);

        result.Id.ShouldBe(id);
    }

    [Fact]
    public async Task GetById_Missing_ThrowsNotFound()
    {
        await Should.ThrowAsync<NotFoundException>(() => CreateService().GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Create_WithNewSku_AddsProductSavesAndReturnsDto()
    {
        var command = new CreateProductCommand("Speaker", "bs-021", "Loud", 5, 59.99m, 10, 0.8m);

        var dto = await CreateService().CreateAsync(command);

        dto.Sku.ShouldBe("BS-021");
        dto.CategoryName.ShouldBe("Electronics");
        dto.IsInStock.ShouldBeTrue();
        _repository.Received(1).Add(Arg.Is<Product>(p => p.Sku == "BS-021" && p.Price == 59.99m));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_WithExistingSku_ThrowsConflictAndDoesNotSave()
    {
        _repository.SkuExistsAsync("BS-021", Arg.Any<CancellationToken>()).Returns(true);

        var ex = await Should.ThrowAsync<ConflictException>(() =>
            CreateService().CreateAsync(new CreateProductCommand("Speaker", "bs-021", null, 5, 1m, 1, 1m)));

        ex.Message.ShouldContain("BS-021");
        _repository.DidNotReceive().Add(Arg.Any<Product>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_WithInvalidInput_ThrowsValidationBeforeTouchingRepository()
    {
        var ex = await Should.ThrowAsync<ValidationException>(() =>
            CreateService().CreateAsync(new CreateProductCommand("", "BAD SKU", null, 999, 1m, 1, 1m)));

        ex.Errors.Keys.ShouldBe(["Name", "CategoryId", "Sku"], ignoreOrder: true);
        await _repository.DidNotReceive().SkuExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _repository.DidNotReceive().Add(Arg.Any<Product>());
    }

    [Fact]
    public async Task Update_ExistingProduct_AppliesChangesAndSaves()
    {
        var product = TestData.Product("RS-001");
        _repository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var dto = await CreateService().UpdateAsync(new UpdateProductCommand(product.Id, "Trail Shoes", "New", 7, 99m, 3, 0.5m));

        dto.Name.ShouldBe("Trail Shoes");
        dto.CategoryName.ShouldBe("Footwear");
        dto.Sku.ShouldBe("RS-001");
        product.Price.ShouldBe(99m);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_MissingProduct_ThrowsNotFound()
    {
        await Should.ThrowAsync<NotFoundException>(() =>
            CreateService().UpdateAsync(new UpdateProductCommand(Guid.NewGuid(), "Name", null, 5, 1m, 1, 1m)));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WithInvalidInput_ThrowsValidationWithoutLoadingProduct()
    {
        var ex = await Should.ThrowAsync<ValidationException>(() =>
            CreateService().UpdateAsync(new UpdateProductCommand(Guid.Empty, "Name", null, 999, 1m, 1, 1m)));

        ex.Errors.Keys.ShouldBe(["Id", "CategoryId"], ignoreOrder: true);
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_BreakingDomainRule_PropagatesDomainException()
    {
        var product = TestData.Product();
        _repository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        var service = new ProductService(
            _repository,
            _readService,
            _catalog,
            _unitOfWork,
            new SearchProductsQueryValidator(TestData.PagedRequestValidator()),
            new CreateProductCommandValidator(TestData.ProductDetailsValidator(_catalog)),
            new FluentValidation.InlineValidator<UpdateProductCommand>());

        await Should.ThrowAsync<DomainException>(() =>
            service.UpdateAsync(new UpdateProductCommand(product.Id, "Name", null, 5, 1.999m, 1, 1m)));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ExistingProduct_SoftDeletesAndSaves()
    {
        var product = TestData.Product();
        _repository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        await CreateService().DeleteAsync(product.Id);

        product.IsDeleted.ShouldBeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_MissingProduct_ThrowsNotFound()
    {
        var ex = await Should.ThrowAsync<NotFoundException>(() => CreateService().DeleteAsync(Guid.NewGuid()));

        ex.Message.ShouldStartWith("Product with id");
    }
}
