namespace PaymentService.Api.Domain.ValueObjects;

public sealed class PaymentMethod
{
    private PaymentMethod(string type, string? pixKey = null, string? cardToken = null)
    {
        Type = type;
        PixKey = pixKey;
        CardToken = cardToken;
    }

    public string Type { get; }
    public string? PixKey { get; }
    public string? CardToken { get; }

    public static PaymentMethod CreatePix(string pixKey)
    {
        if (string.IsNullOrWhiteSpace(pixKey))
            throw new ArgumentException("Pix key cannot be empty");
        return new("pix", pixKey: pixKey);
    }

    public static PaymentMethod CreateCreditCard(string cardToken)
    {
        if (string.IsNullOrWhiteSpace(cardToken))
            throw new ArgumentException("Card token cannot be empty");
        return new("credit_card", cardToken: cardToken);
    }

    public static PaymentMethod Create(string type, string? pixKey = null, string? cardToken = null)
    {
        var normalizedType = type.Trim().ToLowerInvariant();
        return normalizedType switch
        {
            "pix" => CreatePix(pixKey ?? ""),
            "credit_card" => CreateCreditCard(cardToken ?? ""),
            _ => throw new ArgumentException($"Invalid payment method: {type}")
        };
    }

    public override string ToString() => Type;
    public override bool Equals(object? obj) => obj is PaymentMethod pm && pm.Type == Type;
    public override int GetHashCode() => Type.GetHashCode();
}
