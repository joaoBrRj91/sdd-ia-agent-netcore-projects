namespace PaymentService.Api.Application.Messaging;

/// <summary>
/// Secondary port: publishes processed callback outcomes to the integration broker (RabbitMQ in production; in-memory channel in tests).
/// </summary>
public interface IIntegrationMessagePublisher
{
    /// <summary>
    /// Enqueues the message without waiting for downstream consumers to finish processing.
    /// </summary>
    ValueTask PublishAsync(IntegrationPaymentMessage message, CancellationToken cancellationToken = default);
}
