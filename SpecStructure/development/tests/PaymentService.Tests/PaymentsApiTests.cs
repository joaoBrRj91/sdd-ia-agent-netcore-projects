using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PaymentService.Api.Models;

namespace PaymentService.Tests;

public class PaymentsApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task PostPayments_PixValid_ReturnsPendingWithTicketAndCreatedAtUtc()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-PIX-777",
            amount = 100.5m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "12345678909", expiration_seconds = 3600 },
            customer = new { name = "João Silva", document = "123.456.789-00" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload.Ticket);
        Assert.Equal("Pending", payload.Status);
        Assert.Equal(DateTimeOffset.UtcNow.Year, payload.CreatedAt.UtcDateTime.Year);
        Assert.Equal(0, payload.CreatedAt.Offset.Ticks); // UTC (Z)
    }

    [Fact]
    public async Task PostPayments_CreditCardValid_ReturnsPendingWithTicket()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-CC-888",
            amount = 250.0m,
            currency = "BRL",
            payment_method = "credit_card",
            method_details = new
            {
                card_token = "tok_brazil_12345",
                installments = 1,
                soft_descriptor = "MARKETPLACE_XYZ"
            },
            customer = new { name = "Maria Oliveira", document = "987.654.321-11" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload.Ticket);
        Assert.Equal("Pending", payload.Status);
    }

    [Fact]
    public async Task PostPayments_UsdCurrency_IsAccepted()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-USD-1",
            amount = 10m,
            currency = "USD",
            payment_method = "pix",
            method_details = new { pix_key = "key" },
            customer = new { name = "A", document = "1" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PostPayments_AmountNotPositive_ReturnsBadRequest(decimal amount)
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-X",
            amount,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "k" },
            customer = new { name = "A", document = "1" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPayments_InvalidCurrency_ReturnsBadRequest()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-X",
            amount = 1m,
            currency = "EUR",
            payment_method = "pix",
            method_details = new { pix_key = "k" },
            customer = new { name = "A", document = "1" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPayments_CreditCardMissingCardToken_ReturnsBadRequest()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-X",
            amount = 1m,
            currency = "BRL",
            payment_method = "credit_card",
            method_details = new { installments = 1 },
            customer = new { name = "A", document = "1" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPayments_PixMissingPixKey_ReturnsBadRequest()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-X",
            amount = 1m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { expiration_seconds = 1 },
            customer = new { name = "A", document = "1" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPayments_TwoRequests_ProduceDifferentTickets()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var bodyA = new
        {
            order_id = "ORD-A",
            amount = 1m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "k" },
            customer = new { name = "A", document = "1" }
        };
        var bodyB = new
        {
            order_id = "ORD-B",
            amount = 1m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "k" },
            customer = new { name = "A", document = "1" }
        };

        var r1 = await client.PostAsJsonAsync("/payments", bodyA, JsonOptions);
        var r2 = await client.PostAsJsonAsync("/payments", bodyB, JsonOptions);

        var p1 = await r1.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);
        var p2 = await r2.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);
        Assert.NotNull(p1);
        Assert.NotNull(p2);
        Assert.NotEqual(p1.Ticket, p2.Ticket);
    }

    [Fact]
    public async Task PostCallback_SuccessEnvelope_ReturnsNoContent()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var body = new
        {
            ticket,
            event_type = "payment.processed",
            timestamp = "2026-03-21T15:42:00Z",
            data = new
            {
                transaction_id = "TX-999888777",
                status = "approved",
                payment_method = "credit_card",
                authorization_code = "AUTH-789",
                amount_received = 250.0,
                fee_deducted = 7.5,
                processed_at = "2026-03-21T15:41:55Z"
            },
            error = (object?)null
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PostCallback_FailureEnvelope_ReturnsNoContent()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var body = new
        {
            ticket,
            event_type = "payment.processed",
            timestamp = "2026-03-21T15:45:00Z",
            data = new
            {
                transaction_id = "TX-failed-123",
                status = "rejected",
                payment_method = "pix",
                amount_received = 0.0,
                processed_at = "2026-03-21T15:44:50Z"
            },
            error = new { code = "insufficient_funds", message = "The transaction was declined due to insufficient funds." }
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PostPayments_MissingOrderId_ReturnsBadRequest()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            amount = 1m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "k" },
            customer = new { name = "A", document = "1" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPayments_EmptyOrderId_ReturnsBadRequest()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "",
            amount = 1m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "k" },
            customer = new { name = "A", document = "1" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPayments_NullMethodDetails_ReturnsBadRequest()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-X",
            amount = 1m,
            currency = "BRL",
            payment_method = "pix",
            customer = new { name = "A", document = "1" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPayments_NullCustomer_ReturnsBadRequest()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-X",
            amount = 1m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "k" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPayments_InvalidPaymentMethod_ReturnsBadRequest()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-X",
            amount = 1m,
            currency = "BRL",
            payment_method = "debit_card",
            method_details = new { pix_key = "k" },
            customer = new { name = "A", document = "1" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
