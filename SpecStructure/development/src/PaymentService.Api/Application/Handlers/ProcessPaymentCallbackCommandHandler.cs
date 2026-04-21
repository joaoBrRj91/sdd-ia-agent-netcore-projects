using PaymentService.Api.Application.Commands;
using PaymentService.Api.Domain.Repositories;

namespace PaymentService.Api.Application.Handlers;

public sealed class ProcessPaymentCallbackCommandHandler : ICommandHandler<ProcessPaymentCallbackCommand, ProcessPaymentCallbackResult>
{
    private readonly IPaymentRepository _repository;

    public ProcessPaymentCallbackCommandHandler(IPaymentRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProcessPaymentCallbackResult> HandleAsync(ProcessPaymentCallbackCommand command)
    {
        var payment = await _repository.GetByTicketAsync(command.Ticket);
        if (payment is null)
            return new ProcessPaymentCallbackResult(false);

        if (command.Data.Status == "approved")
        {
            payment.ApprovePayment(
                command.Data.TransactionId,
                command.Data.AmountReceived,
                command.Data.ProcessedAt,
                command.Data.AuthorizationCode,
                command.Data.FeeDeducted);
        }
        else if (command.Data.Status == "rejected")
        {
            payment.RejectPayment(
                command.Data.TransactionId,
                command.Data.AmountReceived,
                command.Data.ProcessedAt,
                command.Error?.Code,
                command.Error?.Message);
        }

        await _repository.SaveAsync(payment);
        return new ProcessPaymentCallbackResult(true);
    }
}
