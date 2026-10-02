using FluentValidation;

namespace BidGuard.Application.Features.Commands.Auth.Logout;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512);
}
