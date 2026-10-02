using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Identity;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Auth.Login;

public sealed class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    AuthTokenIssuer issuer,
    IUnitOfWork unitOfWork) : ICommandHandler<LoginCommand, AuthResult>
{
    public async ValueTask<AuthResult> Handle(LoginCommand command, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(command.Email, ct);

        // Same error for unknown email, wrong password and inactive account: no account enumeration.
        if (user is null || !user.IsActive || !hasher.Verify(command.Password, user.PasswordHash))
            throw new UnauthorizedException();

        var result = await issuer.IssueAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return result;
    }
}
