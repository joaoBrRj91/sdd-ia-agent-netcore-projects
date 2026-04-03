using System.Text.Json.Serialization;

namespace PaymentService.Api.Models;

public sealed class GetPaymentStatusResponse
{
    [JsonPropertyName("ticket")]
    public Guid Ticket { get; init; }

    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = "";

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }

    [JsonPropertyName("data")]
    public GetPaymentStatusData? Data { get; init; }

    [JsonPropertyName("error")]
    public GetPaymentStatusError? Error { get; init; }
}

public sealed class GetPaymentStatusData
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
    public decimal AmountReceived { get; init; }

    [JsonPropertyName("fee_deducted")]
    public decimal? FeeDeducted { get; init; }

    [JsonPropertyName("processed_at")]
    public DateTimeOffset ProcessedAt { get; init; }
}

public sealed class GetPaymentStatusError
{
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}
