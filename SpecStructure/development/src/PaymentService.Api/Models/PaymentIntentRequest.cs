using System.Text.Json.Serialization;

namespace PaymentService.Api.Models;

public sealed class PaymentIntentRequest
{
    [JsonPropertyName("order_id")]
    public string OrderId { get; init; } = "";

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = "";

    [JsonPropertyName("payment_method")]
    public string PaymentMethod { get; init; } = "";

    [JsonPropertyName("method_details")]
    public MethodDetailsPayload? MethodDetails { get; init; }

    [JsonPropertyName("customer")]
    public CustomerPayload? Customer { get; init; }
}

public sealed class MethodDetailsPayload
{
    [JsonPropertyName("pix_key")]
    public string? PixKey { get; init; }

    [JsonPropertyName("card_token")]
    public string? CardToken { get; init; }

    [JsonPropertyName("installments")]
    public int? Installments { get; init; }

    [JsonPropertyName("soft_descriptor")]
    public string? SoftDescriptor { get; init; }

    [JsonPropertyName("expiration_seconds")]
    public int? ExpirationSeconds { get; init; }
}

public sealed class CustomerPayload
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("document")]
    public string Document { get; init; } = "";
}
