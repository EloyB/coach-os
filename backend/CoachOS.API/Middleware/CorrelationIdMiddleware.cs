using Serilog.Context;

namespace CoachOS.API.Middleware;

/// <summary>
/// Assigns a correlation ID to every request and echoes it back in X-Correlation-Id.
/// The ID is also pushed into the Serilog log context so all log lines for the
/// request can be joined by ID — useful when a user reports an error by reading
/// the ID from the generic 500 response body.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ContextKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var inbound)
                            && !string.IsNullOrWhiteSpace(inbound)
            ? inbound.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[ContextKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty(ContextKey, correlationId))
        {
            await next(context);
        }
    }
}
