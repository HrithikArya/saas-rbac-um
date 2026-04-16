namespace Application.Common.Settings;

public class StripeSettings
{
    public const string SectionName = "Stripe";
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = "http://localhost:3000/settings/billing?success=true";
    public string CancelUrl { get; set; } = "http://localhost:3000/settings/billing?canceled=true";
    public string PortalReturnUrl { get; set; } = "http://localhost:3000/settings/billing";
}
