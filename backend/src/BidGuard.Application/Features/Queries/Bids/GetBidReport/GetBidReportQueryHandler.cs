using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Bids;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Queries.Bids.GetBidReport;

public sealed class GetBidReportQueryHandler(
    IBidRepository bids,
    IComplianceRuleRepository rules,
    IRuleEvaluationRepository evaluations,
    IVerificationResultRepository verifications,
    IDocumentRepository documents,
    ICurrentUserService currentUser) : IQueryHandler<GetBidReportQuery, BidReportDto>
{
    public async ValueTask<BidReportDto> Handle(GetBidReportQuery query, CancellationToken ct)
    {
        var bid = await bids.GetByIdAsync(query.BidId, ct) ?? throw new NotFoundException(nameof(Bid), query.BidId);
        BidAccess.EnsureCanRead(bid, currentUser);

        var ruleById = (await rules.GetByTenderAsync(bid.TenderId, ct)).ToDictionary(r => r.Id);
        var evaluationDtos = (await evaluations.GetByBidAsync(bid.Id, ct))
            .Where(e => ruleById.ContainsKey(e.RuleId))
            .Select(e => EvaluationDto.From(e, ruleById[e.RuleId]))
            .OrderByDescending(e => e.IsMandatory).ThenBy(e => e.RuleCode)
            .ToList();

        return new BidReportDto(
            BidDto.From(bid),
            TenderDto.From(bid.Tender),
            (await documents.GetByOwnerAsync(DocumentOwnerType.Bid, bid.Id, ct)).Select(DocumentDto.From).ToList(),
            (await verifications.GetByBidAsync(bid.Id, ct)).Select(VerificationDto.From).ToList(),
            evaluationDtos,
            FixPathPlanner.Plan(evaluationDtos));
    }
}
