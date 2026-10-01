namespace ECommerce.Application.Orders.Commands.PlaceOrder;

public sealed record PlaceOrderCommand(
    CustomerDetails Customer,
    IReadOnlyList<OrderLine> Items,
    PaymentCard Payment);

public sealed record CustomerDetails(string Name, string Email, string ShippingAddress);

public sealed record OrderLine(Guid ProductId, int Quantity);

public sealed record PaymentCard(
    string CardHolderName,
    string CardNumber,
    int ExpiryMonth,
    int ExpiryYear,
    string Cvv);
