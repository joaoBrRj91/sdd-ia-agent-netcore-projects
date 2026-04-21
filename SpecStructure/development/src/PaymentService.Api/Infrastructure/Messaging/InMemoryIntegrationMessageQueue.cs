using System.Collections.Concurrent;
using System.Threading.Channels;
using PaymentService.Api.Application.Messaging;

namespace PaymentService.Api.Infrastructure.Messaging;

/// <summary>
/// In-memory substitute for RabbitMQ: unbounded channel for integration tests and local dev.
/// </summary>
public sealed class InMemoryIntegrationMessageQueue : IIntegrationMessagePublisher
{
    private readonly Channel<IntegrationPaymentMessage> _channel = Channel.CreateUnbounded<IntegrationPaymentMessage>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ChannelReader<IntegrationPaymentMessage> Reader => _channel.Reader;

    /// <summary>Messages observed by the hosted consumer (used by integration tests).</summary>
    public ConcurrentBag<IntegrationPaymentMessage> ConsumedMessages { get; } = new();

    public ValueTask PublishAsync(IntegrationPaymentMessage message, CancellationToken cancellationToken = default)
    {
        _channel.Writer.TryWrite(message);
        return ValueTask.CompletedTask;
    }

    internal void RecordConsumed(IntegrationPaymentMessage message) => ConsumedMessages.Add(message);
}
