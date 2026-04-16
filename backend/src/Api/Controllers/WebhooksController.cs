using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class WebhooksController : ControllerBase
{
    private readonly IBillingService _billing;

    public WebhooksController(IBillingService billing) => _billing = billing;

    /// <summary>
    /// Receives Stripe webhook events. Auth is via Stripe-Signature header validation,
    /// not Bearer JWT — this endpoint must remain anonymous.
    ///
    /// Events handled:
    ///   checkout.session.completed  → activates subscription
    ///   customer.subscription.updated → syncs status + period end
    ///   invoice.payment_failed → marks subscription PastDue
    /// </summary>
    [HttpPost("/webhooks/stripe")]
    [AllowAnonymous]
    public async Task<IActionResult> Stripe(CancellationToken ct)
    {
        // Read raw body — must not use [FromBody] or the HMAC check will fail
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(ct);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        await _billing.HandleWebhookAsync(payload, signature, ct);

        return Ok();
    }
}
