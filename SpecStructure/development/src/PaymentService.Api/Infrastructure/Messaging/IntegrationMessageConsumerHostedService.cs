using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentService.Api.Application.Messaging;

namespace PaymentService.Api.Infrastructure.Messaging;

/// <summary>
/// Primary adapter: reads integration queue and performs incremental handling (informational log only in this phase).
/// </summary>
public sealed class IntegrationMessageConsumerHostedService : BackgroundService
{
    private readonly InMemoryIntegrationMessageQueue _queue;
    private readonly ILogger<IntegrationMessageConsumerHostedService> _logger;

    public IntegrationMessageConsumerHostedService(
        InMemoryIntegrationMessageQueue queue,
        ILogger<IntegrationMessageConsumerHostedService> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            _queue.RecordConsumed(message);
            _logger.LogInformation(
                "Integration message consumed for ticket {Ticket} event_type {EventType} status {Status}",
                message.Ticket,
                message.EventType,
                message.Data?.Status);
        }
    }
}
