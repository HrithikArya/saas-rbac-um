using Api.Extensions;
using Application.Organizations;
using Application.Organizations.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("invites")]
[Authorize]
[Produces("application/json")]
public class InvitesController : ControllerBase
{
    private readonly IOrganizationService _orgService;

    public InvitesController(IOrganizationService orgService)
    {
        _orgService = orgService;
    }

    /// <summary>
    /// Accept an organization invite. The current user's email must match the invite.
    /// </summary>
    [HttpPost("accept")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Accept([FromBody] AcceptInviteRequest request, CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        await _orgService.AcceptInviteAsync(userId, request, ct);
        return NoContent();
    }
}
