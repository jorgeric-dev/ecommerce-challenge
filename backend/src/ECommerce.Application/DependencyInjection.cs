using ECommerce.Application.Categories;
using ECommerce.Application.Orders;
using ECommerce.Application.Products;
using ECommerce.Application.Products.Commands.ImportProducts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ECommerce.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Scoped);

        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductImportService, ProductImportService>();
        services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}
