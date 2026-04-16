# 💳 Specification: Payment Service (Gateway)

## 👤 Persona & Scope

This service is the **Single Source of Truth (SSOT)** for the asynchronous payment gateway.

- **Orchestrator Role:** Specialist in Spec-Driven Development (SDD).
- **Primary Source:** `~/specs/payment-service.md`
- **Development Path:** `~/development/src`
- **Testing Path:** `~/development/tests`

---

## 🎯 Service Overview

The **Payment Service** is a microservice designed to act as a gateway for **Credit Card** and **Pix** transactions.
The architecture follows an **Asynchronous Pattern**:

1. **Request:** The client submits a payment intent.
2. **Acknowledgment:** The service validates the payload and returns a `ticket` (GUID) with a `Pending` status.
3. **Processing:** The transaction is processed by the external provider in the background.
4. **Notification:** The final result (Success/Failure) is received through the **Webhook Callback** endpoint and applied to the local payment state.
5. **Event Propagation:** After the callback is processed successfully, the service publishes an asynchronous integration message to a RabbitMQ queue so other services can react without blocking the HTTP request thread.

---

## 🛠 Business Rules

1. **Value Validation:** The `amount` field must be a positive decimal strictly greater than zero (`> 0.00`).
2. **Currency Support:** Only `BRL` and `USD` are permitted.
3. **State Machine:** All transactions must be initialized in the `Pending` state.
4. **Traceability:**
   - `order_id`: Client-side unique reference.
   - `ticket`: Gateway-side unique process identifier.
5. **Conditional Requirements:**
   - If `payment_method` is `credit_card`, `card_token` is **required**.
   - If `payment_method` is `pix`, `pix_key` is **required**.

---

## 🛣 API Endpoints

### 1. Request Payment

**Endpoint:** `POST /payments`  
**Description:** Receives the payment intent and returns an execution ticket.

#### **Scenario A: Pix Method**

**Request Body:**

```json
{
  "order_id": "ORD-PIX-777",
  "amount": 100.5,
  "currency": "BRL",
  "payment_method": "pix",
  "method_details": {
    "pix_key": "12345678909",
    "expiration_seconds": 3600
  },
  "customer": {
    "name": "João Silva",
    "document": "123.456.789-00"
  }
}
```

**Response Body:**

```json
{
  "ticket": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Pending",
  "created_at": "2026-03-21T15:40:00Z"
}
```

#### **Scenario B: Credit Card Method**

**Request Body:**

```json
{
  "order_id": "ORD-CC-888",
  "amount": 250.0,
  "currency": "BRL",
  "payment_method": "credit_card",
  "method_details": {
    "card_token": "tok_brazil_12345",
    "installments": 1,
    "soft_descriptor": "MARKETPLACE_XYZ"
  },
  "customer": {
    "name": "Maria Oliveira",
    "document": "987.654.321-11"
  }
}
```

**Response Body:**

```json
{
  "ticket": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Pending",
  "created_at": "2026-03-21T15:40:00Z"
}
```

### 2. Payment Callback

**Endpoint:** `POST /payments/callback`  
**Description:** Out-of-band notification sent from the Gateway to the Client. The service processes the callback, updates the local payment state, invalidates cache entries when applicable, and publishes an integration event to RabbitMQ at the end of the processing flow.

#### **Callback Processing Requirements**

1. The callback payload must be processed and persisted before any integration message is published.
2. Cache invalidation for `GET /payments/{ticket_id}` must happen during callback processing when the ticket is found.
3. At the end of a successful callback processing flow, the service must publish a message to a RabbitMQ integration queue.
4. This publication must be asynchronous and must not block or stall the main HTTP request thread.
5. The HTTP response from `POST /payments/callback` must not wait for downstream consumers to finish processing the published message.

#### **Scenario A: Success**

**Request Body:**

```json
{
  "ticket": "550e8400-e29b-41d4-a716-446655440000",
  "event_type": "payment.sucessful",
  "timestamp": "2026-03-21T15:42:00Z",
  "data": {
    "transaction_id": "TX-999888777",
    "status": "approved",
    "payment_method": "credit_card",
    "authorization_code": "AUTH-789",
    "amount_received": 250.0,
    "fee_deducted": 7.5,
    "processed_at": "2026-03-21T15:41:55Z"
  },
  "error": null
}
```

#### **Scenario B: Failure**

**Request Body:**

```json
{
  "ticket": "550e8400-e29b-41d4-a716-446655440000",
  "event_type": "payment.failed",
  "timestamp": "2026-03-21T15:45:00Z",
  "data": {
    "transaction_id": "TX-failed-123",
    "status": "rejected",
    "payment_method": "pix",
    "amount_received": 0.0,
    "processed_at": "2026-03-21T15:44:50Z"
  },
  "error": {
    "code": "insufficient_funds",
    "message": "The transaction was declined due to insufficient funds."
  }
}
```

### 3. Get Payment Status

**Endpoint:** `GET /payments/{ticket_id}`  
**Description:** Get status of a payment by its ticket ID. If the status is not finished return only

#### **Scenario A: Success but not finished**

**Response Body:**

```json
{
  "ticket": "550e8400-e29b-41d4-a716-446655440000",
  "event_type": "payment.in_progress",
  "timestamp": "2026-03-21T15:42:00Z",
  "error": null
}
```

#### **Scenario B: Success and finished**

**Response Body:**

```json
{
  "ticket": "550e8400-e29b-41d4-a716-446655440000",
  "event_type": "payment.sucessful",
  "timestamp": "2026-03-21T15:45:00Z",
  "data": {
    "transaction_id": "TX-failed-123",
    "status": "rejected",
    "payment_method": "pix",
    "amount_received": 0.0,
    "processed_at": "2026-03-21T15:44:50Z"
  },
  "error": null
}
```

#### **Scenario B: Failure**

**Response Body:**

```json
{
  "ticket": "550e8400-e29b-41d4-a716-446655440000",
  "event_type": "payment.failed",
  "timestamp": "2026-03-21T15:45:00Z",
  "data": {
    "transaction_id": "TX-failed-123",
    "status": "rejected",
    "payment_method": "pix",
    "amount_received": 0.0,
    "processed_at": "2026-03-21T15:44:50Z"
  },
  "error": {
    "code": "insufficient_funds",
    "message": "The transaction was declined due to insufficient funds."
  }
}
```

---

## 📨 Messaging / Event Propagation

### RabbitMQ Integration

After a valid payment callback is processed successfully, the service must publish an integration message to RabbitMQ so other services can continue the workflow asynchronously.

**Requirements:**

1. **Broker:** RabbitMQ is the target broker for the integration event flow.
2. **Publish Timing:** The message must be published only after the local callback processing has completed successfully.
3. **Async Publication:** The publish operation must be asynchronous to avoid blocking the main request thread.
4. **Queue Purpose:** The queue is intended for cross-service integration and downstream processing.
5. **Consumer:** A dedicated consumer must read messages from this queue.
6. **Incremental Delivery:** In the first iteration, the consumer only performs an informational log entry after receiving the message.
7. **Failure Isolation:** Consumer failures must not alter the HTTP response already returned by the callback endpoint.

### Published Event Contract

The integration message published to RabbitMQ must preserve the semantic meaning of the processed callback result. Downstream consumers must receive the same approved/rejected outcome that was accepted by the callback endpoint.

**Minimum Payload:**

```json
{
  "ticket": "550e8400-e29b-41d4-a716-446655440000",
  "event_type": "payment.sucessful",
  "timestamp": "2026-03-21T15:42:00Z",
  "data": {
    "transaction_id": "TX-999888777",
    "status": "approved",
    "payment_method": "credit_card",
    "authorization_code": "AUTH-789",
    "amount_received": 250.0,
    "fee_deducted": 7.5,
    "processed_at": "2026-03-21T15:41:55Z"
  },
  "error": null
}
```

### Architectural Responsibilities

- **Publisher:** Acts as a **secondary port** responsible for publishing the processed callback event to RabbitMQ.
- **Consumer:** Acts as a **primary adapter** that receives queue messages and triggers downstream handling.

---

## ⚡ Performance & Caching

### GET Payment Status Caching

**Requirement:** The `GET /payments/{ticket_id}` endpoint responses are cached on a per-ticket basis to reduce database/repository queries during high-traffic periods.

**Implementation:**
- **Cache Provider:** In-memory cache (`IMemoryCache`) via `ICacheProvider` secondary port adapter
- **Cache Key Pattern:** `payment:status:{ticket}`
- **TTL (Time-To-Live):** 1 minute (60 seconds), configurable via `appsettings.json`
- **Configuration Key:** `CacheSettings:PaymentStatusTtlSeconds`

**Invalidation Strategy:**
- Cache entries are automatically evicted after the configured TTL expires
- Cache entry for a ticket is **immediately invalidated** when a `POST /payments/callback` is processed for that ticket
- `null` responses (ticket not found) are intentionally **NOT cached** to avoid pinning 404 misses

**Decorator Pattern:**
The caching behavior is implemented as a decorator (`CachedGetPaymentStatusService`) wrapping the real `GetPaymentStatusService`. This provides:
- **Transparency:** Endpoints and consumers see no difference; caching is automatic
- **Testability:** The decorator can be tested independently with stub implementations
- **Flexibility:** Cache provider can be swapped (e.g., Redis) by replacing only the adapter implementation

**Configuration Example:**

```json
{
  "CacheSettings": {
    "PaymentStatusTtlSeconds": 60
  }
}
```

To adjust TTL (e.g., 30 seconds):

```json
{
  "CacheSettings": {
    "PaymentStatusTtlSeconds": 30
  }
}
```

---

## 🧪 Integration Tests / Acceptance Criteria

### Queue Integration Coverage

The system must include integration tests validating the callback-to-queue flow.

**Required Scenarios:**

1. When a valid approved callback is received, the application publishes a message asynchronously to the integration queue.
2. When a valid rejected callback is received, the application publishes a message asynchronously to the integration queue.
3. `POST /payments/callback` must continue returning `204 No Content` without waiting for the consumer processing to complete.
4. The queue consumer must receive the published message and, in this incremental phase, emit an informational log entry.
5. For integration tests, the queue implementation must be replaced by an **in-memory queue** that preserves the same message contract used by RabbitMQ.

### Test Environment Defaults

- RabbitMQ is the production messaging target.
- An in-memory queue adapter is the default substitute for integration tests.
- The test must validate both message publication and message consumption behavior.
- The consumer side effect under test is limited to observable informational logging in this first iteration.
