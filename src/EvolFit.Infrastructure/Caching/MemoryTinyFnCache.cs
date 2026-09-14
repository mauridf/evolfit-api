using System.Collections.Concurrent;
using EvolFit.Application.Features.TinyFn.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace EvolFit.Infrastructure.Caching;

public class MemoryTinyFnCache : ITinyFnCache
{
    private readonly IMemoryCache _cache;

    // Rastreia chaves para permitir cancelamento/limpeza no futuro
    private static readonly ConcurrentDictionary<string, byte> Keys = new();

    public MemoryTinyFnCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
            return cached;

        var value = await factory();
        if (value is null) return default;

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
            SlidingExpiration = null
        };

        _cache.Set(key, value, options);
        Keys.TryAdd(key, 0);

        return value;
    }
}
