using FluentValidation;

namespace BidGuard.Application.Features.Commands.Organizations.AddCredential;

public sealed class AddCredentialCommandValidator : AbstractValidator<AddCredentialCommand>
{
    public AddCredentialCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Number).NotEmpty().MaximumLength(64);
        RuleFor(x => x.ExpiresOn).GreaterThanOrEqualTo(x => x.IssuedOn!.Value)
            .When(x => x.IssuedOn.HasValue && x.ExpiresOn.HasValue)
            .WithMessage("Expiry date cannot be before the issue date.");
    }
}
