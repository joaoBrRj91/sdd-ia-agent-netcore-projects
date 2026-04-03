using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PaymentService.Api.Models;

namespace PaymentService.Tests;

public class ProcessPaymentCallbackTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    // === Success Cases ===

    [Fact]
    public async Task Should_ProcessApprovedCallback_When_ValidSuccessPayloadProvided()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        // First, create a payment
        var createBody = new
        {
            order_id = "ORD-CALLBACK-001",
            amount = 100m,
            currency = "BRL",
            payment_method = "credit_card",
            method_details = new { card_token = "tok_123" },
            customer = new { name = "User", document = "123" }
        };

        var createResponse = await client.PostAsJsonAsync("/payments", createBody, JsonOptions);
        var createPayload = await createResponse.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);
        Assert.NotNull(createPayload);
        var ticket = createPayload.Ticket;

        // Now send success callback
        var callbackBody = new
        {
            ticket,
            event_type = "payment.sucessful",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-999888777",
                status = "approved",
                payment_method = "credit_card",
                authorization_code = "AUTH-789",
                amount_received = 100m,
                fee_deducted = 2.5m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = (object?)null
        };

        var callbackResponse = await client.PostAsJsonAsync("/payments/callback", callbackBody, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, callbackResponse.StatusCode);
    }

    [Fact]
    public async Task Should_ProcessRejectedCallback_When_ValidFailurePayloadProvided()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        // First, create a payment
        var createBody = new
        {
            order_id = "ORD-CALLBACK-002",
            amount = 100m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "key" },
            customer = new { name = "User", document = "123" }
        };

        var createResponse = await client.PostAsJsonAsync("/payments", createBody, JsonOptions);
        var createPayload = await createResponse.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);
        Assert.NotNull(createPayload);
        var ticket = createPayload.Ticket;

        // Now send failure callback
        var callbackBody = new
        {
            ticket,
            event_type = "payment.failed",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-failed-123",
                status = "rejected",
                payment_method = "pix",
                amount_received = 0m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = new
            {
                code = "insufficient_funds",
                message = "The transaction was declined due to insufficient funds."
            }
        };

        var callbackResponse = await client.PostAsJsonAsync("/payments/callback", callbackBody, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, callbackResponse.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnNoContent_When_ApprovedCallbackProcessed()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = Guid.NewGuid();
        var body = new
        {
            ticket,
            event_type = "payment.approved",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-123",
                status = "approved",
                payment_method = "credit_card",
                amount_received = 250m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = (object?)null
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnNoContent_When_RejectedCallbackProcessed()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = Guid.NewGuid();
        var body = new
        {
            ticket,
            event_type = "payment.rejected",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-failed-123",
                status = "rejected",
                payment_method = "pix",
                amount_received = 0m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = new
            {
                code = "network_error",
                message = "Connection timeout"
            }
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Should_AcceptCallbackWithCompleteData_When_AllFieldsProvided()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = Guid.NewGuid();
        var body = new
        {
            ticket,
            event_type = "payment.processed",
            timestamp = "2026-03-21T15:42:00Z",
            data = new
            {
                transaction_id = "TX-COMPLETE-001",
                status = "approved",
                payment_method = "credit_card",
                authorization_code = "AUTH-COMPLETE",
                amount_received = 1000.50m,
                fee_deducted = 25.00m,
                processed_at = "2026-03-21T15:41:55Z"
            },
            error = (object?)null
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Should_AcceptCallbackWithMinimalErrorData_When_RejectionWithoutOptionalFields()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = Guid.NewGuid();
        var body = new
        {
            ticket,
            event_type = "payment.failed",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-MIN-001",
                status = "rejected",
                payment_method = "pix",
                amount_received = 0m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = new
            {
                code = "generic_error",
                message = "Payment rejected"
            }
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Should_HandleCallbackForNonExistentTicket_When_TicketNotFound()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var unknownTicket = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var body = new
        {
            ticket = unknownTicket,
            event_type = "payment.approved",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-UNKNOWN",
                status = "approved",
                payment_method = "pix",
                amount_received = 100m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = (object?)null
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        // Should still return NoContent - graceful handling
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Should_ProcessMultipleCallbacks_When_SeparatePaymentsSentCallbacks()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        // Create two payments
        var ticket1 = await CreatePaymentAndGetTicket(client, "ORD-001");
        var ticket2 = await CreatePaymentAndGetTicket(client, "ORD-002");

        // Send callbacks for both
        var callback1 = new
        {
            ticket = ticket1,
            event_type = "payment.approved",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-001",
                status = "approved",
                payment_method = "pix",
                amount_received = 100m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = (object?)null
        };

        var callback2 = new
        {
            ticket = ticket2,
            event_type = "payment.rejected",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-002",
                status = "rejected",
                payment_method = "pix",
                amount_received = 0m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = new { code = "declined", message = "Card declined" }
        };

        var response1 = await client.PostAsJsonAsync("/payments/callback", callback1, JsonOptions);
        var response2 = await client.PostAsJsonAsync("/payments/callback", callback2, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response1.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, response2.StatusCode);
    }

    // === Data Validation Tests ===

    [Fact]
    public async Task Should_ReturnBadRequest_When_DataPayloadIsMissing()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            ticket = Guid.NewGuid(),
            event_type = "payment.approved",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            error = (object?)null
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_DataPayloadIsNull()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            ticket = Guid.NewGuid(),
            event_type = "payment.approved",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = (object?)null,
            error = (object?)null
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // === Event Type Tests ===

    [Theory]
    [InlineData("payment.sucessful")]
    [InlineData("payment.failed")]
    [InlineData("payment.approved")]
    [InlineData("payment.rejected")]
    [InlineData("payment.processed")]
    public async Task Should_AcceptValidEventTypes_When_DifferentEventTypesProvided(string eventType)
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var body = new
        {
            ticket = Guid.NewGuid(),
            event_type = eventType,
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-TEST",
                status = "approved",
                payment_method = "pix",
                amount_received = 100m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = (object?)null
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // === Callback with Approval Status ===

    [Fact]
    public async Task Should_ProcessApprovedStatus_When_DataStatusIsApproved()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = Guid.NewGuid();
        var body = new
        {
            ticket,
            event_type = "payment.callback",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-APPROVED",
                status = "approved",
                payment_method = "credit_card",
                amount_received = 500m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = (object?)null
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Should_ProcessRejectedStatus_When_DataStatusIsRejected()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = Guid.NewGuid();
        var body = new
        {
            ticket,
            event_type = "payment.callback",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-REJECTED",
                status = "rejected",
                payment_method = "pix",
                amount_received = 0m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = new { code = "declined", message = "Payment declined" }
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // === Callback with Different Payment Methods ===

    [Fact]
    public async Task Should_AcceptCallbackForCreditCardPayment_When_PaymentMethodIsCreditCard()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = Guid.NewGuid();
        var body = new
        {
            ticket,
            event_type = "payment.callback",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-CC-123",
                status = "approved",
                payment_method = "credit_card",
                authorization_code = "AUTH-CC-123",
                amount_received = 250m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = (object?)null
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Should_AcceptCallbackForPixPayment_When_PaymentMethodIsPix()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = Guid.NewGuid();
        var body = new
        {
            ticket,
            event_type = "payment.callback",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-PIX-123",
                status = "approved",
                payment_method = "pix",
                amount_received = 150m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = (object?)null
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // === Error Payload Tests ===

    [Fact]
    public async Task Should_AcceptCallbackWithoutError_When_ApprovedTransaction()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = Guid.NewGuid();
        var body = new
        {
            ticket,
            event_type = "payment.callback",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-NO-ERROR",
                status = "approved",
                payment_method = "pix",
                amount_received = 100m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = (object?)null
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Should_AcceptCallbackWithError_When_RejectedTransaction()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = Guid.NewGuid();
        var body = new
        {
            ticket,
            event_type = "payment.callback",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-WITH-ERROR",
                status = "rejected",
                payment_method = "pix",
                amount_received = 0m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = new
            {
                code = "insufficient_funds",
                message = "Insufficient funds in account"
            }
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData("insufficient_funds")]
    [InlineData("card_declined")]
    [InlineData("network_error")]
    [InlineData("invalid_account")]
    [InlineData("transaction_timeout")]
    public async Task Should_AcceptDifferentErrorCodes_When_VariousErrorsOccur(string errorCode)
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = Guid.NewGuid();
        var body = new
        {
            ticket,
            event_type = "payment.callback",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-ERROR-CODE",
                status = "rejected",
                payment_method = "pix",
                amount_received = 0m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = new
            {
                code = errorCode,
                message = "Error message"
            }
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // === Amount and Fee Tests ===

    [Fact]
    public async Task Should_AcceptCallbackWithZeroAmountReceived_When_RejectedPayment()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = Guid.NewGuid();
        var body = new
        {
            ticket,
            event_type = "payment.callback",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-ZERO-AMOUNT",
                status = "rejected",
                payment_method = "pix",
                amount_received = 0m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = new { code = "declined", message = "Declined" }
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Should_AcceptCallbackWithNonZeroAmountReceived_When_ApprovedPayment()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = Guid.NewGuid();
        var body = new
        {
            ticket,
            event_type = "payment.callback",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-NONZERO-AMOUNT",
                status = "approved",
                payment_method = "credit_card",
                amount_received = 999.99m,
                fee_deducted = 29.99m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = (object?)null
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Should_AcceptCallbackWithLargeAmount_When_HighValueTransaction()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = Guid.NewGuid();
        var body = new
        {
            ticket,
            event_type = "payment.callback",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-LARGE-AMOUNT",
                status = "approved",
                payment_method = "credit_card",
                amount_received = 999999999.99m,
                fee_deducted = 9999999.99m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = (object?)null
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // === Timestamp Tests ===

    [Fact]
    public async Task Should_AcceptCallbackWithCurrentTimestamp_When_RecentTransaction()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var now = DateTimeOffset.UtcNow;
        var ticket = Guid.NewGuid();
        var body = new
        {
            ticket,
            event_type = "payment.callback",
            timestamp = now.ToString("o"),
            data = new
            {
                transaction_id = "TX-CURRENT-TIME",
                status = "approved",
                payment_method = "pix",
                amount_received = 100m,
                processed_at = now.ToString("o")
            },
            error = (object?)null
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Should_AcceptCallbackWithPastTimestamp_When_DelayedNotification()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var pastTime = DateTimeOffset.UtcNow.AddHours(-1);
        var ticket = Guid.NewGuid();
        var body = new
        {
            ticket,
            event_type = "payment.callback",
            timestamp = pastTime.ToString("o"),
            data = new
            {
                transaction_id = "TX-PAST-TIME",
                status = "approved",
                payment_method = "credit_card",
                amount_received = 100m,
                processed_at = pastTime.ToString("o")
            },
            error = (object?)null
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // === Transaction ID Tests ===

    [Fact]
    public async Task Should_AcceptCallbackWithUniqueTransactionId_When_ProvidedInData()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var uniqueTxId = $"TX-{Guid.NewGuid()}";
        var ticket = Guid.NewGuid();
        var body = new
        {
            ticket,
            event_type = "payment.callback",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = uniqueTxId,
                status = "approved",
                payment_method = "pix",
                amount_received = 100m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = (object?)null
        };

        var response = await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // === Integration Tests ===

    [Fact]
    public async Task Should_ProcessFullPaymentLifecycle_When_CreateThenCallback()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        // Create payment
        var createBody = new
        {
            order_id = "ORD-FULL-LIFECYCLE",
            amount = 500m,
            currency = "BRL",
            payment_method = "credit_card",
            method_details = new { card_token = "tok_123" },
            customer = new { name = "Customer", document = "123" }
        };

        var createResponse = await client.PostAsJsonAsync("/payments", createBody, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var createPayload = await createResponse.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);
        Assert.NotNull(createPayload);
        Assert.Equal("Pending", createPayload.Status);
        var ticket = createPayload.Ticket;

        // Process callback with approval
        var callbackBody = new
        {
            ticket,
            event_type = "payment.sucessful",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-APPROVED-123",
                status = "approved",
                payment_method = "credit_card",
                authorization_code = "AUTH-123",
                amount_received = 500m,
                fee_deducted = 15m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = (object?)null
        };

        var callbackResponse = await client.PostAsJsonAsync("/payments/callback", callbackBody, JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, callbackResponse.StatusCode);
    }

    // === Helper Method ===

    private static async Task<Guid> CreatePaymentAndGetTicket(HttpClient client, string orderId)
    {
        var body = new
        {
            order_id = orderId,
            amount = 100m,
            currency = "BRL",
            payment_method = "pix",
            method_details = new { pix_key = "key" },
            customer = new { name = "User", document = "123" }
        };

        var response = await client.PostAsJsonAsync("/payments", body, JsonOptions);
        var payload = await response.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOptions);
        return payload?.Ticket ?? Guid.Empty;
    }
}
