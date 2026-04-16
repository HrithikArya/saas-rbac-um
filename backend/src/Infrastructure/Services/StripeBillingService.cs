using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Settings;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class StripeBillingService : IBillingService
{
    private readonly IAppDbContext _db;
    private readonly IStripeGateway _stripe;
    private readonly StripeSettings _settings;

    public StripeBillingService(IAppDbContext db, IStripeGateway stripe, IOptions<StripeSettings> settings)
    {
        _db = db;
        _stripe = stripe;
        _settings = settings.Value;
    }

    // ── Checkout ──────────────────────────────────────────────────────────────

    public async Task<string> CreateCheckoutSessionAsync(
        Guid orgId, Guid userId, string priceId, CancellationToken ct = default)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == orgId, ct)
            ?? throw new AppException("Organization not found", 404);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new AppException("User not found", 404);

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);

        string customerId;

        if (subscription?.StripeCustomerId is not null)
        {
            customerId = subscription.StripeCustomerId;
        }
        else
        {
            customerId = await _stripe.CreateCustomerAsync(user.Email, org.Name, ct);

            if (subscription is null)
            {
                var freePlan = await _db.Plans.FirstAsync(p => p.Name == "Free", ct);
                subscription = new Subscription
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = orgId,
                    PlanId = freePlan.Id,
                    Status = SubscriptionStatus.Incomplete,
                    StripeCustomerId = customerId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.Subscriptions.Add(subscription);
            }
            else
            {
                subscription.StripeCustomerId = customerId;
                subscription.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
        }

        return await _stripe.CreateCheckoutSessionAsync(
            customerId, priceId, orgId.ToString(),
            _settings.SuccessUrl, _settings.CancelUrl, ct);
    }

    // ── Portal ────────────────────────────────────────────────────────────────

    public async Task<string> CreatePortalSessionAsync(Guid orgId, CancellationToken ct = default)
    {
        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct)
            ?? throw new AppException("No subscription found for this organization");

        if (subscription.StripeCustomerId is null)
            throw new AppException("Organization has no Stripe customer — complete checkout first");

        return await _stripe.CreatePortalSessionAsync(subscription.StripeCustomerId, _settings.PortalReturnUrl, ct);
    }

    // ── Webhooks ──────────────────────────────────────────────────────────────

    public async Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        StripeWebhookEvent evt;
        try
        {
            evt = _stripe.ParseEvent(payload, signature);
        }
        catch (Exception)
        {
            throw new AppException("Invalid Stripe webhook signature", 400);
        }

        switch (evt.Type)
        {
            case "checkout.session.completed":
                await HandleCheckoutCompletedAsync(evt, ct);
                break;

            case "customer.subscription.updated":
                await HandleSubscriptionUpdatedAsync(evt, ct);
                break;

            case "invoice.payment_failed":
                await HandlePaymentFailedAsync(evt, ct);
                break;
        }
    }

    private async Task HandleCheckoutCompletedAsync(StripeWebhookEvent evt, CancellationToken ct)
    {
        if (!Guid.TryParse(evt.SessionOrgId, out var orgId)) return;

        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.OrganizationId == orgId, ct);
        if (sub is null) return;

        sub.StripeSubscriptionId = evt.SessionSubscriptionId;
        sub.StripeCustomerId = evt.SessionCustomerId ?? sub.StripeCustomerId;
        sub.Status = SubscriptionStatus.Active;
        sub.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleSubscriptionUpdatedAsync(StripeWebhookEvent evt, CancellationToken ct)
    {
        if (evt.SubscriptionStripeId is null) return;

        var sub = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == evt.SubscriptionStripeId, ct);
        if (sub is null) return;

        sub.Status = evt.SubscriptionStatus switch
        {
            "active"     => SubscriptionStatus.Active,
            "trialing"   => SubscriptionStatus.Trialing,
            "past_due"   => SubscriptionStatus.PastDue,
            "canceled"   => SubscriptionStatus.Canceled,
            _            => SubscriptionStatus.Incomplete
        };
        sub.CurrentPeriodEnd = evt.SubscriptionPeriodEnd;
        sub.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private async Task HandlePaymentFailedAsync(StripeWebhookEvent evt, CancellationToken ct)
    {
        if (evt.InvoiceSubscriptionId is null) return;

        var sub = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == evt.InvoiceSubscriptionId, ct);
        if (sub is null) return;

        sub.Status = SubscriptionStatus.PastDue;
        sub.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}
