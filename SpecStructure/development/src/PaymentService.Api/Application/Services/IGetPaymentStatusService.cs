using PaymentService.Api.Models;

namespace PaymentService.Api.Application.Services;

public interface IGetPaymentStatusService
{
    Task<GetPaymentStatusResponse?> GetPaymentStatusAsync(Guid ticket);
}
