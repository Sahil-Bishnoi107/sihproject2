using System.Text.Json;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Interfaces;

namespace BidGuard.Application.Services.Audit;

public interface IAuditService
{
    /// <summary>Appends a chained audit entry to the current unit of work (persisted by the caller's SaveChanges).</summary>
    Task LogAsync(string action, string entityType, string entityId, object? payload, CancellationToken ct = default);
}

public sealed class AuditService(IAuditLogRepository repository, ICurrentUserService currentUser, TimeProvider time) : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Entries added in this scope but not yet saved; the next entry must chain from them, not from the DB.
    private AuditLog? _pendingLast;

    public async Task LogAsync(string action, string entityType, string entityId, object? payload, CancellationToken ct = default)
    {
        var last = _pendingLast ?? await repository.GetLastAsync(ct);
        var sequence = (last?.Sequence ?? 0) + 1;
        var prevHash = last?.Hash ?? AuditLog.GenesisHash;

        // Postgres timestamptz stores microseconds; truncate so the hash survives a round trip.
        var now = time.GetUtcNow().UtcDateTime;
        var timestamp = new DateTime(now.Ticks - now.Ticks % 10, DateTimeKind.Utc);

        var payloadJson = payload is null ? "{}" : JsonSerializer.Serialize(payload, JsonOptions);
        var actorId = currentUser.UserId;
        var hash = AuditHasher.Compute(sequence, prevHash, timestamp, actorId, action, entityType, entityId, payloadJson);

        var entry = new AuditLog(sequence, timestamp, actorId, action, entityType, entityId, payloadJson, prevHash, hash);
        await repository.AddAsync(entry, ct);
        _pendingLast = entry;
    }
}
