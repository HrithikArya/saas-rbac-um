using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Settings;
using Application.UnitTests.TestInfrastructure;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.UnitTests.Billing;

public class BillingServiceTests
{
    private readonly IStripeGateway _gateway = Substitute.For<IStripeGateway>();

    private StripeBillingService CreateSut(IAppDbContext db)
        => new(db, _gateway, Options.Create(new StripeSettings
        {
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel",
            PortalReturnUrl = "https://example.com/billing"
        }));

    private static IAppDbContext CreateDb(string name) => TestDbContext.Create(name);

    // ── Checkout ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCheckout_NoExistingSubscription_CreatesCustomerAndReturnsUrl()
    {
        var db = CreateDb(nameof(CreateCheckout_NoExistingSubscription_CreatesCustomerAndReturnsUrl));
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Organizations.Add(new Organization
        {
            Id = orgId, Name = "Test Org", Slug = "test-org",
            OwnerId = userId, CreatedAt = DateTime.UtcNow
        });
        db.Users.Add(new User
        {
            Id = userId, Email = "owner@test.com",
            PasswordHash = "hash", CreatedAt = DateTime.UtcNow
        });
        db.Plans.Add(new Plan { Id = Guid.NewGuid(), Name = "Free", FeaturesJson = "{}" });
        await db.SaveChangesAsync();

        _gateway.CreateCustomerAsync("owner@test.com", "Test Org")
            .Returns("cus_test123");
        _gateway.CreateCheckoutSessionAsync(
                "cus_test123", "price_pro", orgId.ToString(),
                Arg.Any<string>(), Arg.Any<string>())
            .Returns("https://checkout.stripe.com/session_abc");

        var sut = CreateSut(db);
        var url = await sut.CreateCheckoutSessionAsync(orgId, userId, "price_pro");

        url.Should().Be("https://checkout.stripe.com/session_abc");
        var sub = await db.Subscriptions.SingleAsync();
        sub.StripeCustomerId.Should().Be("cus_test123");
        sub.Status.Should().Be(SubscriptionStatus.Incomplete);
    }

    [Fact]
    public async Task CreateCheckout_ExistingCustomerId_DoesNotCreateNewCustomer()
    {
        var db = CreateDb(nameof(CreateCheckout_ExistingCustomerId_DoesNotCreateNewCustomer));
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        db.Organizations.Add(new Organization
        {
            Id = orgId, Name = "Test Org", Slug = "test-org",
            OwnerId = userId, CreatedAt = DateTime.UtcNow
        });
        db.Users.Add(new User
        {
            Id = userId, Email = "owner@test.com",
            PasswordHash = "hash", CreatedAt = DateTime.UtcNow
        });
        db.Plans.Add(new Plan { Id = planId, Name = "Free", FeaturesJson = "{}" });
        db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, PlanId = planId,
            Status = SubscriptionStatus.Active,
            StripeCustomerId = "cus_existing",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        _gateway.CreateCheckoutSessionAsync(
                "cus_existing", "price_team", orgId.ToString(),
                Arg.Any<string>(), Arg.Any<string>())
            .Returns("https://checkout.stripe.com/session_xyz");

        var sut = CreateSut(db);
        var url = await sut.CreateCheckoutSessionAsync(orgId, userId, "price_team");

        url.Should().Be("https://checkout.stripe.com/session_xyz");
        // Must NOT have created a new Stripe customer
        await _gateway.DidNotReceive().CreateCustomerAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task CreateCheckout_OrgNotFound_ThrowsNotFound()
    {
        var db = CreateDb(nameof(CreateCheckout_OrgNotFound_ThrowsNotFound));
        var sut = CreateSut(db);

        var act = () => sut.CreateCheckoutSessionAsync(Guid.NewGuid(), Guid.NewGuid(), "price_pro");

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404);
    }

    // ── Portal ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePortal_WithCustomerId_ReturnsPortalUrl()
    {
        var db = CreateDb(nameof(CreatePortal_WithCustomerId_ReturnsPortalUrl));
        var orgId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        db.Plans.Add(new Plan { Id = planId, Name = "Pro", FeaturesJson = "{}" });
        db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, PlanId = planId,
            Status = SubscriptionStatus.Active,
            StripeCustomerId = "cus_portal_test",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        _gateway.CreatePortalSessionAsync("cus_portal_test", Arg.Any<string>())
            .Returns("https://billing.stripe.com/portal_abc");

        var sut = CreateSut(db);
        var url = await sut.CreatePortalSessionAsync(orgId);

        url.Should().Be("https://billing.stripe.com/portal_abc");
    }

    [Fact]
    public async Task CreatePortal_NoSubscription_ThrowsBadRequest()
    {
        var db = CreateDb(nameof(CreatePortal_NoSubscription_ThrowsBadRequest));
        var sut = CreateSut(db);

        var act = () => sut.CreatePortalSessionAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 400);
    }

    [Fact]
    public async Task CreatePortal_NoCustomerId_ThrowsBadRequest()
    {
        var db = CreateDb(nameof(CreatePortal_NoCustomerId_ThrowsBadRequest));
        var orgId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        db.Plans.Add(new Plan { Id = planId, Name = "Free", FeaturesJson = "{}" });
        db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, PlanId = planId,
            Status = SubscriptionStatus.Incomplete,
            StripeCustomerId = null,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var act = () => sut.CreatePortalSessionAsync(orgId);

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 400);
    }

    // ── Webhook handling ──────────────────────────────────────────────────────

    [Fact]
    public async Task HandleWebhook_CheckoutCompleted_ActivatesSubscription()
    {
        var db = CreateDb(nameof(HandleWebhook_CheckoutCompleted_ActivatesSubscription));
        var orgId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        db.Plans.Add(new Plan { Id = planId, Name = "Free", FeaturesJson = "{}" });
        db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, PlanId = planId,
            Status = SubscriptionStatus.Incomplete,
            StripeCustomerId = "cus_abc",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        _gateway.ParseEvent("payload", "sig").Returns(new StripeWebhookEvent("checkout.session.completed")
        {
            SessionOrgId = orgId.ToString(),
            SessionCustomerId = "cus_abc",
            SessionSubscriptionId = "sub_new123"
        });

        var sut = CreateSut(db);
        await sut.HandleWebhookAsync("payload", "sig");

        var sub = await db.Subscriptions.SingleAsync();
        sub.Status.Should().Be(SubscriptionStatus.Active);
        sub.StripeSubscriptionId.Should().Be("sub_new123");
    }

    [Fact]
    public async Task HandleWebhook_SubscriptionUpdatedPastDue_UpdatesStatus()
    {
        var db = CreateDb(nameof(HandleWebhook_SubscriptionUpdatedPastDue_UpdatesStatus));
        var orgId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var periodEnd = DateTime.UtcNow.AddDays(30);

        db.Plans.Add(new Plan { Id = planId, Name = "Pro", FeaturesJson = "{}" });
        db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, PlanId = planId,
            Status = SubscriptionStatus.Active,
            StripeSubscriptionId = "sub_abc",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        _gateway.ParseEvent("payload", "sig").Returns(new StripeWebhookEvent("customer.subscription.updated")
        {
            SubscriptionStripeId = "sub_abc",
            SubscriptionStatus = "past_due",
            SubscriptionPeriodEnd = periodEnd
        });

        var sut = CreateSut(db);
        await sut.HandleWebhookAsync("payload", "sig");

        var sub = await db.Subscriptions.SingleAsync();
        sub.Status.Should().Be(SubscriptionStatus.PastDue);
        sub.CurrentPeriodEnd.Should().BeCloseTo(periodEnd, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task HandleWebhook_PaymentFailed_SetsPastDue()
    {
        var db = CreateDb(nameof(HandleWebhook_PaymentFailed_SetsPastDue));
        var orgId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        db.Plans.Add(new Plan { Id = planId, Name = "Pro", FeaturesJson = "{}" });
        db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, PlanId = planId,
            Status = SubscriptionStatus.Active,
            StripeSubscriptionId = "sub_fail",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        _gateway.ParseEvent("payload", "sig").Returns(new StripeWebhookEvent("invoice.payment_failed")
        {
            InvoiceSubscriptionId = "sub_fail"
        });

        var sut = CreateSut(db);
        await sut.HandleWebhookAsync("payload", "sig");

        var sub = await db.Subscriptions.SingleAsync();
        sub.Status.Should().Be(SubscriptionStatus.PastDue);
    }

    [Fact]
    public async Task HandleWebhook_InvalidSignature_ThrowsBadRequest()
    {
        var db = CreateDb(nameof(HandleWebhook_InvalidSignature_ThrowsBadRequest));
        _gateway.When(g => g.ParseEvent(Arg.Any<string>(), Arg.Any<string>()))
            .Throw(new Exception("Stripe signature invalid"));

        var sut = CreateSut(db);
        var act = () => sut.HandleWebhookAsync("bad_payload", "bad_sig");

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 400);
    }
}
