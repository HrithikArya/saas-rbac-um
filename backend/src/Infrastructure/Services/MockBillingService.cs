using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
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
    private readonly string _appUrl;

    public MockBillingService(ILogger<MockBillingService> logger, IConfiguration config)
    {
        _logger = logger;
        _appUrl = config["App:Url"] ?? "http://localhost:3300";
    }

    public Task<string> CreateCheckoutSessionAsync(
        Guid orgId, Guid userId, string priceId, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "MockBillingService: CreateCheckoutSession called. OrgId={OrgId} PriceId={PriceId}. " +
            "No real Stripe session created. Set Stripe:SecretKey to use real billing.",
            orgId, priceId);

        return Task.FromResult($"{_appUrl}/dev/billing-mock?action=checkout&priceId={priceId}&orgId={orgId}");
    }

    public Task<string> CreatePortalSessionAsync(Guid orgId, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "MockBillingService: CreatePortalSession called. OrgId={OrgId}. " +
            "No real Stripe portal created.",
            orgId);

        return Task.FromResult($"{_appUrl}/dev/billing-mock?action=portal&orgId={orgId}");
    }

    public Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "MockBillingService: Webhook received but ignored (mock mode). Payload length={Len}",
            payload.Length);

        return Task.CompletedTask;
    }
}
