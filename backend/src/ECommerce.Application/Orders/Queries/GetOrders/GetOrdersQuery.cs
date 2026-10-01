using ECommerce.Application.Common.Models;
using ECommerce.Domain.Orders;
using FluentValidation;

namespace ECommerce.Application.Orders.Queries.GetOrders;

public sealed record GetOrdersQuery(
    string? CustomerEmail = null,
    int Page = PagingDefaults.Page,
    int PageSize = PagingDefaults.PageSize) : IPagedRequest;

public sealed class GetOrdersQueryValidator : AbstractValidator<GetOrdersQuery>
{
    public GetOrdersQueryValidator(IValidator<IPagedRequest> pagedRequestValidator)
    {
        Include(pagedRequestValidator);

        RuleFor(x => x.CustomerEmail).MaximumLength(OrderConstraints.CustomerEmailMaxLength);
    }
}
