using PaymentService.Api.Models;

namespace PaymentService.Api.Application.Messaging;

public sealed class IntegrationPaymentMessage
{
    public required Guid Ticket { get; init; }
    public required string EventType { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public IntegrationPaymentData? Data { get; init; }
    public IntegrationPaymentError? Error { get; init; }

    public static IntegrationPaymentMessage FromCallback(CallbackNotification n)
    {
        IntegrationPaymentData? data = n.Data is null
            ? null
            : new IntegrationPaymentData
            {
                TransactionId = n.Data.TransactionId,
                Status = n.Data.Status,
                PaymentMethod = n.Data.PaymentMethod,
                AuthorizationCode = n.Data.AuthorizationCode,
                AmountReceived = n.Data.AmountReceived,
                FeeDeducted = n.Data.FeeDeducted,
                ProcessedAt = n.Data.ProcessedAt
            };

        IntegrationPaymentError? error = n.Error is null
            ? null
            : new IntegrationPaymentError
            {
                Code = n.Error.Code,
                Message = n.Error.Message
            };

        return new IntegrationPaymentMessage
        {
            Ticket = n.Ticket,
            EventType = n.EventType,
            Timestamp = n.Timestamp,
            Data = data,
            Error = error
        };
    }
}

public sealed class IntegrationPaymentData
{
    public string? TransactionId { get; init; }
    public string? Status { get; init; }
    public string? PaymentMethod { get; init; }
    public string? AuthorizationCode { get; init; }
    public decimal? AmountReceived { get; init; }
    public decimal? FeeDeducted { get; init; }
    public DateTimeOffset? ProcessedAt { get; init; }
}

public sealed class IntegrationPaymentError
{
    public string? Code { get; init; }
    public string? Message { get; init; }
}
