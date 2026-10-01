using ECommerce.Application.Common.Exceptions;

namespace ECommerce.Api.Middleware;

public sealed partial class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public const string UnexpectedErrorMessage = "An unexpected error occurred. Please try again later or contact support with the trace id.";

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var error = ExceptionMapper.Map(exception);

        var problem = ProblemDetailsBuilder.Create(
            context,
            error.StatusCode,
            error.Title,
            error.ExposeMessage ? exception.Message : UnexpectedErrorMessage,
            (exception as ValidationException)?.Errors);

        if (exception is PaymentDeclinedException declined)
        {
            problem.Extensions["orderNumber"] = declined.OrderNumber;
        }

        Log(error, exception, context, problem.Extensions["traceId"]?.ToString() ?? string.Empty);

        context.Response.Clear();
        context.Response.StatusCode = error.StatusCode;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: ProblemDetailsBuilder.ContentType, CancellationToken.None);
    }

    private void Log(ErrorDescriptor error, Exception exception, HttpContext context, string traceId)
    {
        if (error.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(exception, context.Request.Method, context.Request.Path, traceId);
        }
        else if (error.StatusCode != ExceptionMapper.ClientClosedRequest)
        {
            LogHandled(context.Request.Method, context.Request.Path, error.StatusCode, exception.Message, traceId);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception for {Method} {Path} (trace {TraceId})")]
    private partial void LogUnhandled(Exception exception, string method, string path, string traceId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Method} {Path} failed with {StatusCode}: {Reason} (trace {TraceId})")]
    private partial void LogHandled(string method, string path, int statusCode, string reason, string traceId);
}
