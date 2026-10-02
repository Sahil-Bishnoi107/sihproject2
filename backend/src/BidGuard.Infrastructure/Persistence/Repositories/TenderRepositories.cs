using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BidGuard.Infrastructure.Persistence.Repositories;

public sealed class TenderRepository(BidGuardDbContext db) : Repository<Tender>(db), ITenderRepository
{
    public Task<bool> GemBidNumberExistsAsync(string gemBidNumber, CancellationToken ct = default)
    {
        var normalized = gemBidNumber.Trim().ToUpperInvariant();
        return Set.AnyAsync(t => t.GemBidNumber == normalized, ct);
    }

    public async Task<(IReadOnlyList<Tender> Items, int Total)> ListAsync(TenderStatus? status, string? search, int page, int pageSize,
        CancellationToken ct = default)
    {
        var query = Set.AsNoTracking();
        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(term) || t.GemBidNumber.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }
}

public sealed class ComplianceRuleRepository(BidGuardDbContext db) : Repository<ComplianceRule>(db), IComplianceRuleRepository
{
    public async Task<IReadOnlyList<ComplianceRule>> GetByTenderAsync(Guid tenderId, CancellationToken ct = default) =>
        await Set.Where(r => r.TenderId == tenderId).OrderBy(r => r.Code).ToListAsync(ct);
}
