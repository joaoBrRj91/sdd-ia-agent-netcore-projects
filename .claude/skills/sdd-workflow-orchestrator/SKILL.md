---
name: sdd-workflow-orchestrator
description: Automate your Spec-Driven Development (SDD) workflow. Use this when you need to generate tests and code from specifications, validate them with test runs, and get a complete status report. Invoke with `/sdd-workflow <spec-file>` (e.g., `/sdd-workflow payment-service.spec.md`) or just describe the spec you want to work on. The skill orchestrates the full cycle: read spec → generate tests → run tests (red phase) → generate implementation → run tests (green phase) → report results with clear next steps.
compatibility: Requires .NET 10 project with xUnit tests
---

# SDD Workflow Orchestrator

Automates the complete Spec-Driven Development cycle for your .NET project. This skill takes the manual repetition out of the TDD workflow by coordinating test generation, code generation, and validation in sequence.

## How It Works

The skill executes these phases in order, stopping on critical failures:

### Phase 1: Context & Spec Reading
- User provides a spec file (e.g., `payment-service.spec.md`)
- Skill reads the spec file and infers which files to generate

### Phase 2: Test Generation (Red Phase)
- Skill reads your test generation prompt from `SpecStructure/prompts/generate-tests.md`
- Prompts Claude to generate xUnit tests that cover spec requirements, edge cases, and validation rules
- Tests are written to `SpecStructure/development/tests/PaymentService.Tests/`
- **Expected outcome:** Tests exist but fail (spec is not yet implemented)

### Phase 3: Test Validation
- Runs `dotnet test` from `SpecStructure/development/`
- Reports which tests failed and why (this is normal at this stage)
- If `dotnet test` itself fails (e.g., compilation error), stops and reports the blocker

### Phase 4: Implementation Generation (Green Phase)
- Skill reads your code generation prompt from `SpecStructure/prompts/generate-code.md`
- Prompts Claude to generate implementation code based on:
  - The original spec
  - The failing tests (to understand what needs to be built)
  - Architecture patterns from `architecture.backend.md`
- Code is written to `SpecStructure/development/src/PaymentService.Api/`
- **Expected outcome:** Code exists and tests begin to pass

### Phase 5: Final Validation
- Runs `dotnet test` again
- Reports final test results (target: all tests pass)
- If tests still fail, lists which ones and suggests investigation areas

### Phase 6: Report & Next Steps
- **Success signal:** "✅ Workflow complete: X/Y tests passing"
- **File changes:** Lists which test and source files were created/modified
- **Next steps:** Recommends what to do next (e.g., "Run tests again to verify", "Review generated code for business logic correctness", or "Debug: PaymentValidator.cs validation is too strict")

## Invocation

### Slash Command (Explicit)
```
/sdd-workflow payment-service.spec.md
```

### Natural Language
```
run the SDD workflow for the payment service
I want to generate tests and code from the payment spec
work on the callback processing feature
```

The skill infers the spec file from context when possible.

## Requirements

Your project must have:
- Spec files in `SpecStructure/specs/` (e.g., `payment-service.spec.md`)
- Prompts in `SpecStructure/prompts/`:
  - `generate-tests.md` — xUnit test generation guidelines
  - `generate-code.md` — C# implementation guidelines
  - `architecture.backend.md` — Architectural patterns and anti-patterns
- A .NET 10 project with xUnit tests in `SpecStructure/development/`
- Tests runnable via `dotnet test` from the development directory

## Workflow Decisions

### Why Stop on First Failure?
Stopping early makes debugging faster. If test generation fails, there's no point generating code. If the test suite itself has compilation errors, those must be fixed before moving forward. The skill reports exactly what failed and where so you can fix it quickly and re-run.

### Why Generate Tests Before Code?
This is TDD discipline. Tests define the success criteria. When Claude sees the failing tests, it has a clear target for the implementation. This prevents "code that looks good" but doesn't satisfy the actual spec.

### What If Tests Still Fail After Code Generation?
The skill reports which tests failed and suggests investigation areas. This is normal for complex specs — the first pass of generated code may need refinement. You can then:
1. Review the failing test output
2. Refine the spec or prompts if they were ambiguous
3. Run the skill again with updated prompts

## Example Output

```
🔄 Phase 1: Reading spec and context
✅ Spec loaded: SpecStructure/specs/payment-service.spec.md (2.3 KB)

🔄 Phase 2: Generating tests from spec
✅ Generated: PaymentServiceTests.cs (487 lines)
✅ Generated: PaymentValidationTests.cs (156 lines)

🔄 Phase 3: Running test suite (Red phase)
⚠️  Tests running...
📊 Result: 12 tests, 12 failed, 0 passed (expected in red phase)

🔄 Phase 4: Generating implementation from spec + failing tests
✅ Generated: Domain/Entities/Payment.cs (143 lines)
✅ Generated: Application/Commands/CreatePaymentCommand.cs (89 lines)
✅ Generated: Application/Handlers/CreatePaymentCommandHandler.cs (156 lines)
✅ Generated: Endpoints/PaymentRoutes.cs (124 lines)

🔄 Phase 5: Running test suite (Green phase)
✅ Tests running...
📊 Result: 12 tests, 11 passed, 1 failed

❌ Workflow status: PARTIAL SUCCESS
Tests: 11/12 passing (91%)
Failed test: CreatePaymentCommandHandlerTests.ShouldRejectNegativeAmount
Reason: ValidationException expected but none thrown

🎯 Next steps:
1. Review generated validation logic in PaymentValidator.cs
2. Ensure Amount.Create() validates > 0
3. Run workflow again to confirm fix
```

## Anti-Patterns This Skill Helps Avoid

- ❌ **Writing code without tests** — Skill ensures tests are written first
- ❌ **Forgetting the spec** — Each phase references the spec file
- ❌ **Silent test failures** — Skill reports exact pass/fail counts and failure reasons
- ❌ **Architecture violations** — Generated code follows patterns from architecture.backend.md
- ❌ **Incomplete workflows** — Skill coordinates all phases sequentially

## When to Use This Skill

✅ Implementing a new feature from a spec
✅ Generating tests and code in one coordinated session
✅ Validating that a spec is implementable (if tests fail compilation, the spec may be ambiguous)
✅ Onboarding a new spec into the SDD workflow
✅ Refactoring existing code (regenerate tests + code to validate new architecture)

❌ Debugging existing code (use standard debugging tools)
❌ Writing specs themselves (use your spec writing workflow first)
❌ Reviewing generated code quality (it's good, but human review adds value)

## Customization

If you want to adjust how tests or code are generated, edit:
- `SpecStructure/prompts/generate-tests.md` — Test generation persona and rules
- `SpecStructure/prompts/generate-code.md` — Code generation persona and patterns
- `SpecStructure/specs/architecture.backend.md` — Architectural rules (automatically referenced)

The skill reads these files fresh each time, so changes take effect on the next run.
