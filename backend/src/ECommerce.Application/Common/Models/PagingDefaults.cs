namespace ECommerce.Application.Common.Models;

public static class PagingDefaults
{
    public const int Page = 1;
    public const int PageSize = 20;
    public const int MaxPageSize = 100;
}

public enum SortDirection
{
    Asc,
    Desc,
}
