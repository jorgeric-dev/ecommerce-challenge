namespace ECommerce.Domain.Orders;

public static class OrderConstraints
{
    public const int OrderNumberMaxLength = 20;
    public const int CustomerNameMaxLength = 150;
    public const int CustomerEmailMaxLength = 256;
    public const int ShippingAddressMaxLength = 500;
    public const int PaymentReferenceMaxLength = 100;
    public const int CardLast4Length = 4;
    public const int StatusMaxLength = 20;
    public const int MaxQuantityPerLine = 100;
    public const int MaxLinesPerOrder = 50;
}
