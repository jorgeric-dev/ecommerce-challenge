using System.Linq.Expressions;
using ECommerce.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.ReadServices;

internal static class QueryableExtensions
{
    private const char LikeEscapeCharacter = '\\';

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = totalCount == 0
            ? []
            : await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<T>(items, page, pageSize, totalCount);
    }

    public static IOrderedQueryable<T> OrderByDirection<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        SortDirection direction) =>
        direction == SortDirection.Desc ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);

    public static string ToContainsLikePattern(this string term)
    {
        var escaped = term
            .Replace(LikeEscapeCharacter.ToString(), $"{LikeEscapeCharacter}{LikeEscapeCharacter}", StringComparison.Ordinal)
            .Replace("%", $"{LikeEscapeCharacter}%", StringComparison.Ordinal)
            .Replace("_", $"{LikeEscapeCharacter}_", StringComparison.Ordinal)
            .Replace("[", $"{LikeEscapeCharacter}[", StringComparison.Ordinal);

        return $"%{escaped}%";
    }

    public static string LikeEscape => LikeEscapeCharacter.ToString();
}
