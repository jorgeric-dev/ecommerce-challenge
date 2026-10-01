using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Common.Validation;
using ECommerce.Application.Products.Commands.CreateProduct;
using ECommerce.Application.Products.Commands.UpdateProduct;
using ECommerce.Application.Products.Queries.SearchProducts;
using ECommerce.Domain.Products;
using FluentValidation;

namespace ECommerce.Application.Products;

public interface IProductService
{
    Task<PagedResult<ProductDto>> SearchAsync(SearchProductsQuery query, CancellationToken cancellationToken = default);

    Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProductDto> CreateAsync(CreateProductCommand command, CancellationToken cancellationToken = default);

    Task<ProductDto> UpdateAsync(UpdateProductCommand command, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class ProductService(
    IProductRepository productRepository,
    IProductReadService productReadService,
    ICategoryCatalog categoryCatalog,
    IUnitOfWork unitOfWork,
    IValidator<SearchProductsQuery> searchValidator,
    IValidator<CreateProductCommand> createValidator,
    IValidator<UpdateProductCommand> updateValidator) : IProductService
{
    public async Task<PagedResult<ProductDto>> SearchAsync(SearchProductsQuery query, CancellationToken cancellationToken = default)
    {
        await searchValidator.EnsureValidAsync(query, cancellationToken);

        return await productReadService.SearchAsync(query, cancellationToken);
    }

    public async Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await productReadService.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Product", id);

    public async Task<ProductDto> CreateAsync(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        await createValidator.EnsureValidAsync(command, cancellationToken);

        var category = await categoryCatalog.FindByIdAsync(command.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Category", command.CategoryId);

        var sku = Product.NormalizeSku(command.Sku);
        if (await productRepository.SkuExistsAsync(sku, cancellationToken))
        {
            throw new ConflictException($"A product with SKU '{sku}' already exists.");
        }

        var product = Product.Create(
            command.Name,
            sku,
            command.Description,
            command.CategoryId,
            command.Price,
            command.Stock,
            command.WeightKg);

        productRepository.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto(category.Name);
    }

    public async Task<ProductDto> UpdateAsync(UpdateProductCommand command, CancellationToken cancellationToken = default)
    {
        await updateValidator.EnsureValidAsync(command, cancellationToken);

        var product = await productRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Product", command.Id);

        var category = await categoryCatalog.FindByIdAsync(command.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Category", command.CategoryId);

        product.Update(
            command.Name,
            command.Description,
            command.CategoryId,
            command.Price,
            command.Stock,
            command.WeightKg);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto(category.Name);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Product", id);

        product.Delete();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
