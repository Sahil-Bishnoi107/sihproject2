using BidGuard.Application.Common.Validation;
using FluentValidation;

namespace BidGuard.Application.Features.Commands.Organizations.UpdateMyOrganization;

public sealed class UpdateMyOrganizationCommandValidator : AbstractValidator<UpdateMyOrganizationCommand>
{
    public UpdateMyOrganizationCommandValidator()
    {
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Gstin).MatchesUpper(ValidationRules.GstinRegex(), "GSTIN");
        RuleFor(x => x.UdyamNumber).MatchesUpper(ValidationRules.UdyamRegex(), "Udyam registration number");
        RuleFor(x => x.Cin).MatchesUpper(ValidationRules.CinRegex(), "CIN");
        RuleFor(x => x.EpfoCode).MaximumLength(32);
        RuleFor(x => x.DpiitNumber).MaximumLength(32);
        RuleFor(x => x.DpiitNumber).NotEmpty().When(x => x.IsStartup)
            .WithMessage("A DPIIT recognition number is required to claim startup status.");
    }
}
