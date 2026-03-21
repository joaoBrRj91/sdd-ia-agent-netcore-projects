using System.Text.Json.Serialization;

namespace PaymentService.Api.Models;

public sealed class CallbackNotification
{
    [JsonPropertyName("ticket")]
    public Guid Ticket { get; init; }

    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = "";

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }

    [JsonPropertyName("data")]
    public CallbackDataPayload? Data { get; init; }

    [JsonPropertyName("error")]
    public CallbackErrorPayload? Error { get; init; }
}

public sealed class CallbackDataPayload
{
    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; init; }

    [JsonPropertyName("authorization_code")]
    public string? AuthorizationCode { get; init; }

    [JsonPropertyName("amount_received")]
    public decimal? AmountReceived { get; init; }

    [JsonPropertyName("fee_deducted")]
    public decimal? FeeDeducted { get; init; }

    [JsonPropertyName("processed_at")]
    public DateTimeOffset? ProcessedAt { get; init; }
}

public sealed class CallbackErrorPayload
{
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}
