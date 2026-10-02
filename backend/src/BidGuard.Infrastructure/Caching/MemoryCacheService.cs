using BidGuard.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BidGuard.Infrastructure.Caching;

/// <summary>In-process cache used when no Redis connection is configured (single-instance deployments).</summary>
public sealed class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) =>
        Task.FromResult(cache.TryGetValue(key, out T? value) ? value : default);

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        cache.Set(key, value, ttl);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct = default)
    {
        if (cache.TryGetValue(key, out T? cached) && cached is not null)
            return cached;
        var value = await factory();
        if (value is not null)
            cache.Set(key, value, ttl);
        return value;
    }
}
