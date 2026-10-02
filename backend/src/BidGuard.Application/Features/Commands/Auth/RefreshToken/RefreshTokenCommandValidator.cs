using FluentValidation;

namespace BidGuard.Application.Features.Commands.Auth.RefreshToken;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512);
}
