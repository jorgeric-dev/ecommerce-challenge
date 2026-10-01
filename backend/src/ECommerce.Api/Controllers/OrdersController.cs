using ECommerce.Api.Contracts;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders;
using ECommerce.Application.Orders.Commands.PlaceOrder;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[Route(BaseRoute + "/orders")]
[Tags("Orders")]
public sealed class OrdersController(IOrderService orderService) : ApiControllerBase
{
    [HttpGet]
    [EndpointSummary("List orders")]
    [ProducesResponseType<PagedResult<OrderSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> GetOrders(
        [FromQuery] GetOrdersRequest request,
        CancellationToken cancellationToken) =>
        Ok(await orderService.GetOrdersAsync(request.ToQuery(), cancellationToken));

    [HttpGet("{id:guid}", Name = nameof(GetOrderById))]
    [EndpointSummary("Get an order by id")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<OrderDto>> GetOrderById(Guid id, CancellationToken cancellationToken) =>
        Ok(await orderService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Consumes("application/json")]
    [EndpointSummary("Purchase products")]
    [EndpointDescription("Reserves stock, charges the (simulated) payment provider and returns the paid order. Any valid card number is approved, except test cards 4000000000000002 (declined) and 4000000000009995 (insufficient funds).")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status402PaymentRequired, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<OrderDto>> PlaceOrder(
        [FromBody] PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        var order = await orderService.PlaceOrderAsync(command, cancellationToken);
        return CreatedAtRoute(nameof(GetOrderById), new { id = order.Id, version = RequestedApiVersion }, order);
    }
}
