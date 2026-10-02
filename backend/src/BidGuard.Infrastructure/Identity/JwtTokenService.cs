using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BidGuard.Infrastructure.Identity;

public static class BidGuardClaims
{
    public const string UserId = "user_id";
    public const string Email = "email";
    public const string Role = "role";
    public const string OrganizationId = "org_id";
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly TimeProvider _time;
    private readonly SigningCredentials _credentials;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtTokenService(IOptions<JwtSettings> options, TimeProvider time)
    {
        _settings = options.Value;
        _time = time;
        var keyBytes = Encoding.UTF8.GetBytes(_settings.Secret);
        if (keyBytes.Length < 32)
            throw new InvalidOperationException("Jwt:Secret must be at least 32 bytes.");
        _credentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
    }

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_settings.RefreshTokenDays);

    public AccessToken CreateAccessToken(User user)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(BidGuardClaims.UserId, user.Id.ToString()),
            new(BidGuardClaims.Email, user.Email),
            new(BidGuardClaims.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        if (user.OrganizationId is { } orgId)
            claims.Add(new Claim(BidGuardClaims.OrganizationId, orgId.ToString()));

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = _credentials,
        });
        return new AccessToken(token, expires);
    }

    public string GenerateRefreshToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

    public string HashToken(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
