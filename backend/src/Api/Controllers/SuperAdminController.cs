using Application.SuperAdmin;
using Application.SuperAdmin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("superadmin")]
[Authorize(Policy = "SuperAdmin")]
public class SuperAdminController : ControllerBase
{
    private readonly ISuperAdminService _service;

    public SuperAdminController(ISuperAdminService service) => _service = service;

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
        => Ok(await _service.GetStatsAsync(ct));

    [HttpGet("orgs")]
    public async Task<IActionResult> GetOrgs(CancellationToken ct)
        => Ok(await _service.GetOrgsAsync(ct));

    [HttpGet("orgs/{id:guid}")]
    public async Task<IActionResult> GetOrg(Guid id, CancellationToken ct)
        => Ok(await _service.GetOrgDetailAsync(id, ct));

    [HttpPost("orgs")]
    public async Task<IActionResult> CreateOrg([FromBody] SuperAdminCreateOrgRequest request, CancellationToken ct)
    {
        var org = await _service.CreateOrgAsync(request, ct);
        return Created($"/superadmin/orgs/{org.Id}", org);
    }

    [HttpPatch("orgs/{id:guid}/plan")]
    public async Task<IActionResult> ChangePlan(Guid id, [FromBody] ChangePlanRequest request, CancellationToken ct)
    {
        await _service.ChangePlanAsync(id, request.PlanId, ct);
        return NoContent();
    }

    [HttpGet("earnings")]
    public async Task<IActionResult> GetEarnings(CancellationToken ct)
        => Ok(await _service.GetEarningsAsync(ct));
}

public record ChangePlanRequest(Guid PlanId);
