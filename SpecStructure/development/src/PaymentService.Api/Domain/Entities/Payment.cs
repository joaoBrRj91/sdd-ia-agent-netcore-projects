using PaymentService.Api.Domain.Events;
using PaymentService.Api.Domain.ValueObjects;

namespace PaymentService.Api.Domain.Entities;

public sealed class Payment
{
    private readonly List<DomainEvent> _events = [];

    private Payment(
        Guid ticket,
        string orderId,
        Money amount,
        PaymentMethod paymentMethod,
        Customer customer)
    {
        Ticket = ticket;
        OrderId = orderId;
        Amount = amount;
        PaymentMethod = paymentMethod;
        Customer = customer;
        Status = PaymentStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Ticket { get; private set; }
    public string OrderId { get; private set; }
    public Money Amount { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public Customer Customer { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public IReadOnlyList<DomainEvent> Events => _events.AsReadOnly();

    public static Payment Create(
        string orderId,
        decimal amount,
        string currency,
        string paymentMethodType,
        string? pixKey,
        string? cardToken,
        string customerName,
        string customerDocument)
    {
        var currencyObj = Currency.Create(currency);
        var money = Money.Create(amount, currencyObj);
        var method = PaymentMethod.Create(paymentMethodType, pixKey, cardToken);
        var customer = Customer.Create(customerName, customerDocument);

        if (string.IsNullOrWhiteSpace(orderId))
            throw new ArgumentException("Order ID cannot be empty");

        var ticket = Guid.NewGuid();
        var payment = new Payment(ticket, orderId, money, method, customer);
        
        payment.AddEvent(new PaymentCreatedEvent(
            ticket, orderId, amount, currency, paymentMethodType, ticket, "Pending", DateTimeOffset.UtcNow));

        return payment;
    }

    public void ApprovePayment(string transactionId)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Cannot approve a payment that is not pending");

        Status = PaymentStatus.Approved;
        UpdatedAt = DateTimeOffset.UtcNow;
        
        AddEvent(new PaymentApprovedEvent(Ticket, transactionId, UpdatedAt.Value));
    }

    public void RejectPayment(string transactionId, string? errorCode = null, string? errorMessage = null)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Cannot reject a payment that is not pending");

        Status = PaymentStatus.Rejected;
        UpdatedAt = DateTimeOffset.UtcNow;
        
        AddEvent(new PaymentRejectedEvent(Ticket, transactionId, errorCode, errorMessage, UpdatedAt.Value));
    }

    private void AddEvent(DomainEvent @event)
    {
        _events.Add(@event);
    }

    public void ClearEvents()
    {
        _events.Clear();
    }
}
