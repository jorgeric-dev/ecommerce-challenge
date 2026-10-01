using ECommerce.Application.Categories;
using ECommerce.Application.Tests.TestDoubles;
using Shouldly;

namespace ECommerce.Application.Tests.Categories;

public class CategoryServiceTests
{
    [Fact]
    public async Task GetAll_ReturnsCatalogContent()
    {
        var result = await new CategoryService(TestData.Catalog()).GetAllAsync();

        result.ShouldBe([TestData.Electronics, TestData.Footwear]);
    }
}
