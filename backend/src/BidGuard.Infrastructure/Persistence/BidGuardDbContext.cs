using BidGuard.Domain.Entities;
using BidGuard.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BidGuard.Infrastructure.Persistence;

public sealed class BidGuardDbContext(DbContextOptions<BidGuardDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Credential> Credentials => Set<Credential>();
    public DbSet<Tender> Tenders => Set<Tender>();
    public DbSet<ComplianceRule> ComplianceRules => Set<ComplianceRule>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ExtractedField> ExtractedFields => Set<ExtractedField>();
    public DbSet<VerificationResult> VerificationResults => Set<VerificationResult>();
    public DbSet<RuleEvaluation> RuleEvaluations => Set<RuleEvaluation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BidGuardDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
