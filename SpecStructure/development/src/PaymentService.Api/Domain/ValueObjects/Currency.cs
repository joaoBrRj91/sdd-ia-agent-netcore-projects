namespace PaymentService.Api.Domain.ValueObjects;

public sealed class Currency
{
    private Currency(string code)
    {
        Code = code;
    }

    public string Code { get; }

    public static Currency BRL => new("BRL");
    public static Currency USD => new("USD");

    public static Currency Create(string code)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return normalized switch
        {
            "BRL" => BRL,
            "USD" => USD,
            _ => throw new ArgumentException($"Invalid currency: {code}")
        };
    }

    public override string ToString() => Code;
    public override bool Equals(object? obj) => obj is Currency c && c.Code == Code;
    public override int GetHashCode() => Code.GetHashCode();
}
