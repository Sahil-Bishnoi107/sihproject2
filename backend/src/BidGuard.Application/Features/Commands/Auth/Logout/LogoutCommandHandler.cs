using BidGuard.Application.Common.Interfaces;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Auth.Logout;

public sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokens,
    IJwtTokenService jwt,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider time) : ICommandHandler<LogoutCommand>
{
    public async ValueTask<Unit> Handle(LogoutCommand command, CancellationToken ct)
    {
        var token = await refreshTokens.GetByHashAsync(jwt.HashToken(command.RefreshToken), ct);

        // Only the owner may revoke; unknown tokens are ignored so logout is idempotent.
        if (token is not null && token.UserId == currentUser.UserId)
        {
            token.Revoke(time.GetUtcNow().UtcDateTime);
            await unitOfWork.SaveChangesAsync(ct);
        }
        return Unit.Value;
    }
}
