using BidGuard.Application.Common.Validation;
using FluentValidation;

namespace BidGuard.Application.Features.Commands.Rules.CreateRule;

public sealed class CreateRuleCommandValidator : AbstractValidator<CreateRuleCommand>
{
    public CreateRuleCommandValidator()
    {
        RuleFor(x => x.TenderId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Weight).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.Expression).ValidRuleExpression();
        RuleFor(x => x.SourceClause).MaximumLength(4000);
        RuleFor(x => x.SourcePage).GreaterThan(0).When(x => x.SourcePage.HasValue);
        RuleFor(x => x.ExemptionKind).IsInEnum().When(x => x.ExemptionKind.HasValue);
    }
}
