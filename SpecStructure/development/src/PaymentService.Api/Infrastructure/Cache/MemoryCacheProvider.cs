using Microsoft.Extensions.Caching.Memory;
using PaymentService.Api.Application.Cache;

namespace PaymentService.Api.Infrastructure.Cache;

public sealed class MemoryCacheProvider : ICacheProvider
{
    private readonly IMemoryCache _cache;

    public MemoryCacheProvider(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl) where T : class
    {
        var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
        _cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}
