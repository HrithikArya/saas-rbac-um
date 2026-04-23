namespace Domain.Entities;

public class Plan
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? StripePriceId { get; set; }
    public string FeaturesJson { get; set; } = "{}";
    public int PriceInCents { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = [];
}
