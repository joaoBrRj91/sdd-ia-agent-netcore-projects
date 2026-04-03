namespace PaymentService.Api.Application.Cache;

public interface ICacheProvider
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan ttl) where T : class;
    Task InvalidateAsync(string key);
}
