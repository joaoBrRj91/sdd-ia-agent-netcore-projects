using PaymentService.Api.Models;

namespace PaymentService.Api.Application.Services;

public interface IProcessPaymentCallbackService
{
    Task ProcessCallbackAsync(CallbackNotification notification);
}
