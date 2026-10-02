using System.Text.Json;
using BidGuard.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BidGuard.Infrastructure.Caching;

public sealed class RedisSettings
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; init; } = "localhost:6379";
    public string InstanceName { get; init; } = "bidguard:";
}

/// <summary>Cache-aside over Redis. Redis being down never fails a request: errors are logged and treated as misses.</summary>
public sealed class RedisCacheService(IConnectionMultiplexer redis, RedisSettings settings, ILogger<RedisCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private IDatabase Db => redis.GetDatabase();
    private string Key(string key) => settings.InstanceName + key;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var value = await Db.StringGetAsync(Key(key));
            return value.HasValue ? JsonSerializer.Deserialize<T>(value.ToString(), Json) : default;
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or JsonException)
        {
            logger.LogWarning(ex, "Cache read failed for {Key}; treating as miss", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        try
        {
            await Db.StringSetAsync(Key(key), JsonSerializer.Serialize(value, Json), ttl);
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException)
        {
            logger.LogWarning(ex, "Cache write failed for {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await Db.KeyDeleteAsync(Key(key));
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException)
        {
            logger.LogWarning(ex, "Cache delete failed for {Key}", key);
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null)
            return cached;

        var value = await factory();
        if (value is not null)
            await SetAsync(key, value, ttl, ct);
        return value;
    }
}
