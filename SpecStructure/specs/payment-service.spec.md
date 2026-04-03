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
4. **Notification:** The final result (Success/Failure) is pushed to the client via a **Webhook Callback**.

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
**Description:** Out-of-band notification sent from the Gateway to the Client.

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
