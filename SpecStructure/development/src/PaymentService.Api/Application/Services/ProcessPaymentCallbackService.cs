using PaymentService.Api.Application.Commands;
using PaymentService.Api.Application.Handlers;
using PaymentService.Api.Application.Cache;
using PaymentService.Api.Application.Messaging;
using PaymentService.Api.Models;

namespace PaymentService.Api.Application.Services;

public sealed class ProcessPaymentCallbackService : IProcessPaymentCallbackService
{
    private readonly ICommandHandler<ProcessPaymentCallbackCommand, ProcessPaymentCallbackResult> _commandHandler;
    private readonly ICacheProvider _cache;
    private readonly IIntegrationMessagePublisher _integrationPublisher;

    public ProcessPaymentCallbackService(
        ICommandHandler<ProcessPaymentCallbackCommand, ProcessPaymentCallbackResult> commandHandler,
        ICacheProvider cache,
        IIntegrationMessagePublisher integrationPublisher)
    {
        _commandHandler = commandHandler;
        _cache = cache;
        _integrationPublisher = integrationPublisher;
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
            notification.Data.ProcessedAt ?? DateTimeOffset.UtcNow,
            notification.Data.AuthorizationCode,
            notification.Data.FeeDeducted);

        var callbackError = notification.Error is not null
            ? new CallbackError(notification.Error.Code ?? "", notification.Error.Message ?? "")
            : null;

        var command = new ProcessPaymentCallbackCommand(
            notification.Ticket,
            notification.EventType,
            notification.Timestamp,
            callbackData,
            callbackError);

        // Persist first (publish only after successful local processing)
        var result = await _commandHandler.HandleAsync(command);

        if (!result.PaymentFound)
            return;

        await _cache.InvalidateAsync($"payment:status:{notification.Ticket}");

        var integrationMessage = IntegrationPaymentMessage.FromCallback(notification);
        await _integrationPublisher.PublishAsync(integrationMessage);
    }
}
