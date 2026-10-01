using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Middleware;

public static class ProblemDetailsBuilder
{
    public const string ContentType = "application/problem+json";
    public const string ValidationTitle = "Validation failed";
    public const string ValidationDetail = "One or more validation errors occurred.";

    public static ProblemDetails Create(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = $"{context.Request.Method} {context.Request.Path}",
            Type = $"https://httpstatuses.io/{statusCode}",
        };

        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        return problem;
    }
}
