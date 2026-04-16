using Application.Common.Interfaces;
using Application.UnitTests.TestInfrastructure;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Services;

namespace Application.UnitTests.Billing;

public class FeatureGateTests
{
    private static IAppDbContext CreateDb(string name) => TestDbContext.Create(name);

    private static async Task<(IAppDbContext db, Guid orgId)> SetupOrgWithPlanAsync(
        string dbName, string featuresJson, SubscriptionStatus status)
    {
        var db = CreateDb(dbName);
        var orgId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        db.Plans.Add(new Plan { Id = planId, Name = "Pro", FeaturesJson = featuresJson });
        db.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            PlanId = planId,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return (db, orgId);
    }

    // ── Enabled features ──────────────────────────────────────────────────────

    [Fact]
    public async Task IsEnabled_ActiveSubscriptionFeatureTrue_ReturnsTrue()
    {
        var (db, orgId) = await SetupOrgWithPlanAsync(
            nameof(IsEnabled_ActiveSubscriptionFeatureTrue_ReturnsTrue),
            "{\"advanced_reports\":true,\"max_members\":20}",
            SubscriptionStatus.Active);

        var sut = new FeatureGateService(db);
        var result = await sut.IsEnabledAsync("advanced_reports", orgId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEnabled_TrialingSubscription_ReturnsTrue()
    {
        var (db, orgId) = await SetupOrgWithPlanAsync(
            nameof(IsEnabled_TrialingSubscription_ReturnsTrue),
            "{\"advanced_reports\":true}",
            SubscriptionStatus.Trialing);

        var sut = new FeatureGateService(db);
        var result = await sut.IsEnabledAsync("advanced_reports", orgId);

        result.Should().BeTrue();
    }

    // ── Disabled features ─────────────────────────────────────────────────────

    [Fact]
    public async Task IsEnabled_FreePlanFeatureFalse_ReturnsFalse()
    {
        var (db, orgId) = await SetupOrgWithPlanAsync(
            nameof(IsEnabled_FreePlanFeatureFalse_ReturnsFalse),
            "{\"advanced_reports\":false}",
            SubscriptionStatus.Active);

        var sut = new FeatureGateService(db);
        var result = await sut.IsEnabledAsync("advanced_reports", orgId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEnabled_FeatureNotInPlan_ReturnsFalse()
    {
        var (db, orgId) = await SetupOrgWithPlanAsync(
            nameof(IsEnabled_FeatureNotInPlan_ReturnsFalse),
            "{\"max_members\":3}",
            SubscriptionStatus.Active);

        var sut = new FeatureGateService(db);
        var result = await sut.IsEnabledAsync("advanced_reports", orgId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEnabled_NoSubscription_ReturnsFalse()
    {
        var db = CreateDb(nameof(IsEnabled_NoSubscription_ReturnsFalse));
        var sut = new FeatureGateService(db);

        var result = await sut.IsEnabledAsync("advanced_reports", Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEnabled_CanceledSubscription_ReturnsFalse()
    {
        var (db, orgId) = await SetupOrgWithPlanAsync(
            nameof(IsEnabled_CanceledSubscription_ReturnsFalse),
            "{\"advanced_reports\":true}",
            SubscriptionStatus.Canceled);

        var sut = new FeatureGateService(db);
        var result = await sut.IsEnabledAsync("advanced_reports", orgId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEnabled_PastDueSubscription_ReturnsFalse()
    {
        var (db, orgId) = await SetupOrgWithPlanAsync(
            nameof(IsEnabled_PastDueSubscription_ReturnsFalse),
            "{\"advanced_reports\":true}",
            SubscriptionStatus.PastDue);

        var sut = new FeatureGateService(db);
        var result = await sut.IsEnabledAsync("advanced_reports", orgId);

        result.Should().BeFalse();
    }
}
