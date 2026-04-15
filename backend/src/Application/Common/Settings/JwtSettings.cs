namespace Application.Common.Settings;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    // Uses set (not init) so Configure<JwtSettings> can bind it after construction
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "saas-api";
    public string Audience { get; set; } = "saas-client";
    public int ExpiryMinutes { get; set; } = 15;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
