using BidGuard.Application.Common.Interfaces;
using Minio;
using Minio.DataModel.Args;

namespace BidGuard.Infrastructure.Storage;

public sealed class MinioSettings
{
    public const string SectionName = "Minio";

    public string Endpoint { get; init; } = "localhost:9000";
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string Bucket { get; init; } = "bidguard-documents";
    public bool UseSsl { get; init; }
}

/// <summary>S3-compatible object storage (MinIO locally; Cloudflare R2 / S3 in production). Only keys go to Postgres.</summary>
public sealed class MinioFileStorageService(IMinioClient client, MinioSettings settings) : IFileStorageService
{
    private volatile bool _bucketReady;

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        await EnsureBucketAsync(ct);

        Stream upload = content;
        if (!content.CanSeek)
        {
            var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            upload = buffer;
        }
        upload.Position = 0;

        await client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(settings.Bucket)
            .WithObject(key)
            .WithStreamData(upload)
            .WithObjectSize(upload.Length)
            .WithContentType(contentType), ct);
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
    {
        var buffer = new MemoryStream();
        await client.GetObjectAsync(new GetObjectArgs()
            .WithBucket(settings.Bucket)
            .WithObject(key)
            .WithCallbackStream(async (stream, token) => await stream.CopyToAsync(buffer, token)), ct);
        buffer.Position = 0;
        return buffer;
    }

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        if (_bucketReady) return;
        var exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(settings.Bucket), ct);
        if (!exists)
            await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(settings.Bucket), ct);
        _bucketReady = true;
    }
}
