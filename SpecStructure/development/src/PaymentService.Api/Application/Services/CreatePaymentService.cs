using PaymentService.Api.Application.Commands;
using PaymentService.Api.Application.DTOs;
using PaymentService.Api.Application.Handlers;
using PaymentService.Api.Models;

namespace PaymentService.Api.Application.Services;

public sealed class CreatePaymentService : ICreatePaymentService
{
    private readonly ICommandHandler<CreatePaymentCommand, CreatePaymentResponseDto> _commandHandler;

    public CreatePaymentService(ICommandHandler<CreatePaymentCommand, CreatePaymentResponseDto> commandHandler)
    {
        _commandHandler = commandHandler;
    }

    public async Task<CreatePaymentResponseDto> CreatePaymentAsync(PaymentIntentRequest request)
    {
        // Validate request structure
        if (request.MethodDetails is null || request.Customer is null)
            throw new ArgumentException("Method details and customer information are required");

        if (string.IsNullOrWhiteSpace(request.OrderId))
            throw new ArgumentException("Order ID is required");

        // Validate currency
        var currency = request.Currency.Trim().ToUpperInvariant();
        if (currency is not ("BRL" or "USD"))
            throw new ArgumentException($"Invalid currency: {currency}. Supported currencies are BRL and USD");

        // Validate amount
        if (request.Amount <= 0m)
            throw new ArgumentException("Amount must be greater than zero");

        // Validate payment method and extract method-specific details
        var method = request.PaymentMethod.Trim().ToLowerInvariant();
        string? pixKey = null;
        string? cardToken = null;

        if (method == "credit_card")
        {
            if (string.IsNullOrWhiteSpace(request.MethodDetails.CardToken))
                throw new ArgumentException("Card token is required for credit card payments");
            cardToken = request.MethodDetails.CardToken;
        }
        else if (method == "pix")
        {
            if (string.IsNullOrWhiteSpace(request.MethodDetails.PixKey))
                throw new ArgumentException("Pix key is required for Pix payments");
            pixKey = request.MethodDetails.PixKey;
        }
        else
        {
            throw new ArgumentException($"Invalid payment method: {method}. Supported methods are 'pix' and 'credit_card'");
        }

        // Create and execute command
        var command = new CreatePaymentCommand(
            request.OrderId,
            request.Amount,
            currency,
            method,
            pixKey,
            cardToken,
            request.Customer.Name,
            request.Customer.Document);

        var response = await _commandHandler.HandleAsync(command);
        return response;
    }
}
