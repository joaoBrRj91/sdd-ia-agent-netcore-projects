# .NET Architecture - Optimized Summary for AI

> **IMPORTANT SYSTEMS:**
>
> This specification is intended to be used as a **foundation for code development**.
> It **MUST NOT be replicated verbatim** before analyzing whether this architectural approach should be followed for the specific project.

## Key Rules

### STOP BEFORE CODE GENERATION

**Before generating ANY code, the AI MUST:**

1. **ANALYZE** the project requirements and constraints
2. **EVALUATE** whether each architectural pattern is appropriate
3. **DETERMINE** which elements should be applied
4. **DOCUMENT** decisions and adaptations
5. **THEN** generate code aligned with the actual needs

## Essence

**Pattern**: Ports & Adapters (Hexagonal) + SOLID + DDD  
**Goal**: Testable code, framework-independent, with pure business logic

---

## Three Layers

| Layer              | What It Is                                  | Depends On            |
| ------------------ | ------------------------------------------- | --------------------- |
| **Domain**         | Business logic, Entities, ValueObjects      | NOTHING               |
| **Application**    | Use Cases (Commands/Queries), Orchestration | Domain + Abstractions |
| **Infrastructure** | DB, Cache, Queues, APIs, Email              | Application           |
| **API**            | HTTP/gRPC Controllers                       | Application           |

---

## Ports & Adapters

### Primary Ports (Input)

**What the service receives:**

- `ICommandHandler<T>` → Executes action
- `IQueryHandler<Q,R>` → Returns data
- `IEventHandler<T>` → Reacts to event

**Adapters**: REST Controllers, gRPC, Message Consumers, WebSockets

### Secondary Ports (Output)

**What the service needs:**

- `IRepository<T>` → Persistence
- `ICacheProvider` → Distributed cache
- `IMessagePublisher` → Publish events
- `IExternalApiClient` → Call APIs
- `IEmailService` → Notifications

**Adapters**: EF Core, Redis, RabbitMQ, HttpClient, SendGrid

---

## Structure per Service

```
ServiceName/
├── Domain/              ← No deps. Entities, ValueObjects, DomainEvents
├── Application/         ← Commands, Queries, DTOs, Validators, Abstractions
├── Infrastructure/      ← Repositories, DbContext, ExternalClients, Config
├── API/                 ← Controllers, Filters, Middlewares, Program.cs
└── Tests/               ← Unit + Integration
```

### Standard Naming Conventions

- Aggregates: `OrderAggregate`
- Events: `OrderCreatedEvent`
- Commands: `CreateOrderCommand` + `CreateOrderCommandHandler`
- Queries: `GetOrderByIdQuery` + `GetOrderByIdQueryHandler`
- Repositories: `OrderRepository`
- DTOs: `OrderCreateDto`, `OrderResponseDto`

---

## Messaging

### IChannel (In-Memory)

**Use for**: Non-critical notifications, cache, logs, recommendations  
**Characteristics**: Fire & forget, no persistence, losses OK, minimal latency

### RabbitMQ (Broker)

**Use for**: Critical events, multi-service flows, audit trail  
**Characteristics**: Durable, replay, distributed, confirmation

---

## Implementation Checklist

- [ ] Domain **without** external dependencies
- [ ] Application with abstract interfaces (Ports)
- [ ] Infrastructure implements interfaces
- [ ] Constructor injection in all classes
- [ ] Each class has ≤1 responsibility
- [ ] Interfaces ≤4 methods
- [ ] Events published after persistence
- [ ] Validators before command execution
- [ ] Test Domain isolated, Application with mocks, Integration with real DB
- [ ] Middleware for errors, logging, correlation ID

---

## Decision: Monolith vs Microservice

**Modular Monolith**: < 100k lines, ≤3 teams, weekly deploy  
**Microservice**: > 100k lines, > 3 teams, daily deploy, multiple contexts

---

## Decision Guide for Generated Code

1. **What's the input?** → Create Primary Port (`ICommandHandler`, `IQueryHandler`)
2. **What's the output?** → Create Secondary Port (`IRepository`, `IService`)
3. **Has business rule?** → Place in Domain (Entity, ValueObject)
4. **Has orchestration?** → Place in ApplicationService
5. **Is low-level technical implementation like database or cache?** → Place in Infrastructure
6. **Needs validation?** → Validator or Command first
7. **Important event?** → DomainEvent + Publish after persistence
8. **Needs data from another source?** → Create Port and Adapter

---

## Anti-patterns to Avoid

❌ Domain depending on Infrastructure  
❌ Giant service with multiple responsibilities  
❌ Interface with >5 methods  
❌ Without abstractions (injecting concrete implementation)  
❌ Publishing event before persisting  
❌ Silent error recovery  
❌ Without unit tests for business logic
