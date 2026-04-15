using Api.Extensions;
using Application.Common.Constants;
using Application.Organizations;
using Application.Organizations.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("members")]
[Authorize]
[Produces("application/json")]
public class MembersController : ControllerBase
{
    private readonly IOrganizationService _orgService;

    public MembersController(IOrganizationService orgService)
    {
        _orgService = orgService;
    }

    /// <summary>
    /// Change a member's role. Requires members.manage permission in the org.
    /// Cannot demote the Owner or promote anyone to Owner.
    /// </summary>
    [HttpPatch("{id:guid}/role")]
    [Authorize(Policy = Permissions.MembersManage)]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ChangeRole(
        Guid id,
        [FromBody] ChangeRoleRequest request,
        CancellationToken ct)
    {
        var actorUserId = HttpContext.GetUserId();
        var orgId = HttpContext.GetOrganizationId();

        await _orgService.ChangeMemberRoleAsync(orgId, id, actorUserId, request, ct);
        return NoContent();
    }

    /// <summary>
    /// Remove a member from the organization. Requires members.manage permission.
    /// Cannot remove the Owner.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.MembersManage)]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        var actorUserId = HttpContext.GetUserId();
        var orgId = HttpContext.GetOrganizationId();

        await _orgService.RemoveMemberAsync(orgId, id, actorUserId, ct);
        return NoContent();
    }
}
