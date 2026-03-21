namespace PaymentService.Api.Application.DTOs;

public sealed class CreatePaymentResponseDto
{
    public Guid Ticket { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAt { get; set; }
}
