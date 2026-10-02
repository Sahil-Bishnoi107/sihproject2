using BidGuard.Domain.Entities;

namespace BidGuard.Application.Common.Interfaces;

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public interface IJwtTokenService
{
    AccessToken CreateAccessToken(User user);
    string GenerateRefreshToken();
    string HashToken(string token);
    TimeSpan RefreshTokenLifetime { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
