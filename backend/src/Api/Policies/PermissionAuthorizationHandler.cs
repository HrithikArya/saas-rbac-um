using Application.Common.Constants;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Api.Policies;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // Org context must have been resolved by OrganizationContextMiddleware
        if (httpContext.Items["MemberRole"] is not MemberRole role)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var allowed = Permissions.ForRole(role);

        if (allowed.Contains(requirement.Permission))
            context.Succeed(requirement);
        else
            context.Fail();

        return Task.CompletedTask;
    }
}
