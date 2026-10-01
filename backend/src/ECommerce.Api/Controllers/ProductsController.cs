using ECommerce.Api.Contracts;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products;
using ECommerce.Application.Products.Commands.CreateProduct;
using ECommerce.Application.Products.Commands.ImportProducts;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[Route(BaseRoute + "/products")]
[Tags("Products")]
public sealed class ProductsController(IProductService productService, IProductImportService productImportService) : ApiControllerBase
{
    private const long ImportRequestSizeLimit = ImportProductsCommand.MaxFileSizeInBytes + (64 * 1024);

    [HttpGet]
    [EndpointSummary("Search products")]
    [EndpointDescription("Returns a paged list of products filtered by free text, category, price range and stock availability.")]
    [ProducesResponseType<PagedResult<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] SearchProductsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await productService.SearchAsync(request.ToQuery(), cancellationToken));

    [HttpGet("{id:guid}", Name = nameof(GetProductById))]
    [EndpointSummary("Get a product by id")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<ProductDto>> GetProductById(Guid id, CancellationToken cancellationToken) =>
        Ok(await productService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Consumes("application/json")]
    [EndpointSummary("Create a product")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<ProductDto>> CreateProduct(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var product = await productService.CreateAsync(command, cancellationToken);
        return CreatedAtRoute(nameof(GetProductById), new { id = product.Id, version = RequestedApiVersion }, product);
    }

    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [EndpointSummary("Update a product")]
    [EndpointDescription("Replaces the editable details of a product. The SKU is immutable.")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken) =>
        Ok(await productService.UpdateAsync(request.ToCommand(id), cancellationToken));

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a product")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        await productService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImportRequestSizeLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImportRequestSizeLimit)]
    [EndpointSummary("Import products from a CSV file")]
    [EndpointDescription("Columns: name, sku, description, category, price, stock, weight_kg. Existing SKUs are updated, new SKUs are created and invalid rows are reported without aborting the import.")]
    [ProducesResponseType<ProductImportResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<ProductImportResult>> ImportProducts(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using var content = new MemoryStream();
        await file.CopyToAsync(content, cancellationToken);
        content.Position = 0;

        return Ok(await productImportService.ImportAsync(new ImportProductsCommand(content, file.FileName), cancellationToken));
    }
}
