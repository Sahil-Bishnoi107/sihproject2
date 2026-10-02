using BidGuard.Application.Common.Models;
using Mediator;

namespace BidGuard.Application.Features.Queries.Audit.GetAuditLogs;

public sealed record AuditLogDto(long Sequence, DateTime Timestamp, Guid? ActorId, string Action, string EntityType,
    string EntityId, string PayloadJson, string Hash);

public sealed record GetAuditLogsQuery(string? EntityType, string? EntityId, int Page = 1, int PageSize = 50)
    : IQuery<PagedResult<AuditLogDto>>;
