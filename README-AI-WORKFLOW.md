# AI-Accelerated Development via Spec-Driven Development

## What is Spec-Driven Development (SDD)?

**Spec-Driven Development** is a methodology where **specifications are the single source of truth (SSOT)**, and AI is used to automatically generate tests and implementation code from those specifications.

Unlike traditional development (code-first) or even TDD (test-code together), SDD separates concerns:

```
Business Analyst / Architect
        ↓
    SPECIFICATIONS (SSOT)
        ↓ AI reads
    ┌───┴──────────────────────────┐
    ↓                              ↓
Generate Tests (Red Phase)   Generate Code (Green Phase)
        ↓                              ↓
xUnit Failures (Expected)    Implementation
        ↓                              ↓
   VALIDATION (Human Review)
        ↓
✅ Approved & Merged
```

### Key Principles

1. **Single Source of Truth** — Specifications are immutable input; all code and tests derive from them
2. **AI as Code Generator** — AI generates tests and implementation; humans validate
3. **Strict Governance** — Architecture constraints ensure generated code follows patterns
4. **Repeatability** — Same spec + same prompts = consistent, high-quality output

---

## The SDD Workflow

### Phase 1: Write Specification

The architect defines business requirements, rules, and API contracts in **clear, executable prose**:

**File:** `SpecStructure/specs/payment-service.spec.md`

```markdown
# Payment Service Specification

## Business Rules
1. Amount must be > 0.00
2. Only BRL and USD allowed
3. All transactions start in Pending status
...

## Endpoints
- POST /payments → creates intent, returns ticket
- POST /payments/callback → processes result
- GET /payments/{ticket} → retrieves status
```

### Phase 2: Generate Tests (Red)

AI reads the spec and generates comprehensive xUnit tests covering:
- ✅ All business rules
- ✅ Success paths
- ✅ Failure paths (invalid inputs, edge cases)
- ✅ Integration scenarios
- ✅ State machine transitions

**Prompt:** `SpecStructure/prompts/generate-tests.md`

```
Persona: Senior .NET Engineer
Generate xUnit tests for ASP.NET Core API covering:
- All rules from the spec
- Success and failure cases
```

**Output:** `SpecStructure/development/tests/PaymentService.Tests/`

**Status:** ❌ Tests FAIL (Red phase — expected, spec not yet implemented)

### Phase 3: Define Architecture Constraints

The architect creates an **architecture reference guide** that constrains how generated code must be structured:

**File:** `SpecStructure/specs/architecture/architecture.backend.md`

**Contents:**
- Hexagonal (Ports & Adapters) pattern
- Four-layer dependency graph (Domain → Application → Infrastructure → API)
- Primary ports (ICommandHandler, IQueryHandler) and secondary ports (IRepository, ICacheProvider)
- Anti-patterns explicitly forbidden
- Decision tree for where code belongs (Domain vs Application vs Infrastructure)

### Phase 4: Generate Implementation (Green)

AI reads the spec + architecture guide and generates production code that:
- ✅ Passes all generated tests
- ✅ Follows the architectural pattern exactly
- ✅ Implements all business rules
- ✅ Adheres to SOLID principles

**Prompt:** `SpecStructure/prompts/generate-code.md`

```
Persona: Senior .NET Engineer
Generate code following ~/specs/architecture.backend.md:
- Cover all rules from the spec
- Include success and failure cases
```

**Output:** `SpecStructure/development/src/PaymentService.Api/`

**Status:** ✅ Tests PASS (Green phase — all tests satisfied)

### Phase 5: Validate & Review

Human review ensures:
- ✅ Code is correct and idiomatic
- ✅ No security vulnerabilities
- ✅ Architecture is respected
- ✅ Business logic is faithful to spec
- ✅ Tests are meaningful

**Output:** Approved commit to main branch

### Phase 6: Iterate (When Spec Changes)

When new features are added:

1. **Update spec** — Add new requirement or rule
2. **Re-run workflow** — Start at Phase 2 (test generation)
3. **Regenerate + Validate** — New tests, new implementation
4. **Result** — Existing tests still pass; new tests now pass too

---

## Project Artifacts

### Input: Specifications (Read-Only SSOT)

| File | Purpose |
|------|---------|
| `specs/payment-service.spec.md` | Business rules, endpoints, messaging requirements |
| `specs/architecture/architecture.backend.md` | Architectural patterns, layer definitions, anti-patterns |
| `prompts/generate-tests.md` | Persona & constraints for test generation |
| `prompts/generate-code.md` | Persona & constraints for code generation |

### Output: Generated Code & Tests

| Directory | Purpose |
|-----------|---------|
| `development/src/PaymentService.Api/` | Production code (Domain, Application, Infrastructure, API layers) |
| `development/tests/PaymentService.Tests/` | xUnit test suite (80+ tests) |

### Orchestration: Workflow Automation

| File | Purpose |
|------|---------|
| `orquestration.spec.md` | SDD workflow persona & orchestration steps |
| `.claude/skills/sdd-workflow-orchestrator/` | Claude Code skill that automates the workflow |

---

## The Orchestrator: Automating SDD

### What is the Orchestrator?

The **SDD Orchestrator** is a Claude Code **skill** (automated agent) that orchestrates the entire workflow:

```
User Command:
  /sdd-workflow payment-service.spec.md
         ↓
SDD Orchestrator (skill)
  1. Read spec
  2. Extract business rules
  3. Generate tests (call AI)
  4. Run tests (expect failures)
  5. Generate implementation (call AI)
  6. Run tests (expect passes)
  7. Report status
         ↓
✅ Complete workflow in one command
```

### How to Invoke

```bash
/sdd-workflow SpecStructure/specs/payment-service.spec.md
```

**Or** (from VS Code):

1. Open command palette: `Ctrl+Shift+P` (Windows) or `Cmd+Shift+P` (Mac)
2. Type: `SDD Workflow`
3. Select spec file
4. Watch orchestration complete

### What It Does

1. **Read Specification** — Extracts all business requirements
2. **Generate Tests** — Calls AI to create comprehensive xUnit tests
3. **Run Tests** — Executes test suite (expect ~40-50% failures on first run)
4. **Generate Implementation** — Calls AI to create production code
5. **Run Tests Again** — Verifies all tests pass
6. **Report** — Displays:
   - Number of tests generated
   - Test coverage summary
   - Code quality metrics
   - Pass/fail status
   - Time elapsed

### When to Use

- **New feature implementation** — Add to spec, run orchestrator
- **Spec updates** — Change spec, re-run orchestrator
- **Verification** — Ensure all tests pass after changes
- **Documentation** — Review generated tests as living documentation

---

## Prompt Design: How AI Learns What to Generate

### Test Generation Prompt (`generate-tests.md`)

**Key elements:**

```
Persona: Senior .NET Engineer with 10+ years C# experience

Task: Generate comprehensive xUnit tests for ASP.NET Core API

Requirements:
- Cover ALL business rules from the spec
- Include success paths AND failure paths
- Use clear test names (Arrange-Act-Assert pattern)
- Use [Fact] for single scenarios, [Theory]+[InlineData] for parameterized
- Assume WebApplicationFactory<Program> is available
- Output: Test file only (no implementation)
```

**Why it works:**
- Persona sets the tone (senior engineer, not junior)
- Explicit "ALL business rules" = comprehensive coverage
- Specific frameworks (xUnit, WebApplicationFactory) = consistent test style
- Assumption about available test infrastructure = realistic tests

### Code Generation Prompt (`generate-code.md`)

**Key elements:**

```
Persona: Senior .NET Architect with deep DDD and Hexagonal Architecture knowledge

Task: Generate production code to pass the tests and satisfy the spec

Requirements:
- Follow ~/specs/architecture.backend.md EXACTLY
- Implement all business rules from the spec
- Use Domain-Driven Design (entities, value objects, aggregates)
- Strictly separate Domain (no external deps) from Application from Infrastructure
- Include comprehensive error handling
- Output: Production code only (no tests)
```

**Why it works:**
- Persona sets architectural expectations (architect-level thinking)
- Reference to `architecture.backend.md` = guardrails on design
- DDD + Hexagonal = style constraints
- "EXACTLY" = no shortcuts or deviations

### Architecture Reference Guide (`architecture.backend.md`)

Acts as a **constraint decoder** for AI:

```
┌─ Domain (zero external dependencies)
│  ├─ Entities (e.g., Payment aggregate)
│  ├─ ValueObjects (e.g., Money, Currency)
│  └─ DomainEvents (e.g., PaymentApprovedEvent)
│
├─ Application (Domain + interfaces)
│  ├─ Commands (CreatePaymentCommand)
│  ├─ Handlers (CommandHandler<CreatePaymentCommand>)
│  ├─ Services (CreatePaymentService)
│  └─ DTOs (Request/response models)
│
├─ Infrastructure (concrete implementations)
│  ├─ Repositories (InMemoryPaymentRepository)
│  ├─ Caching (MemoryCacheProvider)
│  └─ External APIs (future)
│
└─ API (HTTP endpoints)
   └─ Minimal APIs (PaymentRoutes.cs)
```

When AI reads this, it understands:
- Where each class belongs
- What each layer can depend on
- What interfaces to create
- What to forbid (circular deps, violating layer boundaries)

---

## What AI Does vs What Humans Do

### AI Generates

✅ **Tests** — Complete xUnit test suites with:
- Meaningful test names
- Arrange-Act-Assert pattern
- Edge case scenarios
- Integration test setup

✅ **Production Code** — Full implementation with:
- Correct architecture
- SOLID principles
- Business rule enforcement
- Error handling

✅ **Consistency** — Same spec + same prompts = predictable, uniform code style across the entire codebase

### Humans Decide

✅ **Specifications** — What should the system do? What are the business rules?

✅ **Architecture** — How should the code be organized? What patterns to enforce?

✅ **Validation** — Is the generated code correct? Does it match the intent? Any security concerns?

✅ **Iteration** — When spec changes or issues arise, humans update the spec; AI regenerates

### Collaboration Pattern

```
Human                          AI
  │
  ├─ Write spec ──────────────>
  │                              ├─ Generate tests
  │                              ├─ Generate code
  │                              └─ Run tests
  │                                  ↓
  │ <────────── Report status ──────┘
  │
  ├─ Review output
  ├─ Validate against spec
  └─ Approve / suggest changes
         │
         └──> (If changes: update spec, loop)
```

---

## Benefits Observed in This Project

### 1. Complete Hexagonal Architecture

The entire Payment Service is a **textbook example** of Hexagonal Architecture:
- Domain layer has **zero external dependencies** ✅
- Application layer uses **only interfaces** ✅
- Infrastructure adapters **implement interfaces** ✅
- API layer **depends only on Application** ✅

This wasn't enforced by linting — it emerged naturally from:
1. Architecture guide clarity (humans wrote it)
2. AI's adherence to it (AI read and followed it)
3. Test validation (if architecture was violated, tests would fail)

### 2. Full Test Coverage

**80+ integration and unit tests** covering:
- Every business rule ✅
- Every endpoint ✅
- Happy paths and failure modes ✅
- State machine transitions ✅
- Caching behavior ✅

Generated in **minutes**, not hours. Tests serve as **living documentation** of the spec.

### 3. Strict Spec Fidelity

Every feature in `payment-service.spec.md` has corresponding:
- Test case(s) ✅
- Implementation code ✅
- Domain validation ✅

Changes to the spec automatically trigger test updates and code regeneration. **Zero spec-code divergence.**

### 4. Reduced Defects

Because:
- Tests are generated BEFORE implementation (TDD discipline)
- Architecture is enforced (no ad-hoc decisions)
- Human review catches conceptual errors before large refactors
- Spec is immutable (no "I thought we agreed to that")

### 5. Faster Development

- **Spec write time:** 1–2 hours (human architect)
- **Test generation:** 5 minutes (AI)
- **Code generation:** 10 minutes (AI)
- **Validation + review:** 15 minutes (human)
- **Total:** ~2 hours for a complete feature

Traditional TDD: 4–6 hours. **2x faster.**

### 6. Knowledge Transfer

New team members can:
1. Read the spec (high-level intent)
2. Read the architecture guide (how code is organized)
3. Read the tests (examples of every behavior)
4. Understand the code (it follows the architecture exactly)

No need to reverse-engineer intent from code.

---

## Workflow in Action: Payment Service Example

### Step 1: Specification Written (Human)

**File:** `specs/payment-service.spec.md`

```markdown
## Business Rules
- Amount must be > 0
- Only BRL and USD
- Transactions start in Pending status
- Credit card requires card_token
- Pix requires pix_key

## Endpoints
- POST /payments → returns { ticket, status: Pending }
- POST /payments/callback → processes result
- GET /payments/{ticket} → returns status
```

### Step 2: Run Orchestrator (Automated)

```bash
/sdd-workflow specs/payment-service.spec.md
```

**AI Activity:**

1. **Parse spec** → Extract 5 business rules + 3 endpoints
2. **Generate tests:**
   ```csharp
   [Fact]
   public async Task CreatePayment_WithValidPix_ReturnsTicket()
   { ... }

   [Fact]
   public async Task CreatePayment_WithNegativeAmount_Returns400()
   { ... }

   [Theory]
   [InlineData(0)]
   [InlineData(-10.5)]
   public async Task CreatePayment_InvalidAmount_Returns400(decimal amount)
   { ... }
   ```

3. **Run tests** → ❌ FAIL (red phase)

4. **Generate code:**
   ```csharp
   // Domain/Entities/Payment.cs
   public class Payment {
       public static Payment Create(Guid ticket, decimal amount, Currency currency, ...) {
           if (amount <= 0) throw new ArgumentException("Amount must be > 0");
           if (!Currency.IsValid(currency)) throw new ArgumentException(...);
           return new Payment { Status = PaymentStatus.Pending, ... };
       }
   }

   // Application/Services/CreatePaymentService.cs
   public class CreatePaymentService {
       public async Task<CreatePaymentResponseDto> Execute(CreatePaymentRequest request) {
           Validate(request);
           var payment = Payment.Create(...);
           await _repository.SaveAsync(payment);
           return new CreatePaymentResponseDto { Ticket = payment.Ticket, Status = PaymentStatus.Pending };
       }
   }
   ```

5. **Run tests** → ✅ PASS (green phase)

### Step 3: Review (Human)

Developer checks:
- ✅ All tests pass
- ✅ Code follows architecture
- ✅ Business rules enforced in Domain layer
- ✅ No security issues
- ✅ Idiomatic C#

### Step 4: Merge

```bash
git commit -m "feat: payment service with create, callback, status endpoints"
git push origin main
```

---

## Scaling SDD: Multiple Features

As more features are added, the workflow repeats:

| Sprint | Spec Changes | Result |
|--------|---|---|
| 1 | Create payment intent, process callback | Payment service working |
| 2 | Add GET /payments status, implement caching | Service queryable, performant |
| 3 | Add RabbitMQ integration, event publishing | Async events flowing |
| 4 | Add EF Core persistence, migration scripts | Production-ready database |

Each sprint:
1. **Architect updates spec** — new requirements or rules
2. **Run `/sdd-workflow`** — auto-generates tests + code
3. **Review + validate** — human sign-off
4. **Merge** — new tests + code in production

**Same workflow.** **No manual code generation.** **Consistent architecture.** **High quality.**

---

## Anti-Patterns: What SDD Prevents

### ❌ Spec-Code Divergence

*Without SDD:* Spec says "amount > 0" but code allows zero. Tests don't catch it because they're written to match the code, not the spec.

*With SDD:* Tests are generated from spec first. Spec = truth. Code must match.

### ❌ Architectural Drift

*Without SDD:* Early code follows hexagonal architecture. As developers add features under time pressure, they bypass it. Eventually, Domain depends on Infrastructure. Circular dependencies. Brittle code.

*With SDD:* Prompts enforce architecture. Any deviation in generated code is caught in code review.

### ❌ Inconsistent Code Style

*Without SDD:* 10 developers, 10 coding styles. One uses interfaces, another uses base classes. One layer is packed with 15 methods, another is minimal.

*With SDD:* Same prompt = same style. All generated code is consistent.

### ❌ Silent Failures

*Without SDD:* Code is written, tests are skipped under deadline pressure. Bugs hide until production.

*With SDD:* Tests are **generated before code**. Red phase is unavoidable. Tests MUST pass before merge.

---

## Getting Started with SDD in Your Project

### 1. Write a Spec

Create `specs/my-feature.spec.md`:

```markdown
# Feature Specification

## Business Rules
- Rule 1
- Rule 2

## Endpoints
- GET /feature → returns...
- POST /feature → creates...

## Data Model
- Entity with fields X, Y, Z
```

### 2. Define Architecture

Create or update `specs/architecture/architecture.backend.md` with your layer structure.

### 3. Write Prompts

Create `prompts/generate-tests.md` and `prompts/generate-code.md` with:
- Persona (senior engineer)
- Constraints (architecture, frameworks, patterns)
- Output expectations (test vs code)

### 4. Run Orchestrator

```bash
/sdd-workflow specs/my-feature.spec.md
```

### 5. Review & Validate

Verify generated code:
- ✅ All tests pass
- ✅ Architecture respected
- ✅ Business logic correct
- ✅ No security issues

### 6. Commit

```bash
git add -A
git commit -m "feat: my feature (generated via SDD)"
```

---

## Tools & Technology

### Claude Code

**CLI tool** for running AI workflows:
- `/plan` — Start planning mode
- `/sdd-workflow <spec>` — Run SDD orchestrator
- `/fast` — Toggle fast mode (same model, faster output)

### SDD Orchestrator Skill

Located at `.claude/skills/sdd-workflow-orchestrator/`

Automates:
1. Spec parsing
2. Test generation
3. Test execution
4. Code generation
5. Final verification
6. Status reporting

### Specs as Configuration

Specifications are **executable configuration**. They drive:
- Test generation
- Code generation
- Architectural enforcement
- CI/CD validation

No separate design docs. No PowerPoint. Spec IS the design.

---

## Common Questions

### Q: Does AI-generated code need review?

**A:** Yes. AI generates **candidate code** from specs. Humans validate:
- Correctness (does it match intent?)
- Security (any vulnerabilities?)
- Performance (any bottlenecks?)
- Idiomaticity (does it fit the codebase style?)

Review is faster than writing code from scratch because you're validating, not creating.

### Q: What if the spec is wrong?

**A:** Update the spec. Re-run `/sdd-workflow`. AI regenerates tests + code. The workflow is designed for iteration.

### Q: Can I manually edit generated code?

**A:** Yes, but **not recommended**. Once you manually edit, you have two sources of truth (spec + manual edits). They will diverge.

**Better:** Update the spec, re-run the workflow, review the new generation.

### Q: How does this scale to large teams?

**A:** Specs become contracts. Different teams can:
- Build different features independently (specs ensure interfaces match)
- Review each other's specs (avoid over-specification)
- Reuse architectural patterns (guardrails prevent drift)

SDD enforces **organizational coherence**.

### Q: What if AI generates bad code?

**A:** Prompts are the control. If AI generates bad code:
1. **Review the prompt** — Is it clear? Specific? Enforced enough?
2. **Update the prompt** — Add constraints, examples, anti-patterns
3. **Re-run** — AI generates better code with updated prompt
4. **Iterate** — Prompts improve over time

Prompts are your specification language for code style.

---

## Further Reading

- **Specification:** `SpecStructure/specs/payment-service.spec.md`
- **Architecture Guide:** `SpecStructure/specs/architecture/architecture.backend.md`
- **Orchestrator Details:** `SpecStructure/orquestration.spec.md`
- **Test Examples:** `SpecStructure/development/tests/PaymentService.Tests/`
- **Generated Code:** `SpecStructure/development/src/PaymentService.Api/`

---

## Summary

**Spec-Driven Development** is a paradigm shift:

| Aspect | Traditional | SDD |
|--------|---|---|
| Source of Truth | Code (implicit) | Specs (explicit) |
| Test-First | Optional, often skipped | Mandatory (spec → tests first) |
| Code Generation | Manual | AI (guided by prompts) |
| Consistency | Variable (per developer) | Enforced (per architecture + prompts) |
| Spec-Code Sync | Drifts over time | Automatic (regenerate on change) |
| Scaling | Harder (more developers = more inconsistency) | Easier (prompts enforce uniformity) |

**Result:** Better code, faster delivery, reduced defects, improved knowledge transfer.

Welcome to the future of software engineering. 🚀
