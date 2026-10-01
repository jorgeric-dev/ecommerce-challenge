using ECommerce.Application.Categories;
using ECommerce.Application.Orders;
using ECommerce.Application.Orders.Commands.PlaceOrder;
using ECommerce.Application.Products;
using ECommerce.Application.Products.Commands.CreateProduct;
using ECommerce.Application.Products.Commands.ImportProducts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ECommerce.Application.Tests.Common;

public class DependencyInjectionTests
{
    [Theory]
    [InlineData(typeof(ICategoryService), typeof(CategoryService))]
    [InlineData(typeof(IProductService), typeof(ProductService))]
    [InlineData(typeof(IProductImportService), typeof(ProductImportService))]
    [InlineData(typeof(IOrderService), typeof(OrderService))]
    public void AddApplication_RegistersServicesAsScoped(Type contract, Type implementation)
    {
        var services = new ServiceCollection().AddApplication();

        services.ShouldContain(descriptor =>
            descriptor.ServiceType == contract
            && descriptor.ImplementationType == implementation
            && descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplication_RegistersValidatorsAndTimeProvider()
    {
        var services = new ServiceCollection().AddApplication();

        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IValidator<CreateProductCommand>));
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IValidator<PlaceOrderCommand>));
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IValidator<ProductImportRow>));
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(TimeProvider));
    }
}
