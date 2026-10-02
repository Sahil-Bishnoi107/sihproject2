using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Domain.Enums;
using BidGuard.Infrastructure.Identity;
using BidGuard.Infrastructure.Persistence;
using BidGuard.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace BidGuard.Application.Tests.TestSupport;

public sealed class TestCurrentUser : ICurrentUserService
{
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public UserRole? Role { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? IpAddress { get; set; } = "127.0.0.1";
}

/// <summary>Real repositories over an isolated EF InMemory database.</summary>
public sealed class TestHarness : IDisposable
{
    public TestHarness()
    {
        var options = new DbContextOptionsBuilder<BidGuardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        Db = new BidGuardDbContext(options);
        Time = new FakeTimeProvider(new DateTimeOffset(2026, 9, 29, 9, 0, 0, TimeSpan.Zero));
        Jwt = new JwtTokenService(Options.Create(new JwtSettings
        {
            Issuer = "bidguard-tests",
            Audience = "bidguard-tests",
            Secret = "test-secret-key-that-is-at-least-32-bytes-long!!",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7,
        }), Time);
        Audit = new AuditService(AuditLogs, CurrentUser, Time);
    }

    public BidGuardDbContext Db { get; }
    public FakeTimeProvider Time { get; }
    public TestCurrentUser CurrentUser { get; } = new();
    public JwtTokenService Jwt { get; }
    public PasswordHasher Hasher { get; } = new();
    public AuditService Audit { get; }
    public InMemoryFileStorage Storage { get; } = new();
    public RecordingJobScheduler Scheduler { get; } = new();
    public FakeDocumentIntelligence Ai { get; } = new();
    public PassThroughCache Cache { get; } = new();

    public UserRepository Users => new(Db);
    public RefreshTokenRepository RefreshTokens => new(Db);
    public OrganizationRepository Organizations => new(Db);
    public CredentialRepository Credentials => new(Db);
    public TenderRepository Tenders => new(Db);
    public ComplianceRuleRepository Rules => new(Db);
    public BidRepository Bids => new(Db);
    public DocumentRepository Documents => new(Db);
    public ExtractedFieldRepository ExtractedFields => new(Db);
    public VerificationResultRepository Verifications => new(Db);
    public RuleEvaluationRepository Evaluations => new(Db);
    public AuditLogRepository AuditLogs => new(Db);
    public NotificationRepository Notifications => new(Db);

    public void Dispose() => Db.Dispose();
}
