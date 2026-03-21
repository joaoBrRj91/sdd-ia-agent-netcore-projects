namespace PaymentService.Api.Domain.Events;

public abstract class DomainEvent
{
    public Guid AggregateId { get; protected set; }
    public DateTimeOffset OccurredAt { get; protected set; } = DateTimeOffset.UtcNow;
}

public sealed class PaymentCreatedEvent : DomainEvent
{
    public PaymentCreatedEvent(
        Guid ticket,
        string orderId,
        decimal amount,
        string currency,
        string paymentMethod,
        Guid aggregateId,
        string status,
        DateTimeOffset occurredAt)
    {
        Ticket = ticket;
        OrderId = orderId;
        Amount = amount;
        Currency = currency;
        PaymentMethod = paymentMethod;
        AggregateId = aggregateId;
        Status = status;
        OccurredAt = occurredAt;
    }

    public Guid Ticket { get; }
    public string OrderId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public string PaymentMethod { get; }
    public string Status { get; }
}

public sealed class PaymentApprovedEvent : DomainEvent
{
    public PaymentApprovedEvent(Guid ticket, string transactionId, DateTimeOffset occurredAt)
    {
        Ticket = ticket;
        AggregateId = ticket;
        TransactionId = transactionId;
        OccurredAt = occurredAt;
    }

    public Guid Ticket { get; }
    public string TransactionId { get; }
}

public sealed class PaymentRejectedEvent : DomainEvent
{
    public PaymentRejectedEvent(
        Guid ticket,
        string transactionId,
        string? errorCode,
        string? errorMessage,
        DateTimeOffset occurredAt)
    {
        Ticket = ticket;
        AggregateId = ticket;
        TransactionId = transactionId;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        OccurredAt = occurredAt;
    }

    public Guid Ticket { get; }
    public string TransactionId { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
}
