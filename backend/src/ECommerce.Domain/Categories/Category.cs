using ECommerce.Domain.Common;

namespace ECommerce.Domain.Categories;

public sealed class Category : Entity<int>, IAggregateRoot
{
    private Category()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public static Category Create(int id, string name)
    {
        Guard.Positive(id, "Category id");

        return new Category
        {
            Id = id,
            Name = Guard.RequiredText(name, "Category name", CategoryConstraints.NameMaxLength),
        };
    }
}
