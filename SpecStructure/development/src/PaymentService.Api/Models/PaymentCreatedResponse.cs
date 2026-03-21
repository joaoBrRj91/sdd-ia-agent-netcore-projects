using System.Text.Json.Serialization;

namespace PaymentService.Api.Models;

public sealed class PaymentCreatedResponse
{
    [JsonPropertyName("ticket")]
    public Guid Ticket { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = "";

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
}
