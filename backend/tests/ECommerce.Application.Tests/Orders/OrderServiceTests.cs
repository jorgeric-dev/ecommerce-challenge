using ECommerce.Application.Abstractions.Payments;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders;
using ECommerce.Application.Orders.Commands.PlaceOrder;
using ECommerce.Application.Orders.Queries.GetOrders;
using ECommerce.Application.Tests.TestDoubles;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Orders;
using ECommerce.Domain.Products;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using ValidationException = ECommerce.Application.Common.Exceptions.ValidationException;

namespace ECommerce.Application.Tests.Orders;

public class OrderServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 10, 0, 0, TimeSpan.Zero);

    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IOrderReadService _readService = Substitute.For<IOrderReadService>();
    private readonly IPaymentGateway _paymentGateway = Substitute.For<IPaymentGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Product _mouse = TestData.Product("WM-042", price: 29.99m, stock: 10);
    private readonly Product _cable = TestData.Product("UC-003", price: 4.99m, stock: 3);

    public OrderServiceTests()
    {
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [_mouse.Id] = _mouse, [_cable.Id] = _cable });
        _paymentGateway.ChargeAsync(Arg.Any<PaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaymentResult.Approved("PAY-1", "4242"));
    }

    private OrderService CreateService()
    {
        var timeProvider = new FakeTimeProvider(Now);
        return new OrderService(
            _productRepository,
            _orderRepository,
            _readService,
            _paymentGateway,
            _unitOfWork,
            new GetOrdersQueryValidator(TestData.PagedRequestValidator()),
            new PlaceOrderCommandValidator(timeProvider),
            timeProvider,
            NullLogger<OrderService>.Instance);
    }

    private PlaceOrderCommand Command(params OrderLine[] lines) => new(
        new CustomerDetails("Jane Doe", "jane@example.com", "1 Main St"),
        lines.Length == 0 ? [new OrderLine(_mouse.Id, 2), new OrderLine(_cable.Id, 1)] : lines,
        new PaymentCard("Jane Doe", "4242424242424242", 12, 2030, "123"));

    [Fact]
    public async Task GetById_Existing_ReturnsOrder()
    {
        var id = Guid.NewGuid();
        var dto = new OrderDto(id, "ORD-1", "Jane", "jane@example.com", "Addr", OrderStatus.Paid, 10m, "PAY", "4242", DateTime.UtcNow, DateTime.UtcNow, []);
        _readService.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await CreateService().GetByIdAsync(id);

        result.ShouldBeSameAs(dto);
    }

    [Fact]
    public async Task GetById_Missing_ThrowsNotFound()
    {
        var ex = await Should.ThrowAsync<NotFoundException>(() => CreateService().GetByIdAsync(Guid.NewGuid()));

        ex.Message.ShouldStartWith("Order with id");
    }

    [Fact]
    public async Task GetOrders_DelegatesToReadService()
    {
        var query = new GetOrdersQuery("jane@example.com", 1, 10);
        var page = new PagedResult<OrderSummaryDto>([], 1, 10, 0);
        _readService.GetOrdersAsync(query, Arg.Any<CancellationToken>()).Returns(page);

        var result = await CreateService().GetOrdersAsync(query);

        result.ShouldBeSameAs(page);
    }

    [Fact]
    public async Task GetOrders_WithInvalidPaging_ThrowsValidationWithoutQuerying()
    {
        var ex = await Should.ThrowAsync<ValidationException>(() => CreateService().GetOrdersAsync(new GetOrdersQuery(PageSize: 0)));

        ex.Errors.ShouldContainKey("PageSize");
        await _readService.DidNotReceive().GetOrdersAsync(Arg.Any<GetOrdersQuery>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1, 20, true)]
    [InlineData(0, 20, false)]
    [InlineData(1, 101, false)]
    public void GetOrdersValidator_ValidatesPaging(int page, int pageSize, bool expected)
    {
        var validator = new GetOrdersQueryValidator(TestData.PagedRequestValidator());

        validator.Validate(new GetOrdersQuery(null, page, pageSize)).IsValid.ShouldBe(expected);
    }

    [Fact]
    public void GetOrdersValidator_RejectsTooLongEmail()
    {
        var validator = new GetOrdersQueryValidator(TestData.PagedRequestValidator());

        validator.Validate(new GetOrdersQuery(new string('a', OrderConstraints.CustomerEmailMaxLength + 1))).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void OrderMappings_ToDto_MapsAllFields()
    {
        var order = Order.Create("Jane", "jane@example.com", "Addr");
        order.AddItem(TestData.Product("A-1", price: 2.50m, stock: 5), 2);

        var dto = order.ToDto();

        dto.OrderNumber.ShouldBe(order.OrderNumber);
        dto.TotalAmount.ShouldBe(5m);
        var item = dto.Items.ShouldHaveSingleItem();
        item.Sku.ShouldBe("A-1");
        item.LineTotal.ShouldBe(5m);
    }

    [Fact]
    public async Task PlaceOrder_WithApprovedPayment_ReturnsPaidOrderAndDecreasesStock()
    {
        var order = await CreateService().PlaceOrderAsync(Command());

        order.Status.ShouldBe(OrderStatus.Paid);
        order.TotalAmount.ShouldBe(64.97m);
        order.PaymentReference.ShouldBe("PAY-1");
        order.CardLast4.ShouldBe("4242");
        order.PaidAtUtc.ShouldBe(Now.UtcDateTime);
        order.Items.Count.ShouldBe(2);
        _mouse.Stock.ShouldBe(8);
        _cable.Stock.ShouldBe(2);
        _orderRepository.Received(1).Add(Arg.Any<Order>());
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrder_ChargesTheOrderTotalWithCardDetails()
    {
        await CreateService().PlaceOrderAsync(Command());

        await _paymentGateway.Received(1).ChargeAsync(
            Arg.Is<PaymentRequest>(request =>
                request.Amount == 64.97m
                && request.Currency == OrderService.Currency
                && request.CardNumber == "4242424242424242"
                && request.OrderNumber.StartsWith("ORD-")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrder_LoadsAllProductsInASingleQuery()
    {
        await CreateService().PlaceOrderAsync(Command());

        await _productRepository.Received(1).GetByIdsAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrder_WithInvalidCommand_ThrowsValidationBeforeLoadingProducts()
    {
        var command = Command() with { Items = [] };

        var ex = await Should.ThrowAsync<ValidationException>(() => CreateService().PlaceOrderAsync(command));

        ex.Errors.ShouldContainKey("Items");
        await _productRepository.DidNotReceive().GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
        await _paymentGateway.DidNotReceive().ChargeAsync(Arg.Any<PaymentRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrder_WithDeclinedPayment_ReleasesStockMarksOrderFailedAndThrows()
    {
        _paymentGateway.ChargeAsync(Arg.Any<PaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaymentResult.Declined("Card declined", "0002"));
        Order? savedOrder = null;
        _orderRepository.When(repository => repository.Add(Arg.Any<Order>())).Do(call => savedOrder = call.Arg<Order>());

        var ex = await Should.ThrowAsync<PaymentDeclinedException>(() => CreateService().PlaceOrderAsync(Command()));

        ex.Reason.ShouldBe("Card declined");
        savedOrder.ShouldNotBeNull().Status.ShouldBe(OrderStatus.PaymentFailed);
        ex.OrderNumber.ShouldBe(savedOrder.OrderNumber);
        _mouse.Stock.ShouldBe(10);
        _cable.Stock.ShouldBe(3);
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrder_WithDeclinedPaymentWithoutReason_UsesDefaultMessage()
    {
        _paymentGateway.ChargeAsync(Arg.Any<PaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentResult(false, null, "0002", null));

        var ex = await Should.ThrowAsync<PaymentDeclinedException>(() => CreateService().PlaceOrderAsync(Command()));

        ex.Reason.ShouldBe("The payment was declined.");
    }

    [Fact]
    public async Task PlaceOrder_WithUnknownProduct_ThrowsNotFoundBeforeCharging()
    {
        var unknownId = Guid.NewGuid();

        var ex = await Should.ThrowAsync<NotFoundException>(() =>
            CreateService().PlaceOrderAsync(Command(new OrderLine(_mouse.Id, 1), new OrderLine(unknownId, 1))));

        ex.Message.ShouldContain(unknownId.ToString());
        await _paymentGateway.DidNotReceive().ChargeAsync(Arg.Any<PaymentRequest>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrder_WithInsufficientStock_ThrowsBeforeSavingOrCharging()
    {
        await Should.ThrowAsync<InsufficientStockException>(() =>
            CreateService().PlaceOrderAsync(Command(new OrderLine(_cable.Id, 4))));

        _orderRepository.DidNotReceive().Add(Arg.Any<Order>());
        await _paymentGateway.DidNotReceive().ChargeAsync(Arg.Any<PaymentRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrder_WhenReservationConflicts_PropagatesConflictWithoutCharging()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns<int>(_ => throw new ConflictException("modified"));

        await Should.ThrowAsync<ConflictException>(() => CreateService().PlaceOrderAsync(Command()));

        await _paymentGateway.DidNotReceive().ChargeAsync(Arg.Any<PaymentRequest>(), Arg.Any<CancellationToken>());
    }
}
