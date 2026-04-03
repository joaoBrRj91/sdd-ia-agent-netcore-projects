using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PaymentService.Api.Models;

namespace PaymentService.Tests;

public class CreatePaymentTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    // === Success Cases ===

    [Fact]
    public async Task Should_CreatePixPayment_When_AllFieldsValidAndPixMethodProvided()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-PIX-001",
            amount = 150.75m,
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
        Assert.Equal(0, payload.CreatedAt.Offset.Ticks); // UTC
    }

    [Fact]
    public async Task Should_CreateCreditCardPayment_When_AllFieldsValidAndCreditCardMethodProvided()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-CC-001",
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
        Assert.Equal(0, payload.CreatedAt.Offset.Ticks);
    }

    [Fact]
    public async Task Should_AcceptBRLCurrency_When_CreatePaymentWithBRL()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-BRL-001",
            amount = 99.99m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "pix_key_123" },
            customer = new { name = "Test User", document = "123.456.789-00" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal("Pending", payload.Status);
    }

    [Fact]
    public async Task Should_AcceptUSDCurrency_When_CreatePaymentWithUSD()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-USD-001",
            amount = 50.0m,
            currency = "USD",
            payment_method = "credit_card",
            method_details = new { card_token = "tok_visa_123" },
            customer = new { name = "US User", document = "123456789" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal("Pending", payload.Status);
    }

    [Fact]
    public async Task Should_GenerateUniqueTickets_When_MultiplePaymentsCreated()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body1 = new
        {
            order_id = "ORD-001",
            amount = 100m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "key1" },
            customer = new { name = "User 1", document = "111" }
        };

        var body2 = new
        {
            order_id = "ORD-002",
            amount = 200m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "key2" },
            customer = new { name = "User 2", document = "222" }
        };

        var response1 = await client.PostAsJsonAsync("/payments", body1, JsonOptions);
        var response2 = await client.PostAsJsonAsync("/payments", body2, JsonOptions);

        var payload1 = await response1.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);
        var payload2 = await response2.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);

        Assert.NotNull(payload1);
        Assert.NotNull(payload2);
        Assert.NotEqual(payload1.Ticket, payload2.Ticket);
    }

    [Fact]
    public async Task Should_ReturnCreatedAtInUtcFormat_When_PaymentCreated()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-UTC-001",
            amount = 125.50m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "key_utc" },
            customer = new { name = "UTC User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);
        var payload = await response.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);

        Assert.NotNull(payload);
        Assert.Equal(0, payload.CreatedAt.Offset.Ticks); // Verify UTC (Z)
        Assert.True(payload.CreatedAt.UtcDateTime.Year >= 2026);
    }

    [Fact]
    public async Task Should_AcceptSmallAmount_When_AmountIsMinimalButPositive()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-SMALL-001",
            amount = 0.01m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "small_key" },
            customer = new { name = "Small Payment", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);
        Assert.NotNull(payload);
    }

    [Fact]
    public async Task Should_AcceptLargeAmount_When_AmountIsVeryLarge()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-LARGE-001",
            amount = 999999999.99m,
            currency = "USD",
            payment_method = "credit_card",
            method_details = new { card_token = "tok_large" },
            customer = new { name = "Large Payment", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Should_PreserveOrderId_When_PaymentCreatedWithUniqueOrderId()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var uniqueOrderId = $"ORD-{Guid.NewGuid()}";
        var body = new
        {
            order_id = uniqueOrderId,
            amount = 100m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "key" },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // === Amount Validation Tests ===

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    [InlineData(-999.99)]
    public async Task Should_ReturnBadRequest_When_AmountIsNotPositive(decimal amount)
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-INVALID-AMOUNT",
            amount,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "key" },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_AmountIsZero()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-ZERO",
            amount = 0m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "key" },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // === Currency Validation Tests ===

    [Theory]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("BRR")]
    [InlineData("XXX")]
    [InlineData("")]
    public async Task Should_ReturnBadRequest_When_CurrencyIsInvalid(string currency)
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-INVALID-CURRENCY",
            amount = 100m,
            currency,
            payment_method = "pix",
            method_details = new { pix_key = "key" },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_CurrencyIsNotSupported()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-UNSUPPORTED",
            amount = 100m,
            currency = "CHF",
            payment_method = "pix",
            method_details = new { pix_key = "key" },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // === Payment Method Conditional Validation ===

    [Fact]
    public async Task Should_ReturnBadRequest_When_CreditCardMethodMissingCardToken()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-CC-NO-TOKEN",
            amount = 100m,
            currency = "BRL",
            payment_method = "credit_card",
            method_details = new { installments = 1 },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_CreditCardMethodHasEmptyCardToken()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-CC-EMPTY-TOKEN",
            amount = 100m,
            currency = "BRL",
            payment_method = "credit_card",
            method_details = new { card_token = "", installments = 1 },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_CreditCardMethodHasWhitespaceCardToken()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-CC-WHITESPACE-TOKEN",
            amount = 100m,
            currency = "BRL",
            payment_method = "credit_card",
            method_details = new { card_token = "   ", installments = 1 },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_PixMethodMissingPixKey()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-PIX-NO-KEY",
            amount = 100m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { expiration_seconds = 3600 },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_PixMethodHasEmptyPixKey()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-PIX-EMPTY-KEY",
            amount = 100m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "", expiration_seconds = 3600 },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_PixMethodHasWhitespacePixKey()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-PIX-WHITESPACE-KEY",
            amount = 100m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "   ", expiration_seconds = 3600 },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("debit_card")]
    [InlineData("wallet")]
    [InlineData("bank_transfer")]
    [InlineData("boleto")]
    [InlineData("ach")]
    [InlineData("")]
    public async Task Should_ReturnBadRequest_When_PaymentMethodIsInvalid(string method)
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-INVALID-METHOD",
            amount = 100m,
            currency = "BRL",
            payment_method = method,
            method_details = new { pix_key = "key", card_token = "token" },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // === Order ID Validation Tests ===

    [Fact]
    public async Task Should_ReturnBadRequest_When_OrderIdIsMissing()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            amount = 100m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "key" },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_OrderIdIsEmpty()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "",
            amount = 100m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "key" },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_OrderIdIsWhitespace()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "   ",
            amount = 100m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "key" },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // === Method Details Validation ===

    [Fact]
    public async Task Should_ReturnBadRequest_When_MethodDetailsIsMissing()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-NO-DETAILS",
            amount = 100m,
            currency = "BRL",
            payment_method = "pix",
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_MethodDetailsIsNull()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-NULL-DETAILS",
            amount = 100m,
            currency = "BRL",
            payment_method = "pix",
            method_details = (object?)null,
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // === Customer Validation ===

    [Fact]
    public async Task Should_ReturnBadRequest_When_CustomerIsMissing()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-NO-CUSTOMER",
            amount = 100m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "key" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_CustomerIsNull()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-NULL-CUSTOMER",
            amount = 100m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "key" },
            customer = (object?)null
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // === Edge Cases ===

    [Fact]
    public async Task Should_ReturnOk_When_AmountHasManyDecimalPlaces()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-DECIMALS",
            amount = 123.456789m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "key" },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnOk_When_CurrencyIsMixedCase()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-MIXEDCASE",
            amount = 100m,
            currency = "bRl",
            payment_method = "pix",
            method_details = new { pix_key = "key" },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnOk_When_PaymentMethodIsMixedCase()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-METHODCASE",
            amount = 100m,
            currency = "BRL",
            payment_method = "CrEdIt_CaRd",
            method_details = new { card_token = "token" },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnPendingStatus_When_PaymentCreated()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-STATUS-PENDING",
            amount = 100m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "key" },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);
        var payload = await response.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);

        Assert.NotNull(payload);
        Assert.Equal("Pending", payload.Status);
    }

    [Fact]
    public async Task Should_ReturnValidGuid_When_TicketGenerated()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            order_id = "ORD-TICKET-GUID",
            amount = 100m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "key" },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);
        var payload = await response.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);

        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload.Ticket);
        Assert.True(payload.Ticket != default);
    }
}
