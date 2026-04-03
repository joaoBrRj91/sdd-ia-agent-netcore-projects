using PaymentService.Api.Application.Commands;
using PaymentService.Api.Application.Handlers;
using PaymentService.Api.Application.Cache;
using PaymentService.Api.Models;

namespace PaymentService.Api.Application.Services;

public sealed class ProcessPaymentCallbackService : IProcessPaymentCallbackService
{
    private readonly ICommandHandler<ProcessPaymentCallbackCommand, Unit> _commandHandler;
    private readonly ICacheProvider _cache;

    public ProcessPaymentCallbackService(
        ICommandHandler<ProcessPaymentCallbackCommand, Unit> commandHandler,
        ICacheProvider cache)
    {
        _commandHandler = commandHandler;
        _cache = cache;
    }

    public async Task ProcessCallbackAsync(CallbackNotification notification)
    {
        // Validate notification structure
        if (notification.Data is null)
            throw new ArgumentException("Callback data is required");

        // Map payload to command
        var callbackData = new CallbackData(
            notification.Data.TransactionId ?? "",
            notification.Data.Status ?? "",
            notification.Data.PaymentMethod ?? "",
            notification.Data.AmountReceived ?? 0m,
            notification.Data.ProcessedAt ?? DateTimeOffset.UtcNow);

        var callbackError = notification.Error is not null
            ? new CallbackError(notification.Error.Code ?? "", notification.Error.Message ?? "")
            : null;

        var command = new ProcessPaymentCallbackCommand(
            notification.Ticket,
            notification.EventType,
            notification.Timestamp,
            callbackData,
            callbackError);

        // Execute command
        await _commandHandler.HandleAsync(command);

        // Invalidate cached payment status on successful callback
        await _cache.InvalidateAsync($"payment:status:{notification.Ticket}");
    }
}
