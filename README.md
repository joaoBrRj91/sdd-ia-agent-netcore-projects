# Payment Service Gateway

A **high-performance, asynchronous payment processing service** built with .NET 10 that handles Credit Card and Pix transactions through an intent-callback workflow. Designed with Hexagonal Architecture and Domain-Driven Design principles.

---

## Quick Start

```bash
cd SpecStructure/development

# Restore dependencies
dotnet restore

# Run tests
dotnet test

# Run the service
dotnet run --project src/PaymentService.Api
```

The API will be available at `https://localhost:5001`.

---

## Project Overview

The Payment Service implements a **two-phase asynchronous payment flow**:

```
1. Client submits payment intent
   ↓
2. Service returns a ticket (GUID) with Pending status
   ↓
3. External payment provider processes the transaction
   ↓
4. Provider sends webhook callback with result (Approved/Rejected)
   ↓
5. Service processes callback and publishes async events
```

### Why Async?

Payment processing can take seconds to minutes. Rather than blocking the HTTP request, we return immediately with a ticket and process the result asynchronously via webhook callbacks. Clients poll `GET /payments/{ticket}` to check status.

---

## Business Rules

All payment transactions must comply with these five rules:

1. **Positive Amount** — Transaction amount must be `> 0.00` (no zero or negative payments)
2. **Allowed Currencies** — Only `BRL` (Brazilian Real) and `USD` (US Dollar) are supported
3. **Initial Status** — All new transactions start in `Pending` state
4. **Traceability** — Each payment has both:
   - `order_id` — Client's reference ID
   - `ticket` — Gateway's unique transaction ID
5. **Conditional Payment Details** — Depends on payment method:
   - **Credit Card** — Requires `card_token`
   - **Pix** — Requires `pix_key`

---

## API Endpoints

### 1. Create Payment Intent

**Request:**
```http
POST /payments
Content-Type: application/json

{
  "order_id": "ORDER-12345",
  "amount": 99.99,
  "currency": "BRL",
  "payment_method": "pix",
  "pix_key": "user@example.com",
  "customer_name": "João Silva",
  "customer_document": "12345678900"
}
```

**Response (202 Accepted):**
```json
{
  "ticket": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "Pending",
  "created_at": "2026-04-20T10:30:45.123Z"
}
```

**Alternative — Credit Card:**
```json
{
  "order_id": "ORDER-67890",
  "amount": 149.99,
  "currency": "USD",
  "payment_method": "credit_card",
  "card_token": "tok_visa_1234567890",
  "customer_name": "Jane Doe",
  "customer_document": "98765432100"
}
```

---

### 2. Process Callback Webhook

**Request** (from payment provider):
```http
POST /payments/callback
Content-Type: application/json

{
  "ticket": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "event_type": "payment.approved",
  "timestamp": "2026-04-20T10:35:12.456Z",
  "callback_data": {
    "authorization_code": "AUTH-999",
    "processing_fee": 2.50
  }
}
```

**Response:**
```
204 No Content
```

The service processes the callback asynchronously without blocking the HTTP response.

---

### 3. Get Payment Status

**Request:**
```http
GET /payments/{ticket}
```

**Response when Pending:**
```json
{
  "ticket": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "Pending",
  "event_type": "in_progress"
}
```

**Response when Approved:**
```json
{
  "ticket": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "Approved",
  "event_type": "sucessful",
  "data": {
    "order_id": "ORDER-12345",
    "amount": 99.99,
    "currency": "BRL",
    "payment_method": "pix",
    "customer_name": "João Silva"
  }
}
```

**Response when Rejected:**
```json
{
  "ticket": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "Rejected",
  "event_type": "failed",
  "error": {
    "code": "insufficient_funds",
    "message": "Customer account has insufficient funds"
  }
}
```

---

## State Machine

Every payment transitions through the following states:

```
┌─────────────┐
│   Pending   │  ← Initial state
└──────┬──────┘
       │
       ├─────────────────────────────┐
       │                             │
   [Approved]                     [Rejected]
       │                             │
       └─────────────────────────────┘
           (Terminal States)
```

Once a payment reaches `Approved` or `Rejected`, it cannot transition further. Multiple callbacks for the same ticket are safely ignored (idempotent).

---

## Architecture

### Hexagonal (Ports & Adapters) + DDD

The service follows **four-layer architecture** with strict dependency rules:

```
┌──────────────────────────────────────────────────────────┐
│  API Layer (HTTP Endpoints)                              │
│  Minimal APIs: POST /payments, POST /callback, GET /...  │
└────────────────────┬─────────────────────────────────────┘
                     │ depends on
┌────────────────────▼─────────────────────────────────────┐
│  Application Layer (Use Cases, Handlers, Services)       │
│  • CreatePaymentService, ProcessPaymentCallbackService   │
│  • CreatePaymentCommandHandler, IQueryHandler            │
│  • DTOs, Validators                                      │
│  • Uses interfaces (ICacheProvider, IPaymentRepository)  │
└────────────────────┬─────────────────────────────────────┘
                     │ depends on
┌────────────────────▼─────────────────────────────────────┐
│  Domain Layer (Pure Business Logic)                      │
│  • Payment entity, ValueObjects (Money, Currency, etc.)  │
│  • State machine, DomainEvents                           │
│  • Business rule enforcement                             │
│  NO external dependencies                                │
└──────────────────────────────────────────────────────────┘
                     ▲
                     │
┌────────────────────┴─────────────────────────────────────┐
│  Infrastructure Layer (DB, Cache, APIs)                  │
│  • InMemoryPaymentRepository                             │
│  • MemoryCacheProvider                                   │
│  • External API clients (future: RabbitMQ, EF Core)      │
└──────────────────────────────────────────────────────────┘
```

### Dependency Rule

**Dependencies flow downward only.** Domain has zero external dependencies; Application depends on Domain + interfaces; Infrastructure implements those interfaces.

### Primary Ports (Input)

- `ICommandHandler<TCommand, TResponse>` — Handles action commands (e.g., CreatePaymentCommand)
- `IQueryHandler<TQuery, TResponse>` — Handles data queries (e.g., GetPaymentStatusQuery)
- **Adapters:** REST endpoints via minimal APIs

### Secondary Ports (Output)

- `IPaymentRepository` — Payment data persistence
- `ICacheProvider` — Status caching
- `IProcessPaymentCallbackService` — Callback processing orchestration
- **Adapters:** InMemoryPaymentRepository, MemoryCacheProvider (swappable for EF Core, Redis)

---

## Key Design Decisions

### 1. Decorator Pattern for Caching

The `CachedGetPaymentStatusService` decorates `GetPaymentStatusService`:

```
GET /payments/{ticket}
         │
         ▼
CachedGetPaymentStatusService
    • Check cache (key: payment:status:{ticket})
    • If hit, return cached result
    • If miss, delegate to wrapped service
    • Cache result with 60-second TTL
    • NO caching of null (not found) responses
```

Cache is automatically invalidated on any callback for that ticket.

### 2. In-Memory Repository (Swappable)

Currently uses `ConcurrentDictionary<Guid, Payment>` for thread-safe, in-process storage. Designed to be replaced with EF Core DbContext without changing the Application layer (interface-based).

### 3. Snake Case JSON Policy

All HTTP payloads use `snake_case` naming (e.g., `card_token`, `customer_name`, `pix_key`). This is the industry standard for payment APIs.

### 4. Async Processing (Non-Blocking)

The callback endpoint returns `204 No Content` immediately. Event publishing to RabbitMQ (or in-memory queue in tests) happens asynchronously on a background thread and does not block the HTTP response.

### 5. Domain-Driven Aggregates

`Payment` is the aggregate root. It enforces all business rules:
- Amount must be > 0
- Currency must be in the whitelist
- Status transitions are guarded (Pending → Approved/Rejected only)
- All state changes raise DomainEvents for integration

---

## Project Structure

```
Demo 01/SpecStructure/
├── specs/                           ← Single Source of Truth (SSOT)
│   ├── payment-service.spec.md      ← Business rules, endpoints, integration
│   └── architecture/
│       └── architecture.backend.md  ← Architectural patterns & decision guide
│
├── prompts/                         ← AI code generation instructions
│   ├── generate-code.md             ← Code generation rules
│   └── generate-tests.md            ← Test generation rules
│
└── development/
    ├── README.md                    ← Operational quick-reference
    ├── PaymentService.slnx          ← Solution file
    │
    ├── src/PaymentService.Api/
    │   ├── Program.cs               ← DI setup, middleware config
    │   ├── Endpoints/PaymentRoutes.cs ← HTTP route handlers
    │   │
    │   ├── Domain/                  ← Pure business logic (no external deps)
    │   │   ├── Entities/Payment.cs
    │   │   ├── ValueObjects/        ← Money, Currency, PaymentMethod, etc.
    │   │   ├── Events/DomainEvents.cs
    │   │   └── Repositories/IPaymentRepository.cs
    │   │
    │   ├── Application/             ← Use cases, handlers, services
    │   │   ├── Commands/            ← CreatePaymentCommand, ProcessCallbackCommand
    │   │   ├── Handlers/            ← Command handlers
    │   │   ├── Services/            ← Business services + decorator
    │   │   ├── Cache/ICacheProvider.cs
    │   │   └── DTOs/                ← Request/response models
    │   │
    │   └── Infrastructure/          ← Adapters (persistence, cache)
    │       ├── Repositories/InMemoryPaymentRepository.cs
    │       └── Cache/MemoryCacheProvider.cs
    │
    └── tests/PaymentService.Tests/
        ├── ApiFactory.cs            ← WebApplicationFactory setup
        ├── PaymentsApiTests.cs       ← End-to-end integration tests
        ├── CreatePaymentTests.cs     ← Payment creation scenarios
        ├── ProcessPaymentCallbackTests.cs ← Callback handling
        ├── GetPaymentStatusTests.cs  ← Status queries & caching
        └── CachedGetPaymentStatusServiceTests.cs ← Decorator tests
```

---

## Running the Project

All commands run from `SpecStructure/development/`:

### Prerequisites

- **.NET 10 SDK** ([download](https://dotnet.microsoft.com/download))
- **Visual Studio Code** or **JetBrains Rider** (optional, but recommended)

### Build & Test

```bash
# Restore NuGet packages
dotnet restore

# Build solution
dotnet build

# Run all tests (xUnit)
dotnet test

# Run tests with verbose output
dotnet test -v detailed

# Run tests matching a pattern
dotnet test --filter "CreatePaymentTests"
```

### Run the Service

```bash
# Development (HTTP, verbose logging)
dotnet run --project src/PaymentService.Api --configuration Debug

# Production (HTTPS only)
dotnet run --project src/PaymentService.Api --configuration Release
```

**Default HTTPS endpoint:** `https://localhost:5001`

### Clean Build

```bash
dotnet clean
```

---

## Example: End-to-End Payment Flow

### Step 1: Create Payment Intent

```bash
curl -X POST https://localhost:5001/payments \
  -H "Content-Type: application/json" \
  -d '{
    "order_id": "ORD-001",
    "amount": 50.00,
    "currency": "BRL",
    "payment_method": "pix",
    "pix_key": "user@example.com",
    "customer_name": "Maria Silva",
    "customer_document": "12345678900"
  }'
```

**Response:**
```json
{
  "ticket": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Pending",
  "created_at": "2026-04-20T10:30:45.123Z"
}
```

### Step 2: Check Status (Still Pending)

```bash
curl -X GET https://localhost:5001/payments/550e8400-e29b-41d4-a716-446655440000
```

**Response:**
```json
{
  "ticket": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Pending",
  "event_type": "in_progress"
}
```

### Step 3: Simulate Callback (Provider Approved)

```bash
curl -X POST https://localhost:5001/payments/callback \
  -H "Content-Type: application/json" \
  -d '{
    "ticket": "550e8400-e29b-41d4-a716-446655440000",
    "event_type": "payment.approved",
    "timestamp": "2026-04-20T10:35:12.456Z",
    "callback_data": {
      "authorization_code": "AUTH-12345"
    }
  }'
```

**Response:** `204 No Content`

### Step 4: Check Final Status

```bash
curl -X GET https://localhost:5001/payments/550e8400-e29b-41d4-a716-446655440000
```

**Response:**
```json
{
  "ticket": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Approved",
  "event_type": "sucessful",
  "data": {
    "order_id": "ORD-001",
    "amount": 50.00,
    "currency": "BRL",
    "payment_method": "pix",
    "customer_name": "Maria Silva"
  }
}
```

---

## Testing

The project includes **80+ integration and unit tests** covering:

- ✅ Create payment (valid inputs, edge cases, invalid inputs)
- ✅ Callback processing (approval, rejection, idempotence, error handling)
- ✅ Status queries (pending, approved, rejected, state transitions)
- ✅ Caching behavior (TTL, invalidation, decorator pattern)
- ✅ API contract validation (request/response shapes)

**Run tests:**
```bash
cd SpecStructure/development
dotnet test
```

---

## Future Enhancements

- **EF Core Integration** — Replace in-memory repository with Entity Framework Core for durable storage
- **RabbitMQ** — Implement message publishing for cross-service event integration
- **Redis Caching** — Replace `IMemoryCache` with Redis for distributed caching
- **Advanced Validation** — FluentValidation for complex payment rules
- **Domain Events** — Implement event sourcing and event replay

---

## Architecture Reference

For detailed architecture patterns, design decisions, and anti-patterns to avoid, see `specs/architecture/architecture.backend.md`.

For complete API specification and integration requirements, see `specs/payment-service.spec.md`.

---

## License

Internal project — All rights reserved.
