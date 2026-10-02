namespace BidGuard.Application.Common.Interfaces;

/// <summary>Distributed cache port (Redis). Implementations must treat cache outages as misses.</summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct = default);
}

public static class CacheKeys
{
    public static string TenderRules(Guid tenderId) => $"tender:{tenderId}:rules";
    public static string Verification(string source, string identifier) => $"verify:{source}:{identifier.ToUpperInvariant()}";
    public const string DashboardStats = "dashboard:stats";
}
