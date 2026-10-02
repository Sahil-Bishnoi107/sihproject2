using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Rules.ApproveRules;

public sealed class ApproveRulesCommandHandler(
    ITenderRepository tenders,
    IComplianceRuleRepository rules,
    ICacheService cache,
    IAuditService audit,
    IUnitOfWork unitOfWork) : ICommandHandler<ApproveRulesCommand, IReadOnlyList<RuleDto>>
{
    public async ValueTask<IReadOnlyList<RuleDto>> Handle(ApproveRulesCommand command, CancellationToken ct)
    {
        var tender = await tenders.GetByIdAsync(command.TenderId, ct) ?? throw new NotFoundException(nameof(Tender), command.TenderId);
        tender.EnsureEditable();

        var ruleSet = await rules.GetByTenderAsync(tender.Id, ct);
        var targets = command.RuleId is { } id
            ? [ruleSet.FirstOrDefault(r => r.Id == id) ?? throw new NotFoundException(nameof(ComplianceRule), id)]
            : ruleSet.Where(r => r.Status == RuleStatus.AiDraft).ToList();

        foreach (var rule in targets)
            rule.Approve();

        await audit.LogAsync("RulesApproved", nameof(Tender), tender.Id.ToString(), new { Codes = targets.Select(r => r.Code) }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKeys.TenderRules(tender.Id), ct);
        return targets.Select(RuleDto.From).ToList();
    }
}
