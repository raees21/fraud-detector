using System.Net;
using System.Text.Json;

namespace FraudEngine.API.Middleware;

/// <summary>
/// Middleware that intercepts unhandled exceptions globally and formats a standardized JSON response.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
    /// </summary>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware, wrapping the request pipeline in a catch block.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception has occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Formats the exception into a standardized, generic error response for the client.
    /// </summary>
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            Error = "InternalServerError", Message = "An unexpected error occurred. Please try again later."
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
