using BidGuard.Application.Common.Interfaces;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;

namespace BidGuard.Application.Services.Identity;

public sealed record UserDto(Guid Id, string Email, string FullName, UserRole Role, Guid? OrganizationId, bool IsActive)
{
    public static UserDto From(User u) => new(u.Id, u.Email, u.FullName, u.Role, u.OrganizationId, u.IsActive);
}

public sealed record AuthResult(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserDto User);

/// <summary>Issues an access token plus a new refresh token (only its hash is stored). Caller saves.</summary>
public sealed class AuthTokenIssuer(
    IJwtTokenService jwt,
    IRefreshTokenRepository refreshTokens,
    TimeProvider time,
    ICurrentUserService currentUser)
{
    public async Task<AuthResult> IssueAsync(User user, CancellationToken ct)
    {
        var access = jwt.CreateAccessToken(user);
        var rawRefresh = jwt.GenerateRefreshToken();
        var refreshExpiresAt = time.GetUtcNow().UtcDateTime.Add(jwt.RefreshTokenLifetime);

        await refreshTokens.AddAsync(new RefreshToken(user.Id, jwt.HashToken(rawRefresh), refreshExpiresAt, currentUser.IpAddress), ct);

        return new AuthResult(access.Token, access.ExpiresAt, rawRefresh, refreshExpiresAt, UserDto.From(user));
    }
}
