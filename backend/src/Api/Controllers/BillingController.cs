using Api.Extensions;
using Application.Billing.Dtos;
using Application.Common.Constants;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billing;

    public BillingController(IBillingService billing) => _billing = billing;

    /// <summary>
    /// Creates a Stripe Checkout session for upgrading the org's plan.
    /// Requires billing.manage permission (Owner only).
    /// Returns the Stripe-hosted checkout URL to redirect the user to.
    /// </summary>
    [HttpPost("/billing/checkout")]
    [Authorize(Policy = Permissions.BillingManage)]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        var userId = HttpContext.GetUserId();
        var orgId = HttpContext.GetOrganizationId();

        var url = await _billing.CreateCheckoutSessionAsync(orgId, userId, request.PriceId, ct);
        return Ok(new CheckoutResponse(url));
    }

    /// <summary>
    /// Creates a Stripe Customer Portal session so the Owner can manage their subscription.
    /// Returns the Stripe-hosted portal URL to redirect the user to.
    /// </summary>
    [HttpPost("/billing/portal")]
    [Authorize(Policy = Permissions.BillingManage)]
    public async Task<IActionResult> Portal(CancellationToken ct)
    {
        var orgId = HttpContext.GetOrganizationId();

        var url = await _billing.CreatePortalSessionAsync(orgId, ct);
        return Ok(new PortalResponse(url));
    }
}
