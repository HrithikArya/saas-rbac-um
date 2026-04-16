using Application.Common.Interfaces;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Infrastructure.Services;

public class FeatureGateService : IFeatureGate
{
    private readonly IAppDbContext _db;

    public FeatureGateService(IAppDbContext db) => _db = db;

    public async Task<bool> IsEnabledAsync(string feature, Guid orgId, CancellationToken ct = default)
    {
        var subscription = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.OrganizationId == orgId &&
                       (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing))
            .FirstOrDefaultAsync(ct);

        if (subscription is null) return false;

        var features = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            subscription.Plan.FeaturesJson);

        if (features is null || !features.TryGetValue(feature, out var value)) return false;

        return value.ValueKind == JsonValueKind.True;
    }
}
