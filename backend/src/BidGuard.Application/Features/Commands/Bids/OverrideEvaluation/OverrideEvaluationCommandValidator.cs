using FluentValidation;

namespace BidGuard.Application.Features.Commands.Bids.OverrideEvaluation;

public sealed class OverrideEvaluationCommandValidator : AbstractValidator<OverrideEvaluationCommand>
{
    public OverrideEvaluationCommandValidator()
    {
        RuleFor(x => x.BidId).NotEmpty();
        RuleFor(x => x.EvaluationId).NotEmpty();
        RuleFor(x => x.Outcome).IsInEnum();
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(10).MaximumLength(1000)
            .WithMessage("Explain the override in at least 10 characters; it is recorded in the audit log.");
    }
}
