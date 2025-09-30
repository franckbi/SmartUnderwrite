using Serilog.Context;

namespace SmartUnderwrite.Api.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get correlation ID from header or generate new one
        var correlationId = GetCorrelationId(context);
        
        // Add to context items for access throughout the request
        context.Items["CorrelationId"] = correlationId;
        
        // Add to response headers
        context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);
        
        // Add to Serilog context for structured logging
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        // Try to get from request header first
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) 
            && !string.IsNullOrEmpty(correlationId))
        {
            return correlationId.ToString();
        }

        // Use TraceIdentifier if available
        if (!string.IsNullOrEmpty(context.TraceIdentifier))
        {
            return context.TraceIdentifier;
        }

        // Generate new GUID as fallback
        return Guid.NewGuid().ToString();
    }
}