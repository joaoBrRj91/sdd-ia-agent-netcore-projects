using PaymentService.Api.Application.Commands;
using PaymentService.Api.Domain.Repositories;

namespace PaymentService.Api.Application.Handlers;

public sealed class ProcessPaymentCallbackCommandHandler : ICommandHandler<ProcessPaymentCallbackCommand, Unit>
{
    private readonly IPaymentRepository _repository;

    public ProcessPaymentCallbackCommandHandler(IPaymentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> HandleAsync(ProcessPaymentCallbackCommand command)
    {
        var payment = await _repository.GetByTicketAsync(command.Ticket);
        if (payment is null)
            return Unit.Value;

        if (command.Data.Status == "approved")
        {
            payment.ApprovePayment(command.Data.TransactionId);
        }
        else if (command.Data.Status == "rejected")
        {
            payment.RejectPayment(
                command.Data.TransactionId,
                command.Error?.Code,
                command.Error?.Message);
        }

        await _repository.SaveAsync(payment);
        return Unit.Value;
    }
}

public struct Unit
{
    public static Unit Value => default;
}
