using BidGuard.Domain.Common;
using BidGuard.Domain.Enums;

namespace BidGuard.Domain.Entities;

/// <summary>Metadata for a file held in object storage; bytes never live in the database.</summary>
public class Document : BaseEntity
{
    private Document() { }

    public Document(DocumentOwnerType ownerType, Guid ownerId, string fileName, string storageKey, string contentType,
        long sizeBytes, string sha256, string? docType, Guid uploadedById)
    {
        OwnerType = ownerType;
        OwnerId = ownerId;
        FileName = fileName;
        StorageKey = storageKey;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        Sha256 = sha256;
        DocType = docType;
        UploadedById = uploadedById;
        ExtractionStatus = ExtractionStatus.Pending;
    }

    public DocumentOwnerType OwnerType { get; private set; }
    public Guid OwnerId { get; private set; }
    public string FileName { get; private set; } = default!;
    public string StorageKey { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public long SizeBytes { get; private set; }
    public string Sha256 { get; private set; } = default!;
    public string? DocType { get; private set; }
    public bool OcrUsed { get; private set; }
    public ExtractionStatus ExtractionStatus { get; private set; }
    public Guid UploadedById { get; private set; }

    public void MarkExtracted(bool ocrUsed)
    {
        OcrUsed = ocrUsed;
        ExtractionStatus = ExtractionStatus.Completed;
        Touch();
    }

    public void MarkExtractionFailed()
    {
        ExtractionStatus = ExtractionStatus.Failed;
        Touch();
    }
}
