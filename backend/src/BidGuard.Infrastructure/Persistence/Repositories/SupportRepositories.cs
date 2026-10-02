using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using BidGuard.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace BidGuard.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository(BidGuardDbContext db) : Repository<AuditLog>(db), IAuditLogRepository
{
    public Task<AuditLog?> GetLastAsync(CancellationToken ct = default) =>
        Set.OrderByDescending(a => a.Sequence).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<AuditLog>> GetAllOrderedAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().OrderBy(a => a.Sequence).ToListAsync(ct);

    public async Task<(IReadOnlyList<AuditLog> Items, int Total)> ListAsync(string? entityType, string? entityId, int page, int pageSize,
        CancellationToken ct = default)
    {
        var query = Set.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(a => a.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(entityId)) query = query.Where(a => a.EntityId == entityId);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(a => a.Sequence).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }
}

public sealed class NotificationRepository(BidGuardDbContext db) : Repository<Notification>(db), INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetForUserAsync(Guid userId, bool unreadOnly, CancellationToken ct = default) =>
        await Set.Where(n => n.UserId == userId && (!unreadOnly || !n.IsRead))
            .OrderByDescending(n => n.CreatedAt).Take(100)
            .ToListAsync(ct);
}

public sealed class ReportingRepository(BidGuardDbContext db) : IReportingRepository
{
    public async Task<ComplianceStats> GetComplianceStatsAsync(CancellationToken ct = default)
    {
        var totalTenders = await db.Tenders.CountAsync(ct);
        var publishedTenders = await db.Tenders.CountAsync(t => t.Status == TenderStatus.Published, ct);

        var officialBids = db.Bids.AsNoTracking().Where(b => !b.IsSimulation);
        var totalBids = await officialBids.CountAsync(ct);

        var decisions = await officialBids
            .Where(b => b.Decision != null || b.SuggestedDecision != null)
            .Select(b => b.Decision ?? b.SuggestedDecision)
            .ToListAsync(ct);
        var byDecision = decisions
            .GroupBy(d => d!.Value.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var officialBidIds = officialBids.Select(b => b.Id);
        var failures = await db.RuleEvaluations.AsNoTracking()
            .Where(e => officialBidIds.Contains(e.BidId)
                        && (e.OfficerOverride == RuleOutcome.Fail || (e.OfficerOverride == null && e.Outcome == RuleOutcome.Fail)))
            .GroupBy(e => new { e.Rule.Code, e.Rule.Title })
            .Select(g => new { g.Key.Code, g.Key.Title, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(ct);

        return new ComplianceStats(totalTenders, publishedTenders, totalBids, byDecision,
            failures.Select(f => new RuleFailureStat(f.Code, f.Title, f.Count)).ToList());
    }
}
