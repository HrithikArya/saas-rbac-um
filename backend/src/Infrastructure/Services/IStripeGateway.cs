namespace Infrastructure.Services;

/// <summary>
/// Thin wrapper around the Stripe SDK — exists purely to make StripeBillingService testable.
/// Only StripeGatewayAdapter should implement this; tests use NSubstitute mocks.
/// </summary>
public interface IStripeGateway
{
    Task<string> CreateCustomerAsync(string email, string orgName, CancellationToken ct = default);

    Task<string> CreateCheckoutSessionAsync(
        string customerId,
        string priceId,
        string orgId,
        string successUrl,
        string cancelUrl,
        CancellationToken ct = default);

    Task<string> CreatePortalSessionAsync(string customerId, string returnUrl, CancellationToken ct = default);

    /// <summary>Validates the Stripe-Signature header and returns the parsed event DTO.</summary>
    StripeWebhookEvent ParseEvent(string payload, string signature);
}
