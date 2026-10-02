using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace BidGuard.Application.Tests.Services;

public class AuditHasherTests
{
    private static List<AuditLog> BuildChain(int count)
    {
        var chain = new List<AuditLog>();
        var prev = AuditLog.GenesisHash;
        for (var i = 1; i <= count; i++)
        {
            var ts = new DateTime(2026, 9, 29, 10, 0, i, DateTimeKind.Utc);
            var payload = $$"""{"n":{{i}}}""";
            var hash = AuditHasher.Compute(i, prev, ts, null, "Test", "Bid", $"b{i}", payload);
            chain.Add(new AuditLog(i, ts, null, "Test", "Bid", $"b{i}", payload, prev, hash));
            prev = hash;
        }
        return chain;
    }

    [Fact]
    public void Intact_chain_verifies()
    {
        AuditHasher.VerifyChain(BuildChain(3)).Should().BeNull();
    }

    [Fact]
    public void Tampered_payload_is_detected_at_that_entry()
    {
        var chain = BuildChain(3);
        var middle = chain[1];
        chain[1] = new AuditLog(middle.Sequence, middle.Timestamp, middle.ActorId, middle.Action, middle.EntityType,
            middle.EntityId, """{"n":999}""", middle.PrevHash, middle.Hash);

        AuditHasher.VerifyChain(chain).Should().Be(chain[1].Id);
    }

    [Fact]
    public void Deleted_entry_breaks_the_chain()
    {
        var chain = BuildChain(3);
        chain.RemoveAt(1);

        AuditHasher.VerifyChain(chain).Should().Be(chain[1].Id);
    }

    [Fact]
    public void Hash_is_64_lowercase_hex_chars()
    {
        AuditHasher.Compute(1, AuditLog.GenesisHash, DateTime.UtcNow, Guid.NewGuid(), "a", "b", "c", "{}")
            .Should().MatchRegex("^[0-9a-f]{64}$");
    }
}

public class AuditServiceTests
{
    private sealed class FakeAuditRepo : IAuditLogRepository
    {
        public List<AuditLog> Saved { get; } = [];
        public List<AuditLog> Added { get; } = [];

        public Task<AuditLog?> GetLastAsync(CancellationToken ct = default) =>
            Task.FromResult(Saved.OrderBy(a => a.Sequence).LastOrDefault());

        public Task AddAsync(AuditLog entity, CancellationToken ct = default)
        {
            Added.Add(entity);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditLog>> GetAllOrderedAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<AuditLog> Items, int Total)> ListAsync(string? entityType, string? entityId, int page, int pageSize, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddRangeAsync(IEnumerable<AuditLog> entities, CancellationToken ct = default) => throw new NotSupportedException();
        public void Remove(AuditLog entity) => throw new NotSupportedException();
        public void RemoveRange(IEnumerable<AuditLog> entities) => throw new NotSupportedException();
    }

    private sealed class FakeUser(Guid? id) : ICurrentUserService
    {
        public Guid? UserId => id;
        public string? Email => null;
        public UserRole? Role => null;
        public Guid? OrganizationId => null;
        public string? IpAddress => null;
    }

    [Fact]
    public async Task Two_entries_in_one_scope_chain_to_each_other()
    {
        var repo = new FakeAuditRepo();
        var service = new AuditService(repo, new FakeUser(Guid.NewGuid()), new FakeTimeProvider(DateTimeOffset.UtcNow));

        await service.LogAsync("TenderCreated", "Tender", "t1", new { title = "x" });
        await service.LogAsync("RuleApproved", "ComplianceRule", "r1", null);

        repo.Added.Should().HaveCount(2);
        repo.Added[0].PrevHash.Should().Be(AuditLog.GenesisHash);
        repo.Added[1].PrevHash.Should().Be(repo.Added[0].Hash);
        repo.Added[1].Sequence.Should().Be(repo.Added[0].Sequence + 1);
        AuditHasher.VerifyChain(repo.Added).Should().BeNull();
    }

    [Fact]
    public async Task Continues_from_last_persisted_entry()
    {
        var repo = new FakeAuditRepo();
        var first = new AuditService(repo, new FakeUser(null), new FakeTimeProvider(DateTimeOffset.UtcNow));
        await first.LogAsync("A", "X", "1", null);
        repo.Saved.AddRange(repo.Added);
        repo.Added.Clear();

        var second = new AuditService(repo, new FakeUser(null), new FakeTimeProvider(DateTimeOffset.UtcNow));
        await second.LogAsync("B", "X", "2", null);

        repo.Added[0].PrevHash.Should().Be(repo.Saved[0].Hash);
        repo.Added[0].Sequence.Should().Be(2);
    }

    [Fact]
    public async Task Timestamp_is_truncated_to_microseconds_for_db_round_trip()
    {
        var repo = new FakeAuditRepo();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 9, 29, 10, 0, 0, TimeSpan.Zero).AddTicks(1234567));
        var service = new AuditService(repo, new FakeUser(null), time);

        await service.LogAsync("A", "X", "1", null);

        (repo.Added[0].Timestamp.Ticks % 10).Should().Be(0);
        repo.Added[0].Timestamp.Kind.Should().Be(DateTimeKind.Utc);
    }
}
