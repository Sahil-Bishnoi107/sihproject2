using BidGuard.Application.Common.Validation;
using FluentValidation;

namespace BidGuard.Application.Features.Commands.Auth.RegisterBidder;

public sealed class RegisterBidderCommandValidator : AbstractValidator<RegisterBidderCommand>
{
    public RegisterBidderCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).StrongPassword();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OrganizationName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Pan).NotEmpty().MatchesUpper(ValidationRules.PanRegex(), "PAN");
        RuleFor(x => x.Category).IsInEnum();
    }
}
