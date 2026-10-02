using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Rules.DeleteRule;

public sealed class DeleteRuleCommandHandler(
    ITenderRepository tenders,
    IComplianceRuleRepository rules,
    ICacheService cache,
    IAuditService audit,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteRuleCommand>
{
    public async ValueTask<Unit> Handle(DeleteRuleCommand command, CancellationToken ct)
    {
        var tender = await tenders.GetByIdAsync(command.TenderId, ct) ?? throw new NotFoundException(nameof(Tender), command.TenderId);
        tender.EnsureEditable();

        var rule = await rules.GetByIdAsync(command.RuleId, ct);
        if (rule is null || rule.TenderId != tender.Id)
            throw new NotFoundException(nameof(ComplianceRule), command.RuleId);

        rules.Remove(rule);
        await audit.LogAsync("RuleDeleted", nameof(ComplianceRule), rule.Id.ToString(), new { rule.Code, rule.Expression }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKeys.TenderRules(tender.Id), ct);
        return Unit.Value;
    }
}
