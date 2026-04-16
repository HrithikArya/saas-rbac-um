using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Development-only billing service — no Stripe keys required.
/// Returns fake URLs and logs what would have happened.
/// Swap for StripeBillingService (or RazorpayBillingService) in production.
/// </summary>
public class MockBillingService : IBillingService
{
    private readonly ILogger<MockBillingService> _logger;

    public MockBillingService(ILogger<MockBillingService> logger) => _logger = logger;

    public Task<string> CreateCheckoutSessionAsync(
        Guid orgId, Guid userId, string priceId, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "MockBillingService: CreateCheckoutSession called. OrgId={OrgId} PriceId={PriceId}. " +
            "No real Stripe session created. Set Stripe:SecretKey to use real billing.",
            orgId, priceId);

        // Returns a local page you can handle in the frontend dev environment
        return Task.FromResult($"http://localhost:3000/dev/billing-mock?action=checkout&priceId={priceId}&orgId={orgId}");
    }

    public Task<string> CreatePortalSessionAsync(Guid orgId, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "MockBillingService: CreatePortalSession called. OrgId={OrgId}. " +
            "No real Stripe portal created.",
            orgId);

        return Task.FromResult($"http://localhost:3000/dev/billing-mock?action=portal&orgId={orgId}");
    }

    public Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "MockBillingService: Webhook received but ignored (mock mode). Payload length={Len}",
            payload.Length);

        return Task.CompletedTask;
    }
}
