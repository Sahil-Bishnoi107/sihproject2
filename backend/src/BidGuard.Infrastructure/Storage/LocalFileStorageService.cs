using BidGuard.Application.Common.Interfaces;

namespace BidGuard.Infrastructure.Storage;

public sealed class LocalStorageSettings
{
    /// <summary>Directory for uploaded files; mount a persistent volume here in production.</summary>
    public string RootPath { get; init; } = "/data/uploads";
}

/// <summary>
/// Disk-backed object storage for single-container deployments that have a persistent volume but no S3/MinIO.
/// </summary>
public sealed class LocalFileStorageService(LocalStorageSettings settings) : IFileStorageService
{
    private readonly string _root = Path.GetFullPath(settings.RootPath);

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, ct);
    }

    public Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
    {
        var path = Resolve(key);
        if (!File.Exists(path)) throw new FileNotFoundException($"Stored file '{key}' not found.");
        return Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));
    }

    /// <summary>Keys are relative paths; anything that would land outside the root is rejected.</summary>
    private string Resolve(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || Path.IsPathRooted(key) || key.StartsWith('/') || key.StartsWith('\\'))
            throw new ArgumentException("Storage key must be a relative path.", nameof(key));
        var full = Path.GetFullPath(Path.Combine(_root, key));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("Storage key escapes the storage root.", nameof(key));
        return full;
    }
}
