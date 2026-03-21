using PaymentService.Api.Domain.Entities;

namespace PaymentService.Api.Domain.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByTicketAsync(Guid ticket);
    Task SaveAsync(Payment payment);
}
