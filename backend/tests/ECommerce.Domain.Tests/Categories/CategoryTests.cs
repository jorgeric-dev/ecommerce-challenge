using ECommerce.Domain.Categories;
using ECommerce.Domain.Exceptions;
using Shouldly;

namespace ECommerce.Domain.Tests.Categories;

public class CategoryTests
{
    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var category = Category.Create(5, "  Electronics ");

        category.Id.ShouldBe(5);
        category.Name.ShouldBe("Electronics");
    }

    [Theory]
    [InlineData(0, "Books")]
    [InlineData(-1, "Books")]
    [InlineData(1, "")]
    [InlineData(1, "   ")]
    public void Create_WithInvalidData_Throws(int id, string name)
    {
        Should.Throw<DomainException>(() => Category.Create(id, name));
    }

    [Fact]
    public void Create_WithTooLongName_Throws()
    {
        Should.Throw<DomainException>(() => Category.Create(1, new string('x', CategoryConstraints.NameMaxLength + 1)));
    }
}
