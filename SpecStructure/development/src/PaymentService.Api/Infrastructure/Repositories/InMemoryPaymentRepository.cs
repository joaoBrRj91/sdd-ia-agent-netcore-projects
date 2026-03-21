using System.Collections.Concurrent;
using PaymentService.Api.Domain.Entities;
using PaymentService.Api.Domain.Repositories;

namespace PaymentService.Api.Infrastructure.Repositories;

public sealed class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly ConcurrentDictionary<Guid, Payment> _payments = new();

    public Task<Payment?> GetByTicketAsync(Guid ticket)
    {
        _payments.TryGetValue(ticket, out var payment);
        return Task.FromResult<Payment?>(payment);
    }

    public Task SaveAsync(Payment payment)
    {
        _payments.AddOrUpdate(payment.Ticket, payment, (_, _) => payment);
        return Task.CompletedTask;
    }
}
