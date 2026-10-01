using System.Text.Json.Serialization;
using Asp.Versioning;
using ECommerce.Api.Middleware;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

namespace ECommerce.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public const string CorsPolicyName = "WebClient";
    public const string OpenApiDocumentName = "v1";

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(entry => entry.Value is { Errors.Count: > 0 })
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors.Select(error => string.IsNullOrEmpty(error.ErrorMessage) ? "The value is invalid." : error.ErrorMessage).ToArray());

                var problem = ProblemDetailsBuilder.Create(
                    context.HttpContext,
                    StatusCodes.Status400BadRequest,
                    ProblemDetailsBuilder.ValidationTitle,
                    ProblemDetailsBuilder.ValidationDetail,
                    errors);

                return new BadRequestObjectResult(problem) { ContentTypes = { ProblemDetailsBuilder.ContentType } };
            });

        services.AddProblemDetails();
        services.AddVersioningWithOpenApi();
        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy(CorsPolicyName, policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Location", RequestLoggingMiddleware.CorrelationIdHeader)));

        return services;
    }

    private static void AddVersioningWithOpenApi(this IServiceCollection services)
    {
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            })
            .AddOpenApi(options => options.Document.AddDocumentTransformer((document, _, _) =>
            {
                document.Info ??= new OpenApiInfo();
                document.Info.Title = "ECommerce API";
                document.Info.Description = "Product catalog management, CSV import, product search and purchasing.";
                return Task.CompletedTask;
            }));
    }
}
