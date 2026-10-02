using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Application.Services.Bids;
using BidGuard.Application.Services.Evaluation;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Bids.OverrideEvaluation;

public sealed class OverrideEvaluationCommandHandler(
    IBidRepository bids,
    IComplianceRuleRepository rules,
    IRuleEvaluationRepository evaluations,
    IScoringService scoring,
    ICurrentUserService currentUser,
    IAuditService audit,
    IUnitOfWork unitOfWork) : ICommandHandler<OverrideEvaluationCommand, BidDto>
{
    public async ValueTask<BidDto> Handle(OverrideEvaluationCommand command, CancellationToken ct)
    {
        var bid = await bids.GetByIdAsync(command.BidId, ct) ?? throw new NotFoundException(nameof(Bid), command.BidId);
        BidAccess.EnsureCanRead(bid, currentUser);

        var all = await evaluations.GetByBidAsync(bid.Id, ct);
        var target = all.FirstOrDefault(e => e.Id == command.EvaluationId)
                     ?? throw new NotFoundException(nameof(RuleEvaluation), command.EvaluationId);

        var before = target.EffectiveOutcome;
        target.Override(command.Outcome, command.Reason, currentUser.UserId!.Value);

        var ruleById = (await rules.GetByTenderAsync(bid.TenderId, ct)).ToDictionary(r => r.Id);
        var score = scoring.Score(all.Where(e => ruleById.ContainsKey(e.RuleId))
            .Select(e => (ruleById[e.RuleId], e.EffectiveOutcome)).ToList());
        bid.Rescore(score.Score, score.Decision);

        await audit.LogAsync("EvaluationOverridden", nameof(Bid), bid.Id.ToString(), new
        {
            EvaluationId = target.Id,
            Rule = ruleById.GetValueOrDefault(target.RuleId)?.Code,
            From = before.ToString(),
            To = command.Outcome.ToString(),
            command.Reason,
            NewScore = score.Score,
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return BidDto.From(bid);
    }
}
