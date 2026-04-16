namespace Infrastructure.Services;

/// <summary>
/// Parsed, Stripe-type-free representation of a webhook event.
/// Lets StripeBillingService be tested without depending on Stripe.net types.
/// </summary>
public record StripeWebhookEvent(string Type)
{
    // checkout.session.completed
    public string? SessionOrgId { get; init; }
    public string? SessionCustomerId { get; init; }
    public string? SessionSubscriptionId { get; init; }

    // customer.subscription.updated
    public string? SubscriptionStripeId { get; init; }
    public string? SubscriptionStatus { get; init; }
    public DateTime? SubscriptionPeriodEnd { get; init; }

    // invoice.payment_failed
    public string? InvoiceSubscriptionId { get; init; }
}
