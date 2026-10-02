using System.Linq.Expressions;
using BidGuard.Application.Common.Interfaces;

namespace BidGuard.Application.Tests.TestSupport;

public sealed class InMemoryFileStorage : IFileStorageService
{
    public Dictionary<string, byte[]> Objects { get; } = [];

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        Objects[key] = ms.ToArray();
    }

    public Task<Stream> DownloadAsync(string key, CancellationToken ct = default) =>
        Objects.TryGetValue(key, out var bytes)
            ? Task.FromResult<Stream>(new MemoryStream(bytes))
            : throw new FileNotFoundException(key);
}

public sealed class RecordingJobScheduler : IBackgroundJobScheduler
{
    public List<LambdaExpression> Enqueued { get; } = [];

    public string Enqueue<TJob>(Expression<Func<TJob, Task>> job)
    {
        Enqueued.Add(job);
        return Enqueued.Count.ToString();
    }

    public string ContinueWith<TJob>(string parentJobId, Expression<Func<TJob, Task>> job) => Enqueue(job);

    public void AddOrUpdateRecurring<TJob>(string recurringJobId, Expression<Func<TJob, Task>> job, string cronExpression) =>
        Enqueued.Add(job);

    public bool HasJob<TJob>() => Enqueued.Any(e => e.Parameters[0].Type == typeof(TJob));
}

public sealed class FakeDocumentIntelligence : IDocumentIntelligenceService
{
    public List<ExtractedRuleDto> Rules { get; set; } = [];
    public Dictionary<string, EntityExtractionResult> EntitiesByFile { get; } = [];
    public Exception? Throw { get; set; }

    public Task<IReadOnlyList<ExtractedRuleDto>> ExtractRulesAsync(Stream document, string fileName, CancellationToken ct = default) =>
        Throw is not null ? Task.FromException<IReadOnlyList<ExtractedRuleDto>>(Throw) : Task.FromResult<IReadOnlyList<ExtractedRuleDto>>(Rules);

    public Task<EntityExtractionResult> ExtractEntitiesAsync(Stream document, string fileName, string? docType, CancellationToken ct = default)
    {
        if (Throw is not null) return Task.FromException<EntityExtractionResult>(Throw);
        return Task.FromResult(EntitiesByFile.GetValueOrDefault(fileName, new EntityExtractionResult(false, [])));
    }
}

/// <summary>Cache that never hits, so handlers always exercise their factory path.</summary>
public sealed class PassThroughCache : ICacheService
{
    public List<string> Removed { get; } = [];

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) => Task.FromResult<T?>(default);
    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        Removed.Add(key);
        return Task.CompletedTask;
    }

    public Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct = default) => factory();
}
