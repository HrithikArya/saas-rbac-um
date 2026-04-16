namespace Application.Organizations.Dtos;

public record SubscriptionResponse(
    string Plan,
    string Status,
    DateTime? CurrentPeriodEnd,
    string? StripeCustomerId
);
