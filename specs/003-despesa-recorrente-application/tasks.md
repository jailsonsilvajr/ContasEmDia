---

description: "Task list for Application de Despesa Recorrente (Cadastro)"
---

# Tasks: Application de Despesa Recorrente (Cadastro)

**Input**: Design documents from `/specs/003-despesa-recorrente-application/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/application-public-api.md, quickstart.md

**Tests**: Required by this project's constitution (Principle II — Test-First Development). `Application.Tests` (xUnit) tasks are written before the implementation task they verify, following Red-Green-Refactor.

**Organization**: Tasks are grouped by user story. This feature has exactly one Use Case (`CreateRecurringExpenseUseCase`) whose three User Stories are three **outcomes** of the same `ExecuteAsync` call (success, validation failure, unexpected failure), not three separable code paths — so the full implementation is delivered in User Story 1 (needed to make the MVP happy path pass at all), and User Stories 2/3 add the test coverage — and, where a genuine gap remains, the small implementation delta — that locks in their specific outcome. Each phase is still independently testable per its Independent Test criterion in spec.md.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Paths are relative to the repository root (`backend/...`)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add the two new sibling projects (`Application`, `Application.Tests`) to the existing `backend/ContasEmDia.sln`, matching the `Domain`/`Domain.Tests` pattern.

- [X] T001 [P] Create `backend/Application/ContasEmDia.Application.csproj` (`TargetFramework net10.0`, `ImplicitUsings enable`, `Nullable enable`, `TreatWarningsAsErrors true`, no package references), matching `backend/Domain/ContasEmDia.Domain.csproj`
- [X] T002 [P] Create `backend/Application.Tests/ContasEmDia.Application.Tests.csproj` (xUnit, `coverlet.collector`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `Using Include="Xunit"`, `IsPackable false`), matching `backend/Domain.Tests/ContasEmDia.Domain.Tests.csproj`, with a `ProjectReference` to `../Application/ContasEmDia.Application.csproj`
- [X] T003 Add `Application` and `Application.Tests` projects to `backend/ContasEmDia.sln` (depends on T001, T002)

**Checkpoint**: `dotnet build backend/ContasEmDia.sln` succeeds with two new empty projects.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The two Domain-touching structural prerequisites decided in the spec clarification (FR-015, FR-016) plus the new Application port (FR-007). **No User Story implementation can begin until this phase is complete** — the Use Case constructor depends on `IRepositoryManager` and `ICurrentDateProvider`, and FR-006/SC-003 require the Domain messages to already be in PT-BR before the Use Case can repass them unmodified.

- [X] T004 Create `IRepositoryManager` interface in `backend/Domain/Repositories/IRepositoryManager.cs`, exposing `IRecurringExpenseRepository RecurringExpenseRepository { get; }` (FR-015)
- [X] T005 Modify `backend/Infrastructure/RepositoryManager.cs` so the existing `RepositoryManager` class implements `ContasEmDia.Domain.Repositories.IRepositoryManager` (signature-only change; internal `Lazy<T>` behavior unchanged) (depends on T004) (FR-015, Principle VII)
- [X] T006 [P] Revise the PT-BR error message in `backend/Domain/ValueObjects/ExpenseName.cs` (currently "Expense name must not be null, empty, or whitespace-only.") — text only, no condition/exception-type change (FR-016)
- [X] T007 [P] Revise the PT-BR error message in `backend/Domain/ValueObjects/ExpenseCategory.cs` (currently "Expense category must be a defined category value.") — text only (FR-016)
- [X] T008 [P] Revise both PT-BR error messages in `backend/Domain/ValueObjects/Money.cs` (currently "Money value must be greater than zero." and "Money value must have at most two decimal places.") — text only (FR-016)
- [X] T009 [P] Revise the PT-BR error message in `backend/Domain/ValueObjects/DueDay.cs` (currently "Due day must be between 1 and 31.") — text only (FR-016)
- [X] T010 [P] Revise the PT-BR error message in `backend/Domain/ValueObjects/Frequency.cs` (currently "Only monthly frequency is supported in this phase.") — text only (FR-016)
- [X] T011 [P] Revise the PT-BR error message in `backend/Domain/ValueObjects/RecurringExpenseStatus.cs` (currently "Recurring expense status must be a defined status value.") — text only (FR-016)
- [X] T012 [P] Revise the PT-BR error message in `backend/Domain/ValueObjects/OccurrenceStatus.cs` (currently "Occurrence status must be a defined status value.") — text only, for consistency even though this Use Case does not exercise it (FR-016)
- [X] T013 [P] Revise both PT-BR error messages in `backend/Domain/ValueObjects/ReferencePeriod.cs` (currently "Month must be between 1 and 12." and "Year must be greater than zero.") — text only, for consistency even though this Use Case only constructs `ReferencePeriod` internally via `FromDate` (FR-016)
- [X] T014 [P] Create `ICurrentDateProvider` port in `backend/Application/Ports/ICurrentDateProvider.cs` with a single member `DateOnly GetCurrentDate();` (depends on T001) (FR-007)

**Checkpoint**: `dotnet build backend/ContasEmDia.sln` succeeds; `RepositoryManager` implements `IRepositoryManager`; all Domain validation messages are in PT-BR; `Domain.Tests` still passes unmodified (no test in that project asserts on exact message text). User Story implementation can now begin.

---

## Phase 3: User Story 1 - Cadastrar despesa recorrente com dados válidos (Priority: P1) 🎯 MVP

**Goal**: Given valid input equivalent to the "Nova despesa recorrente" body, the Use Case converts every field, derives the current competência via `ICurrentDateProvider`, creates `RecurringExpense` through its public constructor, persists it via `IRepositoryManager`, and returns a success Output built exclusively from the aggregate's/entity's read methods — including the generated occurrence when applicable, or an empty occurrence list for Paused/future-start expenses.

**Independent Test**: Execute the Use Case with data for an Active expense starting in the current competência, using test doubles for the repository and the current-date port, and verify the returned Output faithfully reflects what the Domain created (including the generated occurrence) and that persistence (`AddAsync`) was invoked.

### Tests for User Story 1

> Write these tests first; they must fail (skeleton throws `NotImplementedException`) before T026 is implemented.

- [X] T015 [P] [US1] Create `CreateRecurringExpenseUseCaseInput` in `backend/Application/UseCases/CreateRecurringExpense/CreateRecurringExpenseUseCaseInput.cs` per `contracts/application-public-api.md` (7 required primitive fields + optional `Note`)
- [X] T016 [P] [US1] Create `FieldError` record and `OccurrenceData` record + `CreateRecurringExpenseUseCaseOutput` class (with `Success(...)`/`Failure(...)` factory methods) in `backend/Application/UseCases/CreateRecurringExpense/CreateRecurringExpenseUseCaseOutput.cs` per `contracts/application-public-api.md` and `data-model.md`
- [X] T017 [P] [US1] Create `ICreateRecurringExpenseUseCase` interface in `backend/Application/UseCases/CreateRecurringExpense/ICreateRecurringExpenseUseCase.cs` with `Task<CreateRecurringExpenseUseCaseOutput> ExecuteAsync(CreateRecurringExpenseUseCaseInput input);`
- [X] T018 [US1] Create `CreateRecurringExpenseUseCase` skeleton in `backend/Application/UseCases/CreateRecurringExpense/CreateRecurringExpenseUseCase.cs`: constructor taking `IRepositoryManager` and `ICurrentDateProvider`, `ExecuteAsync` throwing `NotImplementedException` (depends on T004, T014, T015, T016, T017)
- [X] T019 [P] [US1] Create `FixedCurrentDateProvider` test double (implements `ICurrentDateProvider`, returns a fixed `DateOnly` set via constructor) in `backend/Application.Tests/UseCases/CreateRecurringExpense/FixedCurrentDateProvider.cs`
- [X] T020 [US1] Create `InMemoryRecurringExpenseRepository` test double (implements `IRecurringExpenseRepository`, same pattern as `Domain.Tests/Repositories/InMemoryRecurringExpenseRepository.cs`) in `backend/Application.Tests/UseCases/CreateRecurringExpense/InMemoryRecurringExpenseRepository.cs`
- [X] T021 [US1] Create `FakeRepositoryManager` test double (implements `IRepositoryManager`, takes an `IRecurringExpenseRepository` via constructor so it can wrap either the in-memory or a throwing repository) in `backend/Application.Tests/UseCases/CreateRecurringExpense/FakeRepositoryManager.cs` (depends on T020)
- [X] T022 [US1] Write `CreateRecurringExpenseUseCaseTests` happy-path tests in `backend/Application.Tests/UseCases/CreateRecurringExpense/CreateRecurringExpenseUseCaseTests.cs` covering Acceptance Scenarios 1–3: (a) Active expense starting in the current competência → success Output with the generated occurrence and `AddAsync` invoked; (b) Paused expense, or Active with a future start competência → success Output with an empty occurrence list; (c) every success field is asserted against the created aggregate's/occurrence's own read methods (never re-derived independently) (depends on T018, T019, T021)

### Implementation for User Story 1

- [X] T023 [US1] Implement `CreateRecurringExpenseUseCase.ExecuteAsync` in `backend/Application/UseCases/CreateRecurringExpense/CreateRecurringExpenseUseCase.cs`: convert each of the 7 required fields inside its own try/catch around the corresponding Value Object constructor, accumulating a `FieldError` (field name + the caught exception's PT-BR `Message`, repassed verbatim) without short-circuiting (FR-002, FR-005, FR-006); guard `Category`/`Frequency`/`Status` with an explicit `Enum.TryParse` boolean check before constructing `ExpenseCategory`/`Frequency`/`RecurringExpenseStatus` (research.md §4, FR-004); guard `StartDate` with `DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out value)` before constructing `CalendarDate` (research.md §5, FR-003); `Note` always converts successfully; if any `FieldError` was accumulated, return `Output.Failure(errors)` without calling `ICurrentDateProvider` or the repository; otherwise call `currentDateProvider.GetCurrentDate()`, derive `ReferencePeriod.FromDate(...)` (FR-007), construct `RecurringExpense` exclusively via its public constructor (FR-008), call `repositoryManager.RecurringExpenseRepository.AddAsync(...)` letting any non-validation exception propagate uncaught (FR-009, FR-012), and build `Output.Success(...)` exclusively from the created aggregate's/occurrence's read methods (FR-010, FR-011) (depends on T023's own tests failing first — i.e. depends on T022)

**Checkpoint**: T022's happy-path tests pass. `CreateRecurringExpenseUseCase` is fully implemented — User Story 1 (MVP) is functional and independently testable.

---

## Phase 4: User Story 2 - Rejeitar cadastro com dados inválidos, agregando todos os erros (Priority: P2)

**Goal**: Given input with one or more invalid fields, the Use Case reports every invalid field's PT-BR message (as provided by the Domain) in a single failure Output, without persisting anything. The aggregation mechanism itself is already delivered by T023; this phase adds the dedicated test coverage that locks in each of User Story 2's acceptance scenarios and edge cases.

**Independent Test**: Execute the Use Case with input containing more than one invalid field simultaneously (e.g. empty name and unknown category) and verify the failure Output carries one error per invalid field, each with the Domain-provided PT-BR message, and that no expense was persisted.

### Tests for User Story 2

- [X] T024 [US2] Add test to `CreateRecurringExpenseUseCaseTests.cs`: exactly one invalid field (e.g. empty `Name`) → a single `FieldError` with the field name and the PT-BR message thrown by `ExpenseName`, and `AddAsync` never invoked (Acceptance Scenario 1)
- [X] T025 [US2] Add test to `CreateRecurringExpenseUseCaseTests.cs`: more than one invalid field simultaneously (e.g. empty `Name` and unknown `Category`) → one `FieldError` per invalid field in the same failure Output, and `AddAsync` never invoked (Acceptance Scenario 2)
- [X] T026 [US2] Add test to `CreateRecurringExpenseUseCaseTests.cs`: `Category` value outside the 5 supported categories → a `FieldError` for `category` is reported without ever constructing `ExpenseCategory` (verifies the `Enum.TryParse` guard from research.md §4, not the fallback `Enum.IsDefined` trap) (Acceptance Scenario 3, edge case)
- [X] T027 [US2] Add test to `CreateRecurringExpenseUseCaseTests.cs`: malformed `StartDate` text and a calendarially-inexistent date (e.g. `"2026-02-31"`) → a `FieldError` for `startDate` in both cases (Acceptance Scenario 4, edge case)

**Checkpoint**: All User Story 2 tests pass against the existing T023 implementation (no additional production code expected; if any of T024–T027 fails, fix the corresponding guard in `CreateRecurringExpenseUseCase.ExecuteAsync` before proceeding). User Stories 1 and 2 both work independently.

---

## Phase 5: User Story 3 - Distinguir falha de validação de falha inesperada (Priority: P3)

**Goal**: A persistence failure unrelated to business-rule validation (e.g. database unavailable) propagates as an exception through `ExecuteAsync`, never converted into a validation-failure Output.

**Independent Test**: Substitute the repository with a test double that throws on `AddAsync`, execute the Use Case with valid input, and verify the exception propagates without being converted into a failure Output.

### Tests for User Story 3

- [X] T028 [US3] Create `ThrowingRecurringExpenseRepository` test double (implements `IRecurringExpenseRepository`, `AddAsync` throws e.g. `InvalidOperationException` simulating database unavailability) in `backend/Application.Tests/UseCases/CreateRecurringExpense/ThrowingRecurringExpenseRepository.cs`
- [X] T029 [US3] Add test to `CreateRecurringExpenseUseCaseTests.cs`: with valid input and a `FakeRepositoryManager` wrapping `ThrowingRecurringExpenseRepository`, `ExecuteAsync` propagates the thrown exception (e.g. via `Assert.ThrowsAsync<InvalidOperationException>`) instead of returning any Output (Acceptance Scenario 1) (depends on T028)

**Checkpoint**: T029 passes against the existing T023 implementation (the non-validation exception is never caught by the field-conversion try/catch blocks, which only catch the Value Object constructors, not `AddAsync`). All three User Stories are independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final verification against quickstart.md's completion criteria.

- [X] T030 [P] Run `dotnet build backend/ContasEmDia.sln` and confirm it completes with zero warnings (`TreatWarningsAsErrors` enabled on `Application`)
- [X] T031 [P] Run `dotnet test backend/Application.Tests` and confirm all tests pass with no real database access
- [X] T032 Verify `backend/Application/ContasEmDia.Application.csproj` references exclusively `ContasEmDia.Domain` (no `Infrastructure` or HTTP-transport package references) per FR-013
- [X] T033 Execute the manual validation steps in `quickstart.md` ("Validação manual rápida") — `Name = ""`, `Category = "Inexistente"`, `MonthlyAmount = 0`, remaining fields valid — and confirm three `FieldError`s (`name`, `category`, `monthlyAmount`) each matching the corresponding Domain message, with no persistence call

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup (T001 for T014) — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion — delivers the full `CreateRecurringExpenseUseCase` implementation (MVP)
- **User Story 2 (Phase 4)**: Depends on User Story 1 completion (tests the aggregation logic T023 already implements)
- **User Story 3 (Phase 5)**: Depends on User Story 1 completion (tests exception propagation through T023's implementation); independent of Phase 4
- **Polish (Phase 6)**: Depends on all three User Stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: No dependency on other stories — delivers the complete Use Case
- **User Story 2 (P2)**: Builds on the User Story 1 implementation (same `ExecuteAsync`, different test scenarios) — not independently implementable in isolation, but independently *testable and verifiable* per its Independent Test criterion
- **User Story 3 (P3)**: Builds on the User Story 1 implementation, independent of User Story 2

### Within User Story 1

- T015–T017 (Input/Output/interface) before T018 (skeleton, which references them)
- T018 (skeleton) and T019/T020/T021 (test doubles) before T022 (tests)
- T022 (tests, must fail first) before T023 (implementation)

### Parallel Opportunities

- T001, T002 (Setup) in parallel
- T006–T013 (PT-BR message revisions, Phase 2) all in parallel — 8 different files
- T014 (ICurrentDateProvider) in parallel with T004–T013
- T015, T016, T017, T019 (Phase 3) in parallel — different files
- T024–T027 (Phase 4) touch the same test file sequentially, but are independent of T028/T029 (Phase 5), so Phases 4 and 5 can be worked on in parallel by different people
- T030, T031 (Polish) in parallel

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Launch all PT-BR message revisions together (8 different files):
Task: "Revise PT-BR message in backend/Domain/ValueObjects/ExpenseName.cs"
Task: "Revise PT-BR message in backend/Domain/ValueObjects/ExpenseCategory.cs"
Task: "Revise PT-BR messages in backend/Domain/ValueObjects/Money.cs"
Task: "Revise PT-BR message in backend/Domain/ValueObjects/DueDay.cs"
Task: "Revise PT-BR message in backend/Domain/ValueObjects/Frequency.cs"
Task: "Revise PT-BR message in backend/Domain/ValueObjects/RecurringExpenseStatus.cs"
Task: "Revise PT-BR message in backend/Domain/ValueObjects/OccurrenceStatus.cs"
Task: "Revise PT-BR messages in backend/Domain/ValueObjects/ReferencePeriod.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — `IRepositoryManager`, PT-BR messages, `ICurrentDateProvider`)
3. Complete Phase 3: User Story 1 — this delivers the entire `CreateRecurringExpenseUseCase`
4. **STOP and VALIDATE**: Run `Application.Tests`, confirm the happy-path scenarios pass

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. User Story 1 → full Use Case implemented and tested (MVP)
3. User Story 2 → validation-aggregation edge cases locked in by tests
4. User Story 3 → exception-propagation behavior locked in by a test
5. Polish → build/test/quickstart verification

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- This feature's three User Stories share one implementation task (T023) by design — see the Organization note at the top
- Commit after each task or logical group
- No mocking library is used anywhere in this repository — all test doubles (T019–T021, T028) are hand-written, matching `Domain.Tests`' existing pattern
