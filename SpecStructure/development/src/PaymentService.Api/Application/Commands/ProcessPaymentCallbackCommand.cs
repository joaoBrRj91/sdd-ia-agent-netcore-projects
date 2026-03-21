namespace PaymentService.Api.Application.Commands;

public sealed record ProcessPaymentCallbackCommand(
    Guid Ticket,
    string EventType,
    DateTimeOffset Timestamp,
    CallbackData Data,
    CallbackError? Error);

public sealed record CallbackData(
    string TransactionId,
    string Status,
    string PaymentMethod,
    decimal AmountReceived,
    DateTimeOffset ProcessedAt);

public sealed record CallbackError(
    string Code,
    string Message);
