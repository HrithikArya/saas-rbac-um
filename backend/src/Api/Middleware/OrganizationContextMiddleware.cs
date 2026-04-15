using Application.Common.Interfaces;
using System.Security.Claims;

namespace Api.Middleware;

/// <summary>
/// Reads X-Organization-Id header, resolves the requesting user's role within that
/// organization, and stores both in HttpContext.Items for downstream use by
/// authorization handlers and controllers.
/// </summary>
public class OrganizationContextMiddleware
{
    private readonly RequestDelegate _next;

    public OrganizationContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IPermissionService permissionService)
    {
        var orgIdHeader = context.Request.Headers["X-Organization-Id"].FirstOrDefault();

        if (!string.IsNullOrEmpty(orgIdHeader) && Guid.TryParse(orgIdHeader, out var orgId))
        {
            var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdStr is not null && Guid.TryParse(userIdStr, out var userId))
            {
                var role = await permissionService.GetRoleAsync(userId, orgId, context.RequestAborted);

                if (role is not null)
                {
                    context.Items["OrganizationId"] = orgId;
                    context.Items["MemberRole"] = role.Value;
                }
            }
        }

        await _next(context);
    }
}
