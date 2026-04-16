using Application.Common.Settings;
using Microsoft.Extensions.Options;
using Stripe;
using CheckoutSessionService = Stripe.Checkout.SessionService;
using CheckoutSessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using CheckoutSessionLineItemOptions = Stripe.Checkout.SessionLineItemOptions;
using PortalSessionService = Stripe.BillingPortal.SessionService;
using PortalSessionCreateOptions = Stripe.BillingPortal.SessionCreateOptions;

namespace Infrastructure.Services;

/// <summary>
/// Production implementation of IStripeGateway backed by Stripe.net SDK.
/// </summary>
public class StripeGatewayAdapter : IStripeGateway
{
    private readonly CustomerService _customers = new();
    private readonly CheckoutSessionService _checkoutSessions = new();
    private readonly PortalSessionService _portalSessions = new();
    private readonly string _webhookSecret;

    public StripeGatewayAdapter(IOptions<StripeSettings> settings)
    {
        var s = settings.Value;
        StripeConfiguration.ApiKey = s.SecretKey;
        _webhookSecret = s.WebhookSecret;
    }

    public async Task<string> CreateCustomerAsync(string email, string orgName, CancellationToken ct = default)
    {
        var customer = await _customers.CreateAsync(
            new CustomerCreateOptions { Email = email, Name = orgName },
            cancellationToken: ct);
        return customer.Id;
    }

    public async Task<string> CreateCheckoutSessionAsync(
        string customerId, string priceId, string orgId,
        string successUrl, string cancelUrl, CancellationToken ct = default)
    {
        var session = await _checkoutSessions.CreateAsync(new CheckoutSessionCreateOptions
        {
            Customer = customerId,
            LineItems = [new CheckoutSessionLineItemOptions { Price = priceId, Quantity = 1 }],
            Mode = "subscription",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            ClientReferenceId = orgId
        }, cancellationToken: ct);
        return session.Url;
    }

    public async Task<string> CreatePortalSessionAsync(
        string customerId, string returnUrl, CancellationToken ct = default)
    {
        var session = await _portalSessions.CreateAsync(
            new PortalSessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = returnUrl
            }, cancellationToken: ct);
        return session.Url;
    }

    public StripeWebhookEvent ParseEvent(string payload, string signature)
    {
        var e = EventUtility.ConstructEvent(payload, signature, _webhookSecret);

        return e.Type switch
        {
            "checkout.session.completed" => MapCheckoutSession(e),
            "customer.subscription.updated" => MapSubscription(e),
            "invoice.payment_failed" => MapInvoice(e),
            _ => new StripeWebhookEvent(e.Type)
        };
    }

    private static StripeWebhookEvent MapCheckoutSession(Event e)
    {
        var s = e.Data.Object as Stripe.Checkout.Session;
        return new StripeWebhookEvent(e.Type)
        {
            SessionOrgId = s?.ClientReferenceId,
            SessionCustomerId = s?.CustomerId,
            SessionSubscriptionId = s?.SubscriptionId
        };
    }

    private static StripeWebhookEvent MapSubscription(Event e)
    {
        var s = e.Data.Object as Stripe.Subscription;
        return new StripeWebhookEvent(e.Type)
        {
            SubscriptionStripeId = s?.Id,
            SubscriptionStatus = s?.Status,
            SubscriptionPeriodEnd = s?.CurrentPeriodEnd
        };
    }

    private static StripeWebhookEvent MapInvoice(Event e)
    {
        var inv = e.Data.Object as Stripe.Invoice;
        return new StripeWebhookEvent(e.Type)
        {
            InvoiceSubscriptionId = inv?.SubscriptionId
        };
    }
}
