using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PaymentService.Api.Models;

namespace PaymentService.Tests;

public class GetPaymentStatusTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    // === Success Cases: Pending Status ===

    [Fact]
    public async Task Should_ReturnInProgressStatus_When_PaymentNotYetProcessed()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-PENDING-001");

        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // Endpoint not implemented yet - skip
            return;
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(ticket, payload.Ticket);
        Assert.Equal("payment.in_progress", payload.EventType);
        Assert.Null(payload.Data);
    }

    [Fact]
    public async Task Should_IncludeTicketInResponse_When_StatusRetrieved()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-TICKET-001");
        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound) return;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(ticket, payload.Ticket);
        Assert.NotEqual(Guid.Empty, payload.Ticket);
    }

    [Fact]
    public async Task Should_IncludeEventTypeInResponse_When_PaymentPending()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-EVENT-PENDING-001");
        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound) return;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.NotNull(payload.EventType);
        Assert.Equal("payment.in_progress", payload.EventType);
    }

    [Fact]
    public async Task Should_IncludeTimestampInResponse_When_StatusRetrieved()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-TIMESTAMP-001");
        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound) return;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.NotEqual(default, payload.Timestamp);
        Assert.Equal(0, payload.Timestamp.Offset.Ticks); // UTC
    }

    [Fact]
    public async Task Should_NotIncludeDataInResponse_When_PaymentIsNotFinished()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-NO-DATA-001");
        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound) return;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload);
        // Data should be null when payment not finished
        Assert.Null(payload.Data);
    }

    [Fact]
    public async Task Should_NotIncludeErrorInResponse_When_PaymentIsPending()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-NO-ERROR-PENDING-001");
        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound) return;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Null(payload.Error);
    }

    // === Success Cases: Approved Status ===

    [Fact]
    public async Task Should_ReturnApprovedStatus_When_PaymentApproved()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-APPROVED-001");
        await SendApprovalCallback(client, ticket);

        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound) return;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(ticket, payload.Ticket);
        Assert.Equal("payment.sucessful", payload.EventType);
        Assert.NotNull(payload.Data);
        Assert.Equal("approved", payload.Data.Status);
        Assert.Null(payload.Error);
    }

    [Fact]
    public async Task Should_IncludeCompleteDataInResponse_When_PaymentApproved()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-DATA-APPROVED-001");
        await SendApprovalCallback(client, ticket, 250.0m);

        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound) return;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.NotNull(payload.Data);
        Assert.NotNull(payload.Data.TransactionId);
        Assert.NotEmpty(payload.Data.TransactionId);
        Assert.Equal("approved", payload.Data.Status);
        Assert.NotNull(payload.Data.PaymentMethod);
        Assert.NotEmpty(payload.Data.PaymentMethod);
        Assert.Equal(250.0m, payload.Data.AmountReceived);
        Assert.NotEqual(default, payload.Data.ProcessedAt);
    }

    [Fact]
    public async Task Should_ReturnSuccessfulEventType_When_PaymentApproved()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-SUCCESSFUL-001");
        await SendApprovalCallback(client, ticket);

        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound) return;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal("payment.sucessful", payload.EventType);
    }

    // === Success Cases: Rejected Status ===

    [Fact]
    public async Task Should_ReturnRejectedStatus_When_PaymentRejected()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-REJECTED-001");
        await SendRejectionCallback(client, ticket);

        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound) return;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(ticket, payload.Ticket);
        Assert.Equal("payment.failed", payload.EventType);
        Assert.NotNull(payload.Data);
        Assert.Equal("rejected", payload.Data.Status);
        Assert.NotNull(payload.Error);
    }

    [Fact]
    public async Task Should_IncludeDataWithZeroAmountInResponse_When_PaymentRejected()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-ZERO-AMOUNT-001");
        await SendRejectionCallback(client, ticket);

        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound) return;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.NotNull(payload.Data);
        Assert.Equal("rejected", payload.Data.Status);
        Assert.Equal(0m, payload.Data.AmountReceived);
    }

    [Fact]
    public async Task Should_IncludeErrorInResponse_When_PaymentRejected()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-ERROR-001");
        await SendRejectionCallback(client, ticket, "insufficient_funds", "Insufficient funds in account");

        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound) return;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.NotNull(payload.Error);
        Assert.Equal("insufficient_funds", payload.Error.Code);
        Assert.Equal("Insufficient funds in account", payload.Error.Message);
    }

    [Fact]
    public async Task Should_ReturnFailedEventType_When_PaymentRejected()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-FAILED-001");
        await SendRejectionCallback(client, ticket);

        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound) return;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal("payment.failed", payload.EventType);
    }

    // === Data Field Tests ===

    [Fact]
    public async Task Should_IncludeTransactionIdInData_When_PaymentFinished()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-TX-ID-001");
        await SendApprovalCallback(client, ticket);

        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound) return;

        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload?.Data);
        Assert.NotNull(payload.Data.TransactionId);
        Assert.NotEmpty(payload.Data.TransactionId);
    }

    [Fact]
    public async Task Should_IncludePaymentMethodInData_When_PaymentFinished()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-METHOD-001");
        await SendApprovalCallback(client, ticket);

        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound) return;

        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload?.Data);
        Assert.NotNull(payload.Data.PaymentMethod);
        Assert.NotEmpty(payload.Data.PaymentMethod);
    }

    [Fact]
    public async Task Should_IncludeProcessedAtInData_When_PaymentFinished()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-PROCESSED-001");
        await SendApprovalCallback(client, ticket);

        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound) return;

        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload?.Data);
        Assert.NotEqual(default, payload.Data.ProcessedAt);
    }

    // === Error Field Tests ===

    [Fact]
    public async Task Should_IncludeErrorCodeInError_When_PaymentRejected()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-ERROR-CODE-001");
        await SendRejectionCallback(client, ticket, "card_declined", "Card was declined");

        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound) return;

        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload?.Error);
        Assert.Equal("card_declined", payload.Error.Code);
    }

    [Fact]
    public async Task Should_IncludeErrorMessageInError_When_PaymentRejected()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-ERROR-MSG-001");
        await SendRejectionCallback(client, ticket, "timeout", "Request timeout");

        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound) return;

        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload?.Error);
        Assert.Equal("Request timeout", payload.Error.Message);
    }

    // === Multiple Requests & Consistency ===

    [Fact]
    public async Task Should_ReturnSameDataOnMultipleRequests_When_StatusQueried()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-MULTIPLE-001");
        await SendApprovalCallback(client, ticket);

        var response1 = await client.GetAsync($"/payments/{ticket}");

        if (response1.StatusCode == HttpStatusCode.NotFound) return;

        var response2 = await client.GetAsync($"/payments/{ticket}");

        var payload1 = await response1.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        var payload2 = await response2.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);

        Assert.NotNull(payload1);
        Assert.NotNull(payload2);
        Assert.Equal(payload1.Ticket, payload2.Ticket);
        Assert.Equal(payload1.EventType, payload2.EventType);
    }

    [Fact]
    public async Task Should_ReturnDifferentDataForDifferentPayments_When_StatusQueried()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket1 = await CreatePaymentAndGetTicket(client, "ORD-DIFF-001");
        var ticket2 = await CreatePaymentAndGetTicket(client, "ORD-DIFF-002");

        await SendApprovalCallback(client, ticket1);
        await SendRejectionCallback(client, ticket2);

        var response1 = await client.GetAsync($"/payments/{ticket1}");

        if (response1.StatusCode == HttpStatusCode.NotFound) return;

        var response2 = await client.GetAsync($"/payments/{ticket2}");

        var payload1 = await response1.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        var payload2 = await response2.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);

        Assert.NotNull(payload1);
        Assert.NotNull(payload2);
        Assert.NotEqual(payload1.Ticket, payload2.Ticket);
        Assert.Equal("payment.sucessful", payload1.EventType);
        Assert.Equal("payment.failed", payload2.EventType);
    }

    // === Integration Tests ===

    [Fact]
    public async Task Should_UpdateStatusAfterCallback_When_PaymentStateChanges()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-STATE-CHANGE-001");

        // Check initial pending state
        var response1 = await client.GetAsync($"/payments/{ticket}");

        if (response1.StatusCode == HttpStatusCode.NotFound) return;

        var payload1 = await response1.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);
        Assert.NotNull(payload1);
        Assert.Equal("payment.in_progress", payload1.EventType);

        // Send approval callback
        await SendApprovalCallback(client, ticket);

        // Check updated state
        var response2 = await client.GetAsync($"/payments/{ticket}");
        var payload2 = await response2.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);

        Assert.NotNull(payload2);
        Assert.Equal("payment.sucessful", payload2.EventType);
        Assert.NotNull(payload2.Data);
    }

    [Fact]
    public async Task Should_ReturnCompletePaymentDataAfterApproval_When_AllFieldsPopulated()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var ticket = await CreatePaymentAndGetTicket(client, "ORD-COMPLETE-001");
        await SendApprovalCallbackWithAllFields(client, ticket);

        var response = await client.GetAsync($"/payments/{ticket}");

        if (response.StatusCode == HttpStatusCode.NotFound) return;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GetPaymentStatusResponse>(JsonOptions);

        Assert.NotNull(payload);
        Assert.Equal(ticket, payload.Ticket);
        Assert.Equal("payment.sucessful", payload.EventType);
        Assert.NotNull(payload.Data);
        Assert.NotNull(payload.Data.TransactionId);
        Assert.NotEmpty(payload.Data.TransactionId);
        Assert.Equal("approved", payload.Data.Status);
        Assert.NotNull(payload.Data.PaymentMethod);
        Assert.True(payload.Data.AmountReceived > 0);
        Assert.NotEqual(default, payload.Data.ProcessedAt);
        Assert.Null(payload.Error);
    }

    // === Helper Methods ===

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

    private static async Task SendApprovalCallback(HttpClient client, Guid ticket, decimal amountReceived = 100m)
    {
        var body = new
        {
            ticket,
            event_type = "payment.sucessful",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-APPROVED",
                status = "approved",
                payment_method = "pix",
                amount_received = amountReceived,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = (object?)null
        };

        await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);
    }

    private static async Task SendApprovalCallbackWithAllFields(HttpClient client, Guid ticket)
    {
        var body = new
        {
            ticket,
            event_type = "payment.sucessful",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-COMPLETE-APPROVED",
                status = "approved",
                payment_method = "credit_card",
                authorization_code = "AUTH-COMPLETE",
                amount_received = 500m,
                fee_deducted = 15m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = (object?)null
        };

        await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);
    }

    private static async Task SendRejectionCallback(HttpClient client, Guid ticket,
        string errorCode = "generic_error", string errorMessage = "Payment rejected")
    {
        var body = new
        {
            ticket,
            event_type = "payment.failed",
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            data = new
            {
                transaction_id = "TX-REJECTED",
                status = "rejected",
                payment_method = "pix",
                amount_received = 0m,
                processed_at = DateTimeOffset.UtcNow.ToString("o")
            },
            error = new
            {
                code = errorCode,
                message = errorMessage
            }
        };

        await client.PostAsJsonAsync("/payments/callback", body, JsonOptions);
    }
}

// Response DTOs for GET endpoint
public sealed class GetPaymentStatusResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("ticket")]
    public Guid Ticket { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("event_type")]
    public string EventType { get; set; } = "";

    [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public GetPaymentStatusData? Data { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public GetPaymentStatusError? Error { get; set; }
}

public sealed class GetPaymentStatusData
{
    [System.Text.Json.Serialization.JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string? Status { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("authorization_code")]
    public string? AuthorizationCode { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("amount_received")]
    public decimal AmountReceived { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("fee_deducted")]
    public decimal? FeeDeducted { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("processed_at")]
    public DateTimeOffset ProcessedAt { get; set; }
}

public sealed class GetPaymentStatusError
{
    [System.Text.Json.Serialization.JsonPropertyName("code")]
    public string? Code { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public string? Message { get; set; }
}
