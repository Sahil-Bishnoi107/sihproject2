using BidGuard.Domain.Common;

namespace BidGuard.Domain.Entities;

/// <summary>
/// Append-only, hash-chained audit entry. <see cref="Hash"/> covers this entry plus <see cref="PrevHash"/>,
/// so altering any historical row breaks every hash after it.
/// </summary>
public class AuditLog : BaseEntity
{
    public const string GenesisHash = "0000000000000000000000000000000000000000000000000000000000000000";

    private AuditLog() { }

    public AuditLog(long sequence, DateTime timestamp, Guid? actorId, string action, string entityType, string entityId,
        string payloadJson, string prevHash, string hash)
    {
        Sequence = sequence;
        Timestamp = timestamp;
        ActorId = actorId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        PayloadJson = payloadJson;
        PrevHash = prevHash;
        Hash = hash;
    }

    public long Sequence { get; private set; }
    public DateTime Timestamp { get; private set; }
    public Guid? ActorId { get; private set; }
    public string Action { get; private set; } = default!;
    public string EntityType { get; private set; } = default!;
    public string EntityId { get; private set; } = default!;
    public string PayloadJson { get; private set; } = default!;
    public string PrevHash { get; private set; } = default!;
    public string Hash { get; private set; } = default!;
}
