using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders.Queries.GetOrders;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Contracts;

public sealed class GetOrdersRequest
{
    [FromQuery(Name = "customerEmail")]
    public string? CustomerEmail { get; init; }

    [FromQuery(Name = "page")]
    public int Page { get; init; } = PagingDefaults.Page;

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; init; } = PagingDefaults.PageSize;

    public GetOrdersQuery ToQuery() => new(CustomerEmail, Page, PageSize);
}
