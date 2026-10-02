using System.Security.Cryptography;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;

namespace BidGuard.Application.Services.Documents;

public static class UploadLimits
{
    public const long MaxBytes = 25L * 1024 * 1024;

    public static readonly IReadOnlySet<string> BidDocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".png", ".jpg", ".jpeg" };

    public static bool IsPdf(string fileName) => Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    public static bool IsAllowedBidDocument(string fileName) => BidDocumentExtensions.Contains(Path.GetExtension(fileName));

    /// <summary>
    /// Content type derived from the (validated) extension. The client-supplied header is never trusted:
    /// it is echoed back on download, so "text/html" would turn an upload into stored XSS.
    /// </summary>
    public static string ContentTypeFor(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => "application/octet-stream",
    };
}

/// <summary>Stores file bytes in object storage and registers a <see cref="Document"/> row (caller saves).</summary>
public sealed class DocumentUploader(IFileStorageService storage, IDocumentRepository documents)
{
    public async Task<Document> UploadAsync(DocumentOwnerType ownerType, Guid ownerId, string fileName, string contentType,
        Stream content, string? docType, Guid uploadedById, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        var sha256 = Convert.ToHexStringLower(SHA256.HashData(buffer.ToArray()));
        var safeName = SanitizeFileName(fileName);
        contentType = UploadLimits.ContentTypeFor(safeName);
        var documentId = Guid.CreateVersion7();
        var key = $"{ownerType.ToString().ToLowerInvariant()}s/{ownerId}/{documentId:N}-{safeName}";

        await storage.UploadAsync(key, buffer, contentType, ct);

        var document = new Document(ownerType, ownerId, safeName, key, contentType, buffer.Length, sha256, docType, uploadedById);
        await documents.AddAsync(document, ct);
        return document;
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) || char.IsWhiteSpace(c) ? '_' : c).ToArray());
        return cleaned.Length > 200 ? cleaned[^200..] : cleaned;
    }
}
