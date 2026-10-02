using BidGuard.Domain.Common;

namespace BidGuard.Domain.Entities;

/// <summary>One entity value pulled from a bid document, with its location for evidence deep-linking.</summary>
public class ExtractedField : BaseEntity
{
    private ExtractedField() { }

    public ExtractedField(Guid bidId, Guid documentId, string key, string value, decimal confidence, int? page, string? boundingBox)
    {
        BidId = bidId;
        DocumentId = documentId;
        Key = key.Trim().ToLowerInvariant();
        Value = value.Trim();
        Confidence = Math.Clamp(confidence, 0m, 1m);
        Page = page;
        BoundingBox = boundingBox;
    }

    public Guid BidId { get; private set; }
    public Guid DocumentId { get; private set; }
    public string Key { get; private set; } = default!;
    public string Value { get; private set; } = default!;
    public decimal Confidence { get; private set; }
    public int? Page { get; private set; }
    /// <summary>JSON array [x0, y0, x1, y1] in PDF points.</summary>
    public string? BoundingBox { get; private set; }
}
