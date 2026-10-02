using BidGuard.Application.Common.Validation;
using BidGuard.Domain.Enums;
using FluentValidation;

namespace BidGuard.Application.Features.Commands.Users.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).StrongPassword();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).Must(r => r is UserRole.Officer or UserRole.Admin)
            .WithMessage("Only Officer or Admin accounts can be created here; bidders self-register.");
    }
}
