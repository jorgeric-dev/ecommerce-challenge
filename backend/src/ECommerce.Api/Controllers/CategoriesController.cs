using ECommerce.Application.Categories;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[Route(BaseRoute + "/categories")]
[Tags("Categories")]
public sealed class CategoriesController(ICategoryService categoryService) : ApiControllerBase
{
    [HttpGet]
    [EndpointSummary("List product categories")]
    [ProducesResponseType<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(CancellationToken cancellationToken) =>
        Ok(await categoryService.GetAllAsync(cancellationToken));
}
