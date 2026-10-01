using System.Diagnostics;

namespace ECommerce.Api.Middleware;

public sealed partial class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    public const string CorrelationIdHeader = "X-Correlation-ID";
    private const int MaxCorrelationIdLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = ResolveCorrelationId(context);
        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        using var scope = logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            await next(context);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            var statusCode = context.Response.StatusCode;

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                LogRequestFailed(context.Request.Method, context.Request.Path, statusCode, elapsed);
            }
            else
            {
                LogRequestCompleted(context.Request.Method, context.Request.Path, statusCode, elapsed);
            }
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var incoming = context.Request.Headers[CorrelationIdHeader].ToString();

        return !string.IsNullOrWhiteSpace(incoming) && incoming.Length <= MaxCorrelationIdLength && incoming.All(IsSafeCharacter)
            ? incoming
            : Guid.NewGuid().ToString("N");
    }

    private static bool IsSafeCharacter(char character) => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.';

    [LoggerMessage(Level = LogLevel.Information, Message = "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds:0.0} ms")]
    private partial void LogRequestCompleted(string method, string path, int statusCode, double elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Error, Message = "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds:0.0} ms")]
    private partial void LogRequestFailed(string method, string path, int statusCode, double elapsedMilliseconds);
}
