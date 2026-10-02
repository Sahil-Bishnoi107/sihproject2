using BidGuard.Application.Common.Dtos;
using Mediator;

namespace BidGuard.Application.Features.Queries.Tenders.GetTenderRules;

public sealed record GetTenderRulesQuery(Guid TenderId) : IQuery<IReadOnlyList<RuleDto>>;
