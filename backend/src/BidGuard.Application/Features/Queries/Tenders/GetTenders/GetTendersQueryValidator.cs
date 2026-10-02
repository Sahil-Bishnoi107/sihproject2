using FluentValidation;

namespace BidGuard.Application.Features.Queries.Tenders.GetTenders;

public sealed class GetTendersQueryValidator : AbstractValidator<GetTendersQuery>
{
    public GetTendersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}
