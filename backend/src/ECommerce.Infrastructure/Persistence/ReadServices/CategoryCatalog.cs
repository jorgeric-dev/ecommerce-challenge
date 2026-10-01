using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Categories;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.ReadServices;

internal sealed class CategoryCatalog(AppDbContext dbContext) : ICategoryCatalog
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryDto(category.Id, category.Name))
            .ToListAsync(cancellationToken);
}
