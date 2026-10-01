using ECommerce.Api.Extensions;
using ECommerce.Api.Middleware;
using ECommerce.Application;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapOpenApi().WithDocumentPerVersion();
if (app.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint($"/openapi/{ApiServiceCollectionExtensions.OpenApiDocumentName}.json", "ECommerce API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "ECommerce API";
    });
}

app.UseCors(ApiServiceCollectionExtensions.CorsPolicyName);
app.MapControllers();
app.MapHealthChecks("/health");

if (Program.IsOpenApiDocumentGeneration)
{
    await app.StartAsync();
    return;
}

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
}

await app.RunAsync();

public partial class Program
{
    internal static bool IsOpenApiDocumentGeneration =>
        string.Equals(System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name, "GetDocument.Insider", StringComparison.Ordinal);
}
