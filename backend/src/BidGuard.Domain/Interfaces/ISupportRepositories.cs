using BidGuard.Domain.Entities;
using BidGuard.Domain.ValueObjects;

namespace BidGuard.Domain.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<AuditLog?> GetLastAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetAllOrderedAsync(CancellationToken ct = default);
    Task<(IReadOnlyList<AuditLog> Items, int Total)> ListAsync(string? entityType, string? entityId, int page, int pageSize, CancellationToken ct = default);
}

public interface INotificationRepository : IRepository<Notification>
{
    Task<IReadOnlyList<Notification>> GetForUserAsync(Guid userId, bool unreadOnly, CancellationToken ct = default);
}

public interface IReportingRepository
{
    Task<ComplianceStats> GetComplianceStatsAsync(CancellationToken ct = default);
}
