namespace Application.Common.Interfaces;

public interface IBillingService
{
    /// <summary>Creates a Stripe Checkout session URL for the given price.</summary>
    Task<string> CreateCheckoutSessionAsync(Guid orgId, Guid userId, string priceId, CancellationToken ct = default);

    /// <summary>Creates a Stripe Customer Portal session URL.</summary>
    Task<string> CreatePortalSessionAsync(Guid orgId, CancellationToken ct = default);

    /// <summary>Validates the webhook signature and processes the event.</summary>
    Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default);
}
