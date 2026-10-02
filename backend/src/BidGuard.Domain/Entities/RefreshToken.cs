using BidGuard.Domain.Common;

namespace BidGuard.Domain.Entities;

/// <summary>Opaque refresh token; only its SHA-256 hash is persisted.</summary>
public class RefreshToken : BaseEntity
{
    private RefreshToken() { }

    public RefreshToken(Guid userId, string tokenHash, DateTime expiresAt, string? createdByIp)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedByIp = createdByIp;
    }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public string? CreatedByIp { get; private set; }

    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive(DateTime now) => !IsRevoked && ExpiresAt > now;

    public void Revoke(DateTime now, string? replacedByTokenHash = null)
    {
        if (IsRevoked) return;
        RevokedAt = now;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
