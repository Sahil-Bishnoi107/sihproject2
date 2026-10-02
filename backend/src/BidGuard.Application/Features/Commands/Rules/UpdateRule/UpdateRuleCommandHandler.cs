using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Rules.UpdateRule;

public sealed class UpdateRuleCommandHandler(
    ITenderRepository tenders,
    IComplianceRuleRepository rules,
    ICacheService cache,
    IAuditService audit,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateRuleCommand, RuleDto>
{
    public async ValueTask<RuleDto> Handle(UpdateRuleCommand command, CancellationToken ct)
    {
        var tender = await tenders.GetByIdAsync(command.TenderId, ct) ?? throw new NotFoundException(nameof(Tender), command.TenderId);
        tender.EnsureEditable();

        var ruleSet = await rules.GetByTenderAsync(tender.Id, ct);
        var rule = ruleSet.FirstOrDefault(r => r.Id == command.RuleId) ?? throw new NotFoundException(nameof(ComplianceRule), command.RuleId);
        if (ruleSet.Any(r => r.Id != rule.Id && string.Equals(r.Code, command.Code.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new ConflictException($"Rule code {command.Code} already exists on this tender.");

        var before = new { rule.Code, rule.Expression, rule.Weight, rule.IsMandatory };
        rule.Update(command.Code, command.Title, command.Category, command.IsMandatory, command.Weight,
            command.Expression.GetRawText(), command.SourceClause, command.SourcePage, command.ExemptionKind);

        await audit.LogAsync("RuleUpdated", nameof(ComplianceRule), rule.Id.ToString(),
            new { before, after = new { rule.Code, rule.Expression, rule.Weight, rule.IsMandatory } }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKeys.TenderRules(tender.Id), ct);
        return RuleDto.From(rule);
    }
}
