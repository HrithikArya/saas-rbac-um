using Api.Extensions;
using Application.Billing.Dtos;
using Application.Common.Constants;
using Application.Common.Interfaces;
using Application.SuperAdmin;
using Application.SuperAdmin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billing;
    private readonly ISuperAdminService _superAdmin;

    public BillingController(IBillingService billing, ISuperAdminService superAdmin)
    {
        _billing = billing;
        _superAdmin = superAdmin;
    }

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

    /// <summary>
    /// Confirms a mock payment — activates the subscription without Stripe.
    /// Only works when MockBillingService is active (no STRIPE_SECRET_KEY set).
    /// </summary>
    [HttpPost("/billing/mock-confirm")]
    [Authorize(Policy = Permissions.BillingManage)]
    public async Task<IActionResult> MockConfirm([FromBody] MockConfirmRequest request, CancellationToken ct)
    {
        var orgId = HttpContext.GetOrganizationId();
        var fullRequest = request with { OrgId = orgId };
        var payment = await _superAdmin.ConfirmMockPaymentAsync(fullRequest, ct);
        return Ok(payment);
    }
}
