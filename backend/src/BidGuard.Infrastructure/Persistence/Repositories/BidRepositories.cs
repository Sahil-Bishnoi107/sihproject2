using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BidGuard.Infrastructure.Persistence.Repositories;

public sealed class BidRepository(BidGuardDbContext db) : Repository<Bid>(db), IBidRepository
{
    public override Task<Bid?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(b => b.Tender).Include(b => b.Organization).FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<Bid>> GetOfficialByTenderAsync(Guid tenderId, CancellationToken ct = default) =>
        await Set.Include(b => b.Organization)
            .Where(b => b.TenderId == tenderId && !b.IsSimulation)
            .OrderByDescending(b => b.OverallScore)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Bid>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default) =>
        await Set.Include(b => b.Tender)
            .Where(b => b.OrganizationId == organizationId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

    public Task<bool> OfficialBidExistsAsync(Guid tenderId, Guid organizationId, CancellationToken ct = default) =>
        Set.AnyAsync(b => b.TenderId == tenderId && b.OrganizationId == organizationId && !b.IsSimulation, ct);
}

public sealed class DocumentRepository(BidGuardDbContext db) : Repository<Document>(db), IDocumentRepository
{
    public async Task<IReadOnlyList<Document>> GetByOwnerAsync(DocumentOwnerType ownerType, Guid ownerId, CancellationToken ct = default) =>
        await Set.Where(d => d.OwnerType == ownerType && d.OwnerId == ownerId).OrderBy(d => d.CreatedAt).ToListAsync(ct);
}

public sealed class ExtractedFieldRepository(BidGuardDbContext db) : Repository<ExtractedField>(db), IExtractedFieldRepository
{
    public async Task<IReadOnlyList<ExtractedField>> GetByBidAsync(Guid bidId, CancellationToken ct = default) =>
        await Set.Where(f => f.BidId == bidId).ToListAsync(ct);
}

public sealed class VerificationResultRepository(BidGuardDbContext db) : Repository<VerificationResult>(db), IVerificationResultRepository
{
    public async Task<IReadOnlyList<VerificationResult>> GetByBidAsync(Guid bidId, CancellationToken ct = default) =>
        await Set.Where(v => v.BidId == bidId).OrderBy(v => v.Source).ToListAsync(ct);
}

public sealed class RuleEvaluationRepository(BidGuardDbContext db) : Repository<RuleEvaluation>(db), IRuleEvaluationRepository
{
    public async Task<IReadOnlyList<RuleEvaluation>> GetByBidAsync(Guid bidId, CancellationToken ct = default) =>
        await Set.Include(e => e.Rule).Where(e => e.BidId == bidId).OrderBy(e => e.Rule.Code).ToListAsync(ct);
}
