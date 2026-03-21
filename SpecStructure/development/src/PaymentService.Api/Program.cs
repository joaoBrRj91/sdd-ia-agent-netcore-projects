using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using PaymentService.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

var payments = new ConcurrentDictionary<Guid, PaymentCreatedResponse>();

app.MapPost("/payments", (PaymentIntentRequest req) =>
{
    if (req.MethodDetails is null || req.Customer is null)
        return Results.BadRequest();

    if (string.IsNullOrWhiteSpace(req.OrderId))
        return Results.BadRequest();

    var currency = req.Currency.Trim().ToUpperInvariant();
    if (currency is not ("BRL" or "USD"))
        return Results.BadRequest();

    if (req.Amount <= 0m)
        return Results.BadRequest();

    var method = req.PaymentMethod.Trim().ToLowerInvariant();
    if (method == "credit_card")
    {
        if (string.IsNullOrWhiteSpace(req.MethodDetails.CardToken))
            return Results.BadRequest();
    }
    else if (method == "pix")
    {
        if (string.IsNullOrWhiteSpace(req.MethodDetails.PixKey))
            return Results.BadRequest();
    }
    else
    {
        return Results.BadRequest();
    }

    var ticket = Guid.NewGuid();
    var createdAt = DateTimeOffset.UtcNow;
    var response = new PaymentCreatedResponse
    {
        Ticket = ticket,
        Status = "Pending",
        CreatedAt = createdAt
    };

    payments[ticket] = response;

    return Results.Ok(response);
});

app.MapPost("/payments/callback", (CallbackNotification _) => Results.NoContent());

app.Run();

public partial class Program;
