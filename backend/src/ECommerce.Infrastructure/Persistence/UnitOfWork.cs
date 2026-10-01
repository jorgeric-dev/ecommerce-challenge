using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

internal sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException("The data was modified by another request. Please reload and try again.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new ConflictException("The changes could not be saved because they conflict with existing data.", ex);
        }
    }
}
