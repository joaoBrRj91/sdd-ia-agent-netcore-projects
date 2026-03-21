namespace PaymentService.Api.Application.Commands;

public sealed record CreatePaymentCommand(
    string OrderId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string? PixKey,
    string? CardToken,
    string CustomerName,
    string CustomerDocument);
