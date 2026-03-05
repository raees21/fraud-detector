using Serilog.Context;

namespace FraudEngine.API.Middleware;

/// <summary>
/// Middleware that ensures a Correlation ID is present on all requests and responses,
/// and pushes it into the Serilog logging context for distributed tracing.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class.
    /// </summary>
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware, handling the correlation ID logic.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = context.Request.Headers[CorrelationIdHeader].ToString();

        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers.Append(CorrelationIdHeader, correlationId);
        }

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
                context.Response.Headers.Append(CorrelationIdHeader, correlationId);
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
