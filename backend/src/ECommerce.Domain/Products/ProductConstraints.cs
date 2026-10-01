namespace ECommerce.Domain.Products;

public static class ProductConstraints
{
    public const int NameMaxLength = 200;
    public const int SkuMaxLength = 50;
    public const int DescriptionMaxLength = 1000;

    public const string SkuPattern = "^[A-Za-z0-9][A-Za-z0-9_-]*$";

    public const int PricePrecision = 18;
    public const int PriceScale = 2;
    public const decimal MinPrice = 0.01m;
    public const decimal MaxPrice = 1_000_000m;

    public const int WeightPrecision = 10;
    public const int WeightScale = 3;
    public const decimal MinWeightKg = 0m;
    public const decimal MaxWeightKg = 10_000m;

    public const int MinStock = 0;
    public const int MaxStock = 1_000_000;
}
