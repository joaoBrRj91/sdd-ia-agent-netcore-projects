using Microsoft.Extensions.Options;
using PaymentService.Api.Application.Cache;
using PaymentService.Api.Models;

namespace PaymentService.Api.Application.Services;

public sealed class CacheSettings
{
    public int PaymentStatusTtlSeconds { get; init; } = 60;
}

public sealed class CachedGetPaymentStatusService : IGetPaymentStatusService
{
    private readonly IGetPaymentStatusService _inner;
    private readonly ICacheProvider _cache;
    private readonly TimeSpan _ttl;

    public CachedGetPaymentStatusService(
        IGetPaymentStatusService inner,
        ICacheProvider cache,
        IOptions<CacheSettings> options)
    {
        _inner = inner;
        _cache = cache;
        _ttl = TimeSpan.FromSeconds(options.Value.PaymentStatusTtlSeconds);
    }

    public async Task<GetPaymentStatusResponse?> GetPaymentStatusAsync(Guid ticket)
    {
        var key = $"payment:status:{ticket}";

        var cached = await _cache.GetAsync<GetPaymentStatusResponse>(key);
        if (cached is not null)
            return cached;

        var response = await _inner.GetPaymentStatusAsync(ticket);
        if (response is not null)
            await _cache.SetAsync(key, response, _ttl);

        return response;
    }
}
