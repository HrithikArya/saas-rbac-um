namespace Application.Common.Interfaces;

public interface IFeatureGate
{
    /// <summary>
    /// Returns true if the given feature is enabled for the organization's active plan.
    /// Returns false if there is no active subscription or the feature is not in the plan.
    /// </summary>
    Task<bool> IsEnabledAsync(string feature, Guid orgId, CancellationToken ct = default);
}
