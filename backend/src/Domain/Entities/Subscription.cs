using Domain.Enums;

namespace Domain.Entities;

public class Subscription
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public Plan Plan { get; set; } = null!;
}
