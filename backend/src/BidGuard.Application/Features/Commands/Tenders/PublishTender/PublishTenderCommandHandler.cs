using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Tenders.PublishTender;

public sealed class PublishTenderCommandHandler(
    ITenderRepository tenders,
    IComplianceRuleRepository rules,
    ICacheService cache,
    IAuditService audit,
    IUnitOfWork unitOfWork) : ICommandHandler<PublishTenderCommand, TenderDto>
{
    public async ValueTask<TenderDto> Handle(PublishTenderCommand command, CancellationToken ct)
    {
        var tender = await tenders.GetByIdAsync(command.TenderId, ct) ?? throw new NotFoundException(nameof(Tender), command.TenderId);
        var ruleSet = await rules.GetByTenderAsync(tender.Id, ct);

        tender.Publish(ruleSet);
        await audit.LogAsync("TenderPublished", nameof(Tender), tender.Id.ToString(),
            new { RuleCount = ruleSet.Count, Rules = ruleSet.Select(r => new { r.Code, r.Expression }) }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKeys.TenderRules(tender.Id), ct);

        return TenderDto.From(tender);
    }
}
