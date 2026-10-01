using ECommerce.Domain.Categories;

namespace ECommerce.Infrastructure.Persistence.Seeding;

internal static class CategorySeed
{
    public static readonly IReadOnlyList<Category> Categories =
    [
        Category.Create(1, "Accessories"),
        Category.Create(2, "Beauty"),
        Category.Create(3, "Books"),
        Category.Create(4, "Clothing"),
        Category.Create(5, "Electronics"),
        Category.Create(6, "Food & Beverage"),
        Category.Create(7, "Footwear"),
        Category.Create(8, "Games"),
        Category.Create(9, "Gifts"),
        Category.Create(10, "Health"),
        Category.Create(11, "Home & Office"),
        Category.Create(12, "Kitchen"),
        Category.Create(13, "Misc"),
        Category.Create(14, "Outdoors"),
        Category.Create(15, "Pets"),
        Category.Create(16, "Sports"),
        Category.Create(17, "Stationery"),
        Category.Create(18, "Tools"),
    ];
}
