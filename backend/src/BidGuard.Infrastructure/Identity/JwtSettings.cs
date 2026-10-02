namespace BidGuard.Infrastructure.Identity;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "bidguard";
    public string Audience { get; init; } = "bidguard-clients";
    /// <summary>HMAC-SHA256 signing key; must be at least 32 bytes.</summary>
    public string Secret { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 7;
}
