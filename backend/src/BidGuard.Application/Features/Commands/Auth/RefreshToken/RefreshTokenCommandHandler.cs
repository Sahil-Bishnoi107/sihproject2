using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Application.Services.Identity;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Auth.RefreshToken;

/// <summary>
/// Rotating refresh tokens: each use revokes the presented token and issues a new one. Presenting an
/// already-revoked token means it was stolen or replayed, so every active token of that user is revoked.
/// </summary>
public sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    IJwtTokenService jwt,
    AuthTokenIssuer issuer,
    IAuditService audit,
    IUnitOfWork unitOfWork,
    TimeProvider time) : ICommandHandler<RefreshTokenCommand, AuthResult>
{
    public async ValueTask<AuthResult> Handle(RefreshTokenCommand command, CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var token = await refreshTokens.GetByHashAsync(jwt.HashToken(command.RefreshToken), ct)
                    ?? throw new UnauthorizedException("Invalid refresh token.");

        if (token.IsRevoked)
        {
            foreach (var active in await refreshTokens.GetActiveForUserAsync(token.UserId, now, ct))
                active.Revoke(now);
            await audit.LogAsync("RefreshTokenReuseDetected", "User", token.UserId.ToString(), new { token.Id }, ct);
            await unitOfWork.SaveChangesAsync(ct);
            throw new UnauthorizedException("Refresh token reuse detected; all sessions have been revoked.");
        }

        if (!token.IsActive(now))
            throw new UnauthorizedException("Refresh token expired.");

        var user = await users.GetByIdAsync(token.UserId, ct);
        if (user is null || !user.IsActive)
            throw new UnauthorizedException("Invalid refresh token.");

        var result = await issuer.IssueAsync(user, ct);
        token.Revoke(now, jwt.HashToken(result.RefreshToken));

        await unitOfWork.SaveChangesAsync(ct);
        return result;
    }
}
