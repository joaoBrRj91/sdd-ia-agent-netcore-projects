using PaymentService.Api.Application.DTOs;
using PaymentService.Api.Models;

namespace PaymentService.Api.Application.Services;

public interface ICreatePaymentService
{
    Task<CreatePaymentResponseDto> CreatePaymentAsync(PaymentIntentRequest request);
}
