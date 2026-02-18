using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace EducationContentService.Web.Middleware;

public class RequestCorrelationIdMiddleware
{
    private const string CORRELATION_ID_HEADER_NAME = "X-Correlation-Id";
    private const string CORRELATION_ID = "CorrelationId";

    private readonly RequestDelegate _next;
    
    public RequestCorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        context.Request.Headers.TryGetValue(CORRELATION_ID_HEADER_NAME, out StringValues correlationIdValues);
        
        string correlationId = correlationIdValues.FirstOrDefault() ?? context.TraceIdentifier;

        using (LogContext.PushProperty(CORRELATION_ID, correlationId))
        {
            return _next(context);
        }
    }
}

public static class RequestCorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestCorrelationIdMiddleware>();
    }
}