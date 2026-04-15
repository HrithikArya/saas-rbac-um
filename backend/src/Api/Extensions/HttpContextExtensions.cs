using Application.Common.Exceptions;
using Domain.Enums;
using System.Security.Claims;

namespace Api.Extensions;

public static class HttpContextExtensions
{
    public static Guid GetUserId(this HttpContext context)
    {
        var value = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(value, out var id)) return id;
        throw new AppException("Unauthorized", 401);
    }

    public static Guid GetOrganizationId(this HttpContext context)
    {
        if (context.Items["OrganizationId"] is Guid orgId) return orgId;
        throw new AppException("Organization context required. Pass X-Organization-Id header.", 400);
    }

    public static MemberRole GetMemberRole(this HttpContext context)
    {
        if (context.Items["MemberRole"] is MemberRole role) return role;
        throw new AppException("Organization context required.", 400);
    }
}
