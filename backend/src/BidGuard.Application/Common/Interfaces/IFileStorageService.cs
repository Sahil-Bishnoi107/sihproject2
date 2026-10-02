namespace BidGuard.Application.Common.Interfaces;

/// <summary>Object storage port (MinIO locally; S3 / Cloudflare R2 in production).</summary>
public interface IFileStorageService
{
    Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string key, CancellationToken ct = default);
}
