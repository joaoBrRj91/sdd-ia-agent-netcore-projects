namespace PaymentService.Api.Domain.ValueObjects;

public sealed class Money
{
    private Money(decimal amount, Currency currency)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero");
        
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }
    public Currency Currency { get; }

    public static Money Create(decimal amount, Currency currency)
    {
        return new(amount, currency);
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
    public override bool Equals(object? obj) => obj is Money m && m.Amount == Amount && m.Currency.Equals(Currency);
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);
}
