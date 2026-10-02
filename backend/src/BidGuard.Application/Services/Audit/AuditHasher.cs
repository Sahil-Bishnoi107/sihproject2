using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BidGuard.Domain.Entities;

namespace BidGuard.Application.Services.Audit;

/// <summary>SHA-256 hash chain over audit entries: hash(n) = SHA256(canonical(n) | hash(n-1)).</summary>
public static class AuditHasher
{
    public static string Compute(long sequence, string prevHash, DateTime timestamp, Guid? actorId, string action,
        string entityType, string entityId, string payloadJson)
    {
        var canonical = string.Join('|',
            sequence.ToString(CultureInfo.InvariantCulture),
            prevHash,
            DateTime.SpecifyKind(timestamp, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture),
            actorId?.ToString("N") ?? "-",
            action,
            entityType,
            entityId,
            payloadJson);

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    public static string Compute(AuditLog e) =>
        Compute(e.Sequence, e.PrevHash, e.Timestamp, e.ActorId, e.Action, e.EntityType, e.EntityId, e.PayloadJson);

    /// <summary>Returns the id of the first entry whose link or content no longer matches, or null if intact.</summary>
    public static Guid? VerifyChain(IEnumerable<AuditLog> orderedEntries)
    {
        var expectedPrev = AuditLog.GenesisHash;
        foreach (var entry in orderedEntries)
        {
            if (entry.PrevHash != expectedPrev || Compute(entry) != entry.Hash)
                return entry.Id;
            expectedPrev = entry.Hash;
        }
        return null;
    }
}
