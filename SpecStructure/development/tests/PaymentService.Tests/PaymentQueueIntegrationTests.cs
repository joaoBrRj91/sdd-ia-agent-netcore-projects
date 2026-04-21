using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentService.Api.Infrastructure.Messaging;
using PaymentService.Api.Models;

namespace PaymentService.Tests;

/// <summary>
/// Spec: Queue integration coverage — callback-to-queue flow, 204 without waiting for consumer, in-memory queue contract.
/// </summary>
public class PaymentQueueIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 10_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;
            await Task.Delay(25);
        }

        Assert.Fail("Timed out waiting for integration queue condition.");
    }

    [Fact]
    public async Task Approved_callback_for_existing_payment_publishes_message_consumed_by_integration_queue()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var queue = factory.Services.GetRequiredService<InMemoryIntegrationMessageQueue>();

        var createBody = new
        {
            order_id = "ORD-QUEUE-APPROVED-1",
            amount = 100m,
            currency = "BRL",
            payment_method = "credit_card",
            method_details = new { card_token = "tok_123" },
            customer = new { name = "User", document = "123" }
        };

        var createResponse = await client.PostAsJsonAsync("/payments", createBody, JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);
        Assert.NotNull(created);
        var ticket = created.Ticket;

        var callbackBody = new
        {
            ticket,
            event_type = "payment.sucessful",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-999888777",
                status = "approved",
                payment_method = "credit_card",
                authorization_code = "AUTH-789",
                amount_received = 100m,
                fee_deducted = 2.5m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = (object?)null
        };

        var callbackResponse = await client.PostAsJsonAsync("/payments/callback", callbackBody, JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, callbackResponse.StatusCode);

        await WaitUntilAsync(() => queue.ConsumedMessages.Any(m => m.Ticket == ticket));

        var published = queue.ConsumedMessages.First(m => m.Ticket == ticket);
        Assert.Equal("payment.sucessful", published.EventType);
        Assert.NotNull(published.Data);
        Assert.Equal("approved", published.Data.Status);
        Assert.Equal("TX-999888777", published.Data.TransactionId);
        Assert.Equal("AUTH-789", published.Data.AuthorizationCode);
    }

    [Fact]
    public async Task Rejected_callback_for_existing_payment_publishes_message_to_integration_queue()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var queue = factory.Services.GetRequiredService<InMemoryIntegrationMessageQueue>();

        var createBody = new
        {
            order_id = "ORD-QUEUE-REJECT-1",
            amount = 50m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "pix-key" },
            customer = new { name = "User", document = "123" }
        };

        var createResponse = await client.PostAsJsonAsync("/payments", createBody, JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);
        Assert.NotNull(created);
        var ticket = created.Ticket;

        var callbackBody = new
        {
            ticket,
            event_type = "payment.failed",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-failed-123",
                status = "rejected",
                payment_method = "pix",
                amount_received = 0m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = new { code = "insufficient_funds", message = "Declined" }
        };

        var callbackResponse = await client.PostAsJsonAsync("/payments/callback", callbackBody, JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, callbackResponse.StatusCode);

        await WaitUntilAsync(() => queue.ConsumedMessages.Any(m => m.Ticket == ticket));

        var published = queue.ConsumedMessages.First(m => m.Ticket == ticket);
        Assert.Equal("payment.failed", published.EventType);
        Assert.NotNull(published.Data);
        Assert.Equal("rejected", published.Data.Status);
        Assert.NotNull(published.Error);
        Assert.Equal("insufficient_funds", published.Error.Code);
    }

    [Fact]
    public async Task Callback_returns_204_before_consumer_finishes_when_consumer_is_slow()
    {
        await using var factory = new SlowConsumerApiFactory();
        var client = factory.CreateClient();
        var queue = factory.Services.GetRequiredService<InMemoryIntegrationMessageQueue>();

        var createBody = new
        {
            order_id = "ORD-QUEUE-SLOW-1",
            amount = 10m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "k" },
            customer = new { name = "A", document = "1" }
        };

        var createResponse = await client.PostAsJsonAsync("/payments", createBody, JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);
        Assert.NotNull(created);
        var ticket = created.Ticket;

        var callbackBody = new
        {
            ticket,
            event_type = "payment.sucessful",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-SLOW",
                status = "approved",
                payment_method = "pix",
                amount_received = 10m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = (object?)null
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var callbackResponse = await client.PostAsJsonAsync("/payments/callback", callbackBody, JsonOptions);
        sw.Stop();

        Assert.Equal(HttpStatusCode.NoContent, callbackResponse.StatusCode);
        Assert.True(sw.ElapsedMilliseconds < 3000, "HTTP response should not wait for slow consumer processing.");

        await WaitUntilAsync(() => queue.ConsumedMessages.Any(m => m.Ticket == ticket), timeoutMs: 20_000);
    }

    [Fact]
    public async Task Unknown_ticket_callback_does_not_publish_integration_message()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var queue = factory.Services.GetRequiredService<InMemoryIntegrationMessageQueue>();

        var unknownTicket = Guid.Parse("550e8400-e29b-41d4-a716-446655440099");
        var callbackBody = new
        {
            ticket = unknownTicket,
            event_type = "payment.sucessful",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-UNKNOWN",
                status = "approved",
                payment_method = "pix",
                amount_received = 1m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = (object?)null
        };

        var callbackResponse = await client.PostAsJsonAsync("/payments/callback", callbackBody, JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, callbackResponse.StatusCode);

        await Task.Delay(500);
        Assert.Empty(queue.ConsumedMessages.Where(m => m.Ticket == unknownTicket));
    }
}

/// <summary>
/// Replaces the default consumer with one that delays before recording consumption, to prove HTTP does not wait.
/// </summary>
internal sealed class SlowConsumerApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            foreach (var d in services.Where(d =>
                         d.ServiceType == typeof(IHostedService) &&
                         d.ImplementationType == typeof(IntegrationMessageConsumerHostedService))
                     .ToList())
            {
                services.Remove(d);
            }

            services.AddHostedService<SlowIntegrationMessageConsumerHostedService>();
        });
    }
}

internal sealed class SlowIntegrationMessageConsumerHostedService : BackgroundService
{
    private readonly InMemoryIntegrationMessageQueue _queue;
    private readonly ILogger<SlowIntegrationMessageConsumerHostedService> _logger;

    public SlowIntegrationMessageConsumerHostedService(
        InMemoryIntegrationMessageQueue queue,
        ILogger<SlowIntegrationMessageConsumerHostedService> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await Task.Delay(5000, stoppingToken);
            _queue.RecordConsumed(message);
            _logger.LogInformation("Slow consumer processed ticket {Ticket}", message.Ticket);
        }
    }
}
