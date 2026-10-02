using BidGuard.Domain.Enums;
using FluentValidation;

namespace BidGuard.Application.Features.Commands.Bids.RecordDecision;

public sealed class RecordDecisionCommandValidator : AbstractValidator<RecordDecisionCommand>
{
    public RecordDecisionCommandValidator()
    {
        RuleFor(x => x.BidId).NotEmpty();
        RuleFor(x => x.Decision).IsInEnum();
        RuleFor(x => x.Remarks).MaximumLength(2000);
        RuleFor(x => x.Remarks).NotEmpty().When(x => x.Decision != BidDecision.Compliant)
            .WithMessage("Remarks are required when a bid is not accepted as compliant.");
    }
}
