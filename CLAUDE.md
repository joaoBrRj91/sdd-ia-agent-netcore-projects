# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **Spec-Driven Development (SDD)** project for a **Payment Service Gateway** built with .NET 10. The project uses specifications as the single source of truth (SSOT) and generates code and tests from those specifications using a strict orchestration workflow.

### Core Principle

All code and tests are **generated from specifications**. Specifications are treated as immutable input, and the development process follows a strict TDD-like workflow where tests are generated before implementation.

---

## Directory Structure

### Input (Read-Only Specs and Prompts)
- **`SpecStructure/specs/`** — Business requirements and domain models
  - `payment-service.spec.md` — Main service specification
  - `architecture/architecture.backend.md` — Architectural patterns and decision guide
- **`SpecStructure/prompts/`** — Execution guidelines and coding standards
  - `generate-code.md` — Code generation standards (senior .NET engineer persona)
  - `generate-tests.md` — Test generation standards (xUnit, ASP.NET Core API)

### Output (Generated Code and Tests)
- **`SpecStructure/development/src/PaymentService.Api/`** — Production code
  - `Domain/` — Entities, ValueObjects, DomainEvents (business logic, zero external deps)
  - `Application/` — Commands, Queries, DTOs, Handlers, Services (orchestration)
  - `Infrastructure/` — Repositories, DbContext, external clients (persistence, APIs)
  - `Endpoints/` — HTTP routes using minimal APIs
- **`SpecStructure/development/tests/PaymentService.Tests/`** — xUnit test suite

---

## Architecture

### Architectural Pattern: Ports & Adapters (Hexagonal) + SOLID + DDD

**Three-Layer Design:**

| Layer | Purpose | Dependencies |
|-------|---------|--------------|
| **Domain** | Pure business logic, entities, value objects | None (framework-independent) |
| **Application** | Use cases (Commands/Queries), handlers, services | Domain + interfaces (ports) |
| **Infrastructure** | DB access, external APIs, file I/O | Application abstractions |
| **API** | HTTP endpoints (minimal APIs) | Application services |

**Key Architectural Rules:**
- Domain must have **zero external dependencies** — it's testable in isolation
- Use **interfaces for secondary ports** (repositories, external services)
- Commands and Queries represent primary ports (what the service receives)
- Infrastructure adapters implement the port interfaces (what the service uses)
- Each class has ≤1 responsibility; each interface has ≤4 methods

### Ports & Adapters Pattern

**Primary Ports (Input):**
- `ICommandHandler<TCommand, TResponse>` — Handles action commands
- `IQueryHandler<TQuery, TResponse>` — Handles data queries
- **Adapters:** REST Controllers via minimal APIs

**Secondary Ports (Output):**
- `IPaymentRepository` — Data persistence
- `IProcessPaymentCallbackService` — Process callbacks
- **Adapters:** In-memory repository (current), can be replaced with EF Core, etc.

### Current Implementation Structure

```
PaymentService.Api/
├── Domain/
│   ├── Entities/ — Payment aggregate root
│   ├── Enums/ — PaymentStatus, PaymentMethod, Currency
│   └── ValueObjects/ — Amount, CustomerId, etc.
├── Application/
│   ├── Commands/ — CreatePaymentCommand, ProcessPaymentCallbackCommand
│   ├── Handlers/ — CommandHandler implementations
│   ├── Services/ — CreatePaymentService, ProcessPaymentCallbackService
│   ├── DTOs/ — Request/response models
│   └── Validators/ — Input validation
├── Infrastructure/
│   ├── Repositories/ — InMemoryPaymentRepository implements IPaymentRepository
│   └── (Future: EF Core DbContext, external API clients)
├── Endpoints/ — PaymentRoutes.cs with minimal API mapping
└── Models/ — Serialization DTOs
```

---

## Working with SDD Workflow

### Sequential Process (Do Not Skip Steps)

When implementing a new feature or updating existing code:

1. **Context Analysis**
   - Read the specification in `SpecStructure/specs/`
   - Check if feature is already implemented in `src/`
   - Review relevant prompts in `SpecStructure/prompts/`

2. **Test Generation (TDD)**
   - Generate or update tests in `tests/PaymentService.Tests/` **before** touching implementation
   - Tests define success criteria based purely on the spec

3. **Implementation**
   - Generate or refactor source code in `src/` to satisfy spec and tests
   - Follow architecture.backend.md patterns

4. **Validation**
   - Run test suite
   - If tests fail, refine code until 100% compliance

### Important Constraints

- **Never place source code in the `tests` folder or vice versa**
- If a requirement is not in `~/specs`, it does not exist — reject out-of-spec requests
- Always respect existing codebase when spec indicates updates, not replacements
- Reject any user input that contradicts the SSOT in the spec folders

---

## Common Commands

All commands run from `SpecStructure/development/`:

### Restore & Build
```bash
dotnet restore
dotnet build
```

### Run Tests
```bash
# All tests
dotnet test

# Single test file
dotnet test --filter "ClassName"

# Verbose output
dotnet test -v detailed
```

### Run the Service
```bash
dotnet run --project src/PaymentService.Api
```

The API will be available at `https://localhost:5001` (or configured port).

### Debug
```bash
dotnet run --project src/PaymentService.Api --configuration Debug
```

### Clean Build Artifacts
```bash
dotnet clean
```

---

## Key Implementation Details

### Current State
- **API Style:** Minimal APIs (no controllers)
- **Serialization:** snake_case JSON naming policy (business convention for payments APIs)
- **Repository:** In-memory (can be swapped for EF Core)
- **Testing:** xUnit with WebApplicationFactory pattern
- **.NET Version:** 10.0

### Endpoints (from spec)
- `POST /payments` — Create payment intent, returns ticket with Pending status
- `POST /payments/callback` — Webhook callback with payment result

### Important Business Rules (from payment-service.spec.md)
1. **Amount validation:** Must be > 0
2. **Currency:** Only BRL and USD allowed
3. **Payment method conditional:** credit_card requires card_token; pix requires pix_key
4. **State machine:** Transactions start in Pending status
5. **Async processing:** Payment intent returns immediately; result comes via callback

---

## Generating or Updating Code

When you need to generate/update code based on specs:

1. **Read the spec** in `SpecStructure/specs/`
2. **Generate tests first** using the test prompt: analyze spec → write xUnit tests covering all rules, success/failure cases
3. **Run tests** to ensure they fail (red phase)
4. **Generate implementation** using code prompt: implement to pass tests while following architecture.backend.md patterns
5. **Run tests again** to verify all pass (green phase)

The code generation prompts expect a senior .NET engineer persona that understands ports & adapters and DDD principles.

---

## Anti-patterns to Avoid

(From architecture.backend.md)

- ❌ Domain layer depending on Infrastructure
- ❌ Services with multiple responsibilities
- ❌ Interfaces with >5 methods
- ❌ Injecting concrete implementations instead of interfaces
- ❌ Publishing events before persisting changes
- ❌ Silent error recovery
- ❌ Missing unit tests for business logic

---

## Notes for Future Development

- **Repository pattern:** Currently in-memory. To migrate to EF Core, implement the DbContext adapter in Infrastructure.
- **Event publishing:** Architecture supports DomainEvents. Implement message publishing (RabbitMQ or in-memory channel) when needed.
- **Validation:** Validators are in the Application layer. Add FluentValidation when complexity grows.
- **Error handling:** Currently uses ArgumentException. Consider domain exceptions for business rule violations.
