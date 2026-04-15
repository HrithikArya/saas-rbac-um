using Serilog.Context;
using System.Security.Claims;

namespace Api.Middleware;

/// <summary>
/// Enriches the Serilog log context with RequestId, UserId, and OrganizationId
/// so every log line carries the full request context.
/// </summary>
public class RequestContextMiddleware
{
    private readonly RequestDelegate _next;

    public RequestContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.TraceIdentifier;
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var orgId = context.Request.Headers["X-Organization-Id"].FirstOrDefault() ?? "none";

        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("OrganizationId", orgId))
        {
            await _next(context);
        }
    }
}
