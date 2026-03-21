namespace PaymentService.Api.Domain.ValueObjects;

public sealed class Customer
{
    private Customer(string name, string document)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Customer name cannot be empty");
        if (string.IsNullOrWhiteSpace(document))
            throw new ArgumentException("Customer document cannot be empty");
        
        Name = name;
        Document = document;
    }

    public string Name { get; }
    public string Document { get; }

    public static Customer Create(string name, string document)
    {
        return new(name, document);
    }

    public override string ToString() => $"{Name} ({Document})";
    public override bool Equals(object? obj) => obj is Customer c && c.Document == Document;
    public override int GetHashCode() => Document.GetHashCode();
}
