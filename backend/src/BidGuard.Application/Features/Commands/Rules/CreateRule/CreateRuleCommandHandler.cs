using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Rules.CreateRule;

public sealed class CreateRuleCommandHandler(
    ITenderRepository tenders,
    IComplianceRuleRepository rules,
    ICacheService cache,
    IAuditService audit,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateRuleCommand, RuleDto>
{
    public async ValueTask<RuleDto> Handle(CreateRuleCommand command, CancellationToken ct)
    {
        var tender = await tenders.GetByIdAsync(command.TenderId, ct) ?? throw new NotFoundException(nameof(Tender), command.TenderId);
        tender.EnsureEditable();

        var existing = await rules.GetByTenderAsync(tender.Id, ct);
        if (existing.Any(r => string.Equals(r.Code, command.Code.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new ConflictException($"Rule code {command.Code} already exists on this tender.");

        var rule = ComplianceRule.CreateApproved(tender.Id, command.Code, command.Title, command.Category, command.IsMandatory,
            command.Weight, command.Expression.GetRawText(), command.SourceClause, command.SourcePage, command.ExemptionKind);

        await rules.AddAsync(rule, ct);
        await audit.LogAsync("RuleCreated", nameof(ComplianceRule), rule.Id.ToString(), new { rule.Code, rule.Expression }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKeys.TenderRules(tender.Id), ct);
        return RuleDto.From(rule);
    }
}
