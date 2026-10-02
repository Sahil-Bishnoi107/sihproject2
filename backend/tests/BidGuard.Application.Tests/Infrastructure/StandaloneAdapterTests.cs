using BidGuard.Infrastructure.Caching;
using BidGuard.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;

namespace BidGuard.Application.Tests.Infrastructure;

/// <summary>Adapters that let the single deployment container run without Redis or S3/MinIO.</summary>
public class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bidguard-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Uploaded_bytes_are_downloaded_unchanged()
    {
        var storage = new LocalFileStorageService(new LocalStorageSettings { RootPath = _root });

        await storage.UploadAsync("bids/abc/file.pdf", new MemoryStream([1, 2, 3, 4]), "application/pdf");
        await using var stream = await storage.DownloadAsync("bids/abc/file.pdf");
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);

        copy.ToArray().Should().Equal(1, 2, 3, 4);
    }

    [Theory]
    [InlineData("../outside.pdf")]
    [InlineData("bids/../../outside.pdf")]
    [InlineData("/etc/passwd")]
    public async Task Keys_cannot_escape_the_storage_root(string key)
    {
        var storage = new LocalFileStorageService(new LocalStorageSettings { RootPath = _root });

        var act = async () => await storage.UploadAsync(key, new MemoryStream([1]), "application/pdf");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Missing_file_raises_FileNotFound()
    {
        var storage = new LocalFileStorageService(new LocalStorageSettings { RootPath = _root });

        var act = async () => await storage.DownloadAsync("nope.pdf");

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}

public class MemoryCacheServiceTests
{
    private static MemoryCacheService NewCache() => new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public async Task GetOrSet_runs_the_factory_once_per_key()
    {
        var cache = NewCache();
        var calls = 0;

        for (var i = 0; i < 3; i++)
            await cache.GetOrSetAsync("k", () => Task.FromResult(++calls), TimeSpan.FromMinutes(1));

        calls.Should().Be(1);
    }

    [Fact]
    public async Task Remove_evicts_the_entry()
    {
        var cache = NewCache();
        await cache.SetAsync("k", "v", TimeSpan.FromMinutes(1));

        await cache.RemoveAsync("k");

        (await cache.GetAsync<string>("k")).Should().BeNull();
    }
}
