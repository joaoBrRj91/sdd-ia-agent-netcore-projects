using PaymentService.Api.Domain.Repositories;
using PaymentService.Api.Domain.ValueObjects;
using PaymentService.Api.Models;

namespace PaymentService.Api.Application.Services;

public sealed class GetPaymentStatusService : IGetPaymentStatusService
{
    private readonly IPaymentRepository _repository;

    public GetPaymentStatusService(IPaymentRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetPaymentStatusResponse?> GetPaymentStatusAsync(Guid ticket)
    {
        var payment = await _repository.GetByTicketAsync(ticket);
        if (payment is null)
            return null;

        return BuildResponse(payment);
    }

    private static GetPaymentStatusResponse BuildResponse(Domain.Entities.Payment payment)
    {
        var eventType = payment.Status switch
        {
            PaymentStatus.Pending => "payment.in_progress",
            PaymentStatus.Approved => "payment.sucessful",
            PaymentStatus.Rejected => "payment.failed",
            _ => "payment.unknown"
        };

        var data = payment.Status switch
        {
            PaymentStatus.Approved => new GetPaymentStatusData
            {
                TransactionId = payment.TransactionId,
                Status = "approved",
                PaymentMethod = payment.PaymentMethod.Type,
                AuthorizationCode = payment.AuthorizationCode,
                AmountReceived = payment.AmountReceived ?? 0m,
                FeeDeducted = payment.FeeDeducted,
                ProcessedAt = payment.ProcessedAt ?? DateTimeOffset.UtcNow
            },
            PaymentStatus.Rejected => new GetPaymentStatusData
            {
                TransactionId = payment.TransactionId,
                Status = "rejected",
                PaymentMethod = payment.PaymentMethod.Type,
                AmountReceived = payment.AmountReceived ?? 0m,
                ProcessedAt = payment.ProcessedAt ?? DateTimeOffset.UtcNow
            },
            _ => null
        };

        var error = payment.Status == PaymentStatus.Rejected
            ? new GetPaymentStatusError
            {
                Code = payment.ErrorCode,
                Message = payment.ErrorMessage
            }
            : null;

        return new GetPaymentStatusResponse
        {
            Ticket = payment.Ticket,
            EventType = eventType,
            Timestamp = payment.UpdatedAt ?? payment.CreatedAt,
            Data = data,
            Error = error
        };
    }
}
