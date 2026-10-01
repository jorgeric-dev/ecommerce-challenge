using ECommerce.Application.Abstractions.Payments;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Common.Validation;
using ECommerce.Application.Orders.Commands.PlaceOrder;
using ECommerce.Application.Orders.Queries.GetOrders;
using ECommerce.Domain.Orders;
using ECommerce.Domain.Products;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders;

public interface IOrderService
{
    Task<PagedResult<OrderSummaryDto>> GetOrdersAsync(GetOrdersQuery query, CancellationToken cancellationToken = default);

    Task<OrderDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<OrderDto> PlaceOrderAsync(PlaceOrderCommand command, CancellationToken cancellationToken = default);
}

public sealed partial class OrderService(
    IProductRepository productRepository,
    IOrderRepository orderRepository,
    IOrderReadService orderReadService,
    IPaymentGateway paymentGateway,
    IUnitOfWork unitOfWork,
    IValidator<GetOrdersQuery> getOrdersValidator,
    IValidator<PlaceOrderCommand> placeOrderValidator,
    TimeProvider timeProvider,
    ILogger<OrderService> logger) : IOrderService
{
    public const string Currency = "USD";

    public async Task<PagedResult<OrderSummaryDto>> GetOrdersAsync(GetOrdersQuery query, CancellationToken cancellationToken = default)
    {
        await getOrdersValidator.EnsureValidAsync(query, cancellationToken);

        return await orderReadService.GetOrdersAsync(query, cancellationToken);
    }

    public async Task<OrderDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await orderReadService.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Order", id);

    public async Task<OrderDto> PlaceOrderAsync(PlaceOrderCommand command, CancellationToken cancellationToken = default)
    {
        await placeOrderValidator.EnsureValidAsync(command, cancellationToken);

        var products = await LoadProductsAsync(command.Items, cancellationToken);

        var order = Order.Create(command.Customer.Name, command.Customer.Email, command.Customer.ShippingAddress);
        foreach (var line in command.Items)
        {
            order.AddItem(products[line.ProductId], line.Quantity);
        }

        orderRepository.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var payment = await paymentGateway.ChargeAsync(
            new PaymentRequest(
                order.OrderNumber,
                order.TotalAmount,
                Currency,
                command.Payment.CardHolderName,
                command.Payment.CardNumber,
                command.Payment.ExpiryMonth,
                command.Payment.ExpiryYear,
                command.Payment.Cvv),
            cancellationToken);

        if (!payment.IsApproved)
        {
            await ReleaseReservedStockAsync(order, products, cancellationToken);
            LogPaymentDeclined(order.OrderNumber, payment.FailureReason);
            throw new PaymentDeclinedException(payment.FailureReason ?? "The payment was declined.", order.OrderNumber);
        }

        order.MarkAsPaid(payment.TransactionId!, payment.CardLast4, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogOrderPlaced(order.OrderNumber, order.TotalAmount);
        return order.ToDto();
    }

    private async Task<IReadOnlyDictionary<Guid, Product>> LoadProductsAsync(
        IReadOnlyList<OrderLine> lines,
        CancellationToken cancellationToken)
    {
        var productIds = lines.Select(line => line.ProductId).Distinct().ToList();
        var products = await productRepository.GetByIdsAsync(productIds, cancellationToken);

        var missingId = productIds.FirstOrDefault(id => !products.ContainsKey(id));
        if (missingId != Guid.Empty)
        {
            throw new NotFoundException("Product", missingId);
        }

        return products;
    }

    private async Task ReleaseReservedStockAsync(
        Order order,
        IReadOnlyDictionary<Guid, Product> products,
        CancellationToken cancellationToken)
    {
        order.MarkPaymentFailed();
        foreach (var item in order.Items)
        {
            products[item.ProductId].IncreaseStock(item.Quantity);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderNumber} placed for {TotalAmount}")]
    private partial void LogOrderPlaced(string orderNumber, decimal totalAmount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Payment for order {OrderNumber} was declined: {Reason}")]
    private partial void LogPaymentDeclined(string orderNumber, string? reason);
}
