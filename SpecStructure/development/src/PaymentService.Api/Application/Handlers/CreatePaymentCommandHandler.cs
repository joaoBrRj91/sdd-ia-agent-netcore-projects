using PaymentService.Api.Application.Commands;
using PaymentService.Api.Application.DTOs;
using PaymentService.Api.Domain.Entities;
using PaymentService.Api.Domain.Repositories;

namespace PaymentService.Api.Application.Handlers;

public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command);
}

public sealed class CreatePaymentCommandHandler : ICommandHandler<CreatePaymentCommand, CreatePaymentResponseDto>
{
    private readonly IPaymentRepository _repository;

    public CreatePaymentCommandHandler(IPaymentRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreatePaymentResponseDto> HandleAsync(CreatePaymentCommand command)
    {
        var payment = Payment.Create(
            command.OrderId,
            command.Amount,
            command.Currency,
            command.PaymentMethod,
            command.PixKey,
            command.CardToken,
            command.CustomerName,
            command.CustomerDocument);

        await _repository.SaveAsync(payment);

        return new CreatePaymentResponseDto
        {
            Ticket = payment.Ticket,
            Status = payment.Status.ToString(),
            CreatedAt = payment.CreatedAt
        };
    }
}
