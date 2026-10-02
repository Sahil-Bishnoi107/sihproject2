using BidGuard.Application.Common.Models;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Queries.Audit.GetAuditLogs;

public sealed class GetAuditLogsQueryHandler(IAuditLogRepository repository)
    : IQueryHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    public async ValueTask<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery query, CancellationToken ct)
    {
        var (items, total) = await repository.ListAsync(query.EntityType, query.EntityId, query.Page, query.PageSize, ct);
        return new PagedResult<AuditLogDto>(
            items.Select(a => new AuditLogDto(a.Sequence, a.Timestamp, a.ActorId, a.Action, a.EntityType, a.EntityId, a.PayloadJson, a.Hash)).ToList(),
            query.Page, query.PageSize, total);
    }
}
