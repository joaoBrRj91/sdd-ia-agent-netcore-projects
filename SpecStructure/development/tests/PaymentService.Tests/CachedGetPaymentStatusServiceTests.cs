using Microsoft.Extensions.Options;
using PaymentService.Api.Application.Cache;
using PaymentService.Api.Application.Services;
using PaymentService.Api.Models;

namespace PaymentService.Tests;

public class CachedGetPaymentStatusServiceTests
{
    [Fact]
    public async Task Should_DelegateToInnerServiceAndStoreResult_When_CacheMiss()
    {
        // Arrange
        var ticket = Guid.NewGuid();
        var response = new PaymentService.Api.Models.GetPaymentStatusResponse
        {
            Ticket = ticket,
            EventType = "payment.in_progress",
            Timestamp = DateTimeOffset.UtcNow
        };

        var innerService = new TestableGetPaymentStatusService { ResponseToReturn = response };
        var cacheProvider = new TestableCacheProvider();
        var options = Options.Create(new CacheSettings { PaymentStatusTtlSeconds = 60 });

        var decorator = new CachedGetPaymentStatusService(innerService, cacheProvider, options);

        // Act
        var result = await decorator.GetPaymentStatusAsync(ticket);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ticket, result.Ticket);
        Assert.Equal(1, innerService.CallCount);
        Assert.Single(cacheProvider.SetCalls);
        Assert.Equal($"payment:status:{ticket}", cacheProvider.SetCalls[0].Key);
        Assert.Equal(TimeSpan.FromSeconds(60), cacheProvider.SetCalls[0].Ttl);
    }

    [Fact]
    public async Task Should_ReturnCachedValue_When_CacheHit()
    {
        // Arrange
        var ticket = Guid.NewGuid();
        var cachedResponse = new PaymentService.Api.Models.GetPaymentStatusResponse
        {
            Ticket = ticket,
            EventType = "payment.in_progress",
            Timestamp = DateTimeOffset.UtcNow
        };

        var innerService = new TestableGetPaymentStatusService();
        var cacheProvider = new TestableCacheProvider();
        cacheProvider.PrePopulate($"payment:status:{ticket}", cachedResponse);

        var options = Options.Create(new CacheSettings { PaymentStatusTtlSeconds = 60 });
        var decorator = new CachedGetPaymentStatusService(innerService, cacheProvider, options);

        // Act
        var result = await decorator.GetPaymentStatusAsync(ticket);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cachedResponse.Ticket, result.Ticket);
        Assert.Equal(0, innerService.CallCount);
    }

    [Fact]
    public async Task Should_NotCacheNull_When_InnerServiceReturnsNull()
    {
        // Arrange
        var ticket = Guid.NewGuid();
        var innerService = new TestableGetPaymentStatusService();
        var cacheProvider = new TestableCacheProvider();
        var options = Options.Create(new CacheSettings { PaymentStatusTtlSeconds = 60 });

        var decorator = new CachedGetPaymentStatusService(innerService, cacheProvider, options);

        // Act
        var result = await decorator.GetPaymentStatusAsync(ticket);

        // Assert
        Assert.Null(result);
        Assert.Empty(cacheProvider.SetCalls);
    }

    [Fact]
    public async Task Should_CallInnerServiceOnlyOnce_When_MultipleCallsWithCacheHit()
    {
        // Arrange
        var ticket = Guid.NewGuid();
        var response = new PaymentService.Api.Models.GetPaymentStatusResponse
        {
            Ticket = ticket,
            EventType = "payment.in_progress",
            Timestamp = DateTimeOffset.UtcNow
        };

        var innerService = new TestableGetPaymentStatusService { ResponseToReturn = response };
        var cacheProvider = new TestableCacheProvider();
        var options = Options.Create(new CacheSettings { PaymentStatusTtlSeconds = 60 });

        var decorator = new CachedGetPaymentStatusService(innerService, cacheProvider, options);

        // Act
        var result1 = await decorator.GetPaymentStatusAsync(ticket);
        var result2 = await decorator.GetPaymentStatusAsync(ticket);

        // Assert
        Assert.Equal(1, innerService.CallCount);
        Assert.Single(cacheProvider.SetCalls);
    }

    [Fact]
    public async Task Should_UseTtlFromOptions()
    {
        // Arrange
        var ticket = Guid.NewGuid();
        var response = new PaymentService.Api.Models.GetPaymentStatusResponse
        {
            Ticket = ticket,
            EventType = "payment.in_progress",
            Timestamp = DateTimeOffset.UtcNow
        };

        var innerService = new TestableGetPaymentStatusService { ResponseToReturn = response };
        var cacheProvider = new TestableCacheProvider();
        var options = Options.Create(new CacheSettings { PaymentStatusTtlSeconds = 30 });

        var decorator = new CachedGetPaymentStatusService(innerService, cacheProvider, options);

        // Act
        await decorator.GetPaymentStatusAsync(ticket);

        // Assert
        Assert.Single(cacheProvider.SetCalls);
        Assert.Equal(TimeSpan.FromSeconds(30), cacheProvider.SetCalls[0].Ttl);
    }

    [Fact]
    public async Task Should_UseDifferentCacheKeys_When_DifferentTickets()
    {
        // Arrange
        var ticket1 = Guid.NewGuid();
        var ticket2 = Guid.NewGuid();
        var response1 = new PaymentService.Api.Models.GetPaymentStatusResponse
        {
            Ticket = ticket1,
            EventType = "payment.in_progress",
            Timestamp = DateTimeOffset.UtcNow
        };
        var response2 = new PaymentService.Api.Models.GetPaymentStatusResponse
        {
            Ticket = ticket2,
            EventType = "payment.in_progress",
            Timestamp = DateTimeOffset.UtcNow
        };

        var innerService = new TestableGetPaymentStatusService();
        var cacheProvider = new TestableCacheProvider();
        var options = Options.Create(new CacheSettings { PaymentStatusTtlSeconds = 60 });

        var decorator = new CachedGetPaymentStatusService(innerService, cacheProvider, options);

        innerService.ResponseToReturn = response1;
        await decorator.GetPaymentStatusAsync(ticket1);

        innerService.ResponseToReturn = response2;
        await decorator.GetPaymentStatusAsync(ticket2);

        // Assert
        Assert.Equal(2, cacheProvider.SetCalls.Count);
        Assert.Equal($"payment:status:{ticket1}", cacheProvider.SetCalls[0].Key);
        Assert.Equal($"payment:status:{ticket2}", cacheProvider.SetCalls[1].Key);
    }

    // Test Doubles

    private class TestableGetPaymentStatusService : IGetPaymentStatusService
    {
        public int CallCount { get; private set; }
        public PaymentService.Api.Models.GetPaymentStatusResponse? ResponseToReturn { get; set; }

        public Task<PaymentService.Api.Models.GetPaymentStatusResponse?> GetPaymentStatusAsync(Guid ticket)
        {
            CallCount++;
            return Task.FromResult(ResponseToReturn);
        }
    }

    private class TestableCacheProvider : ICacheProvider
    {
        private readonly Dictionary<string, object> _store = new();
        public List<(string Key, TimeSpan Ttl)> SetCalls { get; } = new();
        public List<string> InvalidateCalls { get; } = new();

        public void PrePopulate<T>(string key, T value) where T : class
        {
            _store[key] = value;
        }

        public Task<T?> GetAsync<T>(string key) where T : class
        {
            _store.TryGetValue(key, out var val);
            return Task.FromResult(val as T);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl) where T : class
        {
            _store[key] = value;
            SetCalls.Add((key, ttl));
            return Task.CompletedTask;
        }

        public Task InvalidateAsync(string key)
        {
            _store.Remove(key);
            InvalidateCalls.Add(key);
            return Task.CompletedTask;
        }
    }
}
