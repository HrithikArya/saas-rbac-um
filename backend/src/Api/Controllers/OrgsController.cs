using Api.Extensions;
using Application.Common.Constants;
using Application.Organizations;
using Application.Organizations.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("orgs")]
[Authorize]
[Produces("application/json")]
public class OrgsController : ControllerBase
{
    private readonly IOrganizationService _orgService;

    public OrgsController(IOrganizationService orgService)
    {
        _orgService = orgService;
    }

    /// <summary>Create a new organization. The caller becomes the Owner.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrgResponse), 201)]
    public async Task<IActionResult> Create([FromBody] CreateOrgRequest request, CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        var org = await _orgService.CreateAsync(userId, request, ct);
        return CreatedAtAction(nameof(Get), new { id = org.Id }, org);
    }

    /// <summary>List all organizations the current user belongs to.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrgResponse>), 200)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        var orgs = await _orgService.ListForUserAsync(userId, ct);
        return Ok(orgs);
    }

    /// <summary>Get a single organization. Requires membership.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrgResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        var org = await _orgService.GetAsync(id, userId, ct);
        return Ok(org);
    }

    /// <summary>List members of an organization. Requires membership (any role).</summary>
    [HttpGet("{id:guid}/members")]
    [ProducesResponseType(typeof(IReadOnlyList<MemberResponse>), 200)]
    public async Task<IActionResult> ListMembers(Guid id, CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        // Validate membership via GetAsync (throws 404 if not member)
        await _orgService.GetAsync(id, userId, ct);

        var members = await _orgService.ListMembersAsync(id, ct);
        return Ok(members);
    }

    /// <summary>Update organization name. Requires members.manage permission.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.MembersManage)]
    [ProducesResponseType(typeof(OrgResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrgRequest request, CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        var org = await _orgService.UpdateAsync(id, userId, request, ct);
        return Ok(org);
    }

    /// <summary>Get subscription info for the organization. Any member may view.</summary>
    [HttpGet("{id:guid}/subscription")]
    [ProducesResponseType(typeof(SubscriptionResponse), 200)]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSubscription(Guid id, CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        var subscription = await _orgService.GetSubscriptionAsync(id, userId, ct);
        if (subscription is null) return NoContent();
        return Ok(subscription);
    }

    /// <summary>Look up an organization by slug. Public — used for tenant login branding.</summary>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OrgResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var org = await _orgService.GetBySlugAsync(slug, ct);
        if (org is null) return NotFound();
        return Ok(org);
    }

    /// <summary>Invite a user to the organization. Requires members.manage permission.</summary>
    [HttpPost("{id:guid}/invites")]
    [Authorize(Policy = Permissions.MembersManage)]
    [ProducesResponseType(202)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateInvite(
        Guid id,
        [FromBody] InviteMemberRequest request,
        CancellationToken ct)
    {
        var actorUserId = HttpContext.GetUserId();
        await _orgService.CreateInviteAsync(id, actorUserId, request, ct);
        return Accepted();
    }
}
