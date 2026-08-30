---

description: "Task list for feature implementation"
---

# Tasks: Domínio de Despesa Recorrente

**Input**: Design documents from `/specs/001-despesa-recorrente-domain/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/domain-public-api.md](contracts/domain-public-api.md), [quickstart.md](quickstart.md)

**Tests**: Included. Constitution Principle II (Test-First Development) and plan.md's Constitution Check require xUnit tests in `backend/Domain.Tests` to be written and failing before each piece of domain logic is implemented.

**Organization**: Tasks are grouped by user story (US1/US2/US3, per spec.md priorities P1/P2/P3) to enable independent implementation and testing of each story. All Value Objects are foundational (Phase 2) because every user story requires constructing a fully-valid `RecurringExpense`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Tasks that append scenarios to the same test file are listed sequentially (not `[P]`) even when logically independent, to avoid edit conflicts

## Path Conventions

Per plan.md's Structure Decision: `backend/Domain/` (production code, `ContasEmDia.Domain` project) and `backend/Domain.Tests/` (xUnit tests, `ContasEmDia.Domain.Tests` project), referenced from `backend/ContasEmDia.sln`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Scaffold the two backend projects and the mandated DDD folder structure

- [X] T001 Create `backend/ContasEmDia.sln`, `backend/Domain/ContasEmDia.Domain.csproj` (classlib), and `backend/Domain.Tests/ContasEmDia.Domain.Tests.csproj` (xunit); add both projects to the solution; add a project reference from `Domain.Tests` to `Domain` (per [quickstart.md](quickstart.md) Setup section)
- [X] T002 Configure `backend/Domain/ContasEmDia.Domain.csproj`: `<TargetFramework>net10.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, and no external package references (Constitution Principle III; plan.md Technical Context — pure dependency-free domain library)
- [X] T003 [P] Configure `backend/Domain.Tests/ContasEmDia.Domain.Tests.csproj`: `<TargetFramework>net10.0</TargetFramework>` plus xUnit/`Microsoft.NET.Test.Sdk`/`xunit.runner.visualstudio` package references
- [X] T004 [P] Create the mandated empty folders `backend/Domain/Aggregates/`, `backend/Domain/Entities/`, `backend/Domain/ValueObjects/`, `backend/Domain/Repositories/` (Constitution Principle VI)

**Checkpoint**: Solution builds (with no source files yet) — `dotnet build backend/ContasEmDia.sln` succeeds.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: All 9 Value Objects from [data-model.md](data-model.md) §Value Objects. Every user story needs a fully-constructed `RecurringExpense`, which needs all of these — so none of the user story phases can start until this phase is done.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Tests for Value Objects (write first, confirm they fail to compile/run before implementing)

- [X] T005 [P] Write failing tests for `ExpenseName` in `backend/Domain.Tests/ValueObjects/ExpenseNameTests.cs`: valid non-empty name accepted; null, empty, and whitespace-only rejected (FR-002)
- [X] T006 [P] Write failing tests for `ExpenseCategory` in `backend/Domain.Tests/ValueObjects/ExpenseCategoryTests.cs`: each defined `ExpenseCategoryType` value (Housing, Services, Transportation, Subscriptions, Other) accepted; an undefined enum value rejected (FR-003)
- [X] T007 [P] Write failing tests for `Money` in `backend/Domain.Tests/ValueObjects/MoneyTests.cs`: positive values with 0-2 decimal places accepted; zero, negative, and values with more than 2 decimal places rejected (FR-004)
- [X] T008 [P] Write failing tests for `DueDay` in `backend/Domain.Tests/ValueObjects/DueDayTests.cs`: 1 and 31 accepted as boundaries; 0 and 32 rejected (FR-005)
- [X] T009 [P] Write failing tests for `CalendarDate` in `backend/Domain.Tests/ValueObjects/CalendarDateTests.cs`: wraps and round-trips any valid `DateOnly` via `GetValue()`
- [X] T010 [P] Write failing tests for `ReferencePeriod` in `backend/Domain.Tests/ValueObjects/ReferencePeriodTests.cs`: month outside 1-12 or year <= 0 rejected; `FromDate` derives the correct year/month; `CompareTo`/`>=`/`<` order periods correctly across year boundaries (e.g., Dec 2025 < Jan 2026)
- [X] T011 [P] Write failing tests for `Frequency` in `backend/Domain.Tests/ValueObjects/FrequencyTests.cs`: `Monthly` accepted; any other `FrequencyType` value rejected (FR-008)
- [X] T012 [P] Write failing tests for `RecurringExpenseStatus` in `backend/Domain.Tests/ValueObjects/RecurringExpenseStatusTests.cs`: `Active` and `Paused` both accepted as defined enum values
- [X] T013 [P] Write failing tests for `OccurrenceStatus` in `backend/Domain.Tests/ValueObjects/OccurrenceStatusTests.cs`: `Pending` and `Paid` both accepted as defined enum values
- [X] T014 [P] Write failing tests for `Note` in `backend/Domain.Tests/ValueObjects/NoteTests.cs`: `null`, empty string, and arbitrary free text all accepted with no validation, `GetValue()` returns exactly what was passed (FR-010)

### Implementation for Value Objects

- [X] T015 [P] Implement `ExpenseName` in `backend/Domain/ValueObjects/ExpenseName.cs` (satisfies T005)
- [X] T016 [P] Implement `ExpenseCategory` + `ExpenseCategoryType` enum in `backend/Domain/ValueObjects/ExpenseCategory.cs` (satisfies T006)
- [X] T017 [P] Implement `Money` in `backend/Domain/ValueObjects/Money.cs` (satisfies T007)
- [X] T018 [P] Implement `DueDay` in `backend/Domain/ValueObjects/DueDay.cs` (satisfies T008)
- [X] T019 [P] Implement `CalendarDate` in `backend/Domain/ValueObjects/CalendarDate.cs` (satisfies T009)
- [X] T020 [P] Implement `ReferencePeriod` in `backend/Domain/ValueObjects/ReferencePeriod.cs` with `Year`/`Month` read-only properties, `FromDate(DateOnly)` factory, `IComparable<ReferencePeriod>`, and `>=`/`<` operators (satisfies T010)
- [X] T021 [P] Implement `Frequency` + `FrequencyType` enum in `backend/Domain/ValueObjects/Frequency.cs` (satisfies T011)
- [X] T022 [P] Implement `RecurringExpenseStatus` + `RecurringExpenseStatusType` enum in `backend/Domain/ValueObjects/RecurringExpenseStatus.cs` (satisfies T012)
- [X] T023 [P] Implement `OccurrenceStatus` + `OccurrenceStatusType` enum in `backend/Domain/ValueObjects/OccurrenceStatus.cs` (satisfies T013)
- [X] T024 [P] Implement `Note` in `backend/Domain/ValueObjects/Note.cs` (satisfies T014)

**Checkpoint**: All Value Objects implemented, tested, and passing — `RecurringExpense`/`Occurrence` construction is now unblocked for every user story.

---

## Phase 3: User Story 1 - Cadastrar despesa recorrente ativa com ocorrência automática (Priority: P1) 🎯 MVP

**Goal**: An Active despesa whose start date is within the current competência automatically produces exactly one Pending occurrence at creation time, with the due-day-clamping rule applied and display data snapshotted from the despesa.

**Independent Test**: Construct a `RecurringExpense` with valid data, status Active, and start date ≤ the given current `ReferencePeriod`; assert `GetOccurrences()` contains exactly one `Occurrence` with status Pending, `GetExpectedAmount()` equal to the despesa's monthly amount, and (for a due day of 31 in a short month) a due date on the last day of that month.

### Tests for User Story 1 (write first, confirm they fail before implementing)

- [X] T025 [US1] Write failing test in `backend/Domain.Tests/Aggregates/RecurringExpenseTests.cs`: Active despesa with start date ≤ current competência → `GetOccurrences()` has exactly one occurrence, status Pending, `GetExpectedAmount()` equals the despesa's `GetMonthlyAmount()` (spec User Story 1, Scenario 1)
- [X] T026 [US1] Write failing tests in `backend/Domain.Tests/Aggregates/RecurringExpenseTests.cs`: due day 31 despesa generates an occurrence whose due date falls on the last day of the competência's month, covering February in a non-leap year, February in a leap year, and at least one other short month (April/June/September/November) (spec User Story 1, Scenario 2; FR-006; edge cases)
- [X] T027 [US1] Write failing test in `backend/Domain.Tests/Aggregates/RecurringExpenseTests.cs`: the generated occurrence's `GetName()`/`GetCategory()`/`GetExpectedAmount()` match the despesa's `GetName()`/`GetCategory()`/`GetMonthlyAmount()` at generation time (spec User Story 1, Scenario 3; FR-015)

### Implementation for User Story 1

- [X] T028 [US1] Implement `Occurrence` entity in `backend/Domain/Entities/Occurrence.cs`: `internal` constructor `(ReferencePeriod, CalendarDate, ExpenseName, ExpenseCategory, Money)`, status fixed to `Pending` (FR-019), `GetId()`/`GetReferencePeriod()`/`GetDueDate()`/`GetStatus()`/`GetName()`/`GetCategory()`/`GetExpectedAmount()` accessors, internally generated `Guid`
- [X] T029 [US1] Implement `RecurringExpense` aggregate constructor in `backend/Domain/Aggregates/RecurringExpense.cs`: internally generated `Guid`, field assignment, all `GetX()` accessors, `startPeriod = ReferencePeriod.FromDate(startDate.GetValue())`, and — when `status.GetValue() == Active` and `currentReferencePeriod >= startPeriod` — generation of exactly one `Occurrence` for `currentReferencePeriod` with due date `min(dueDay.GetValue(), DateTime.DaysInMonth(period.Year, period.Month))` (FR-006/FR-011/FR-020/FR-021) (depends on T028)

**Checkpoint**: User Story 1 is fully functional and independently testable — an Active, in-vigência despesa correctly produces its first occurrence.

---

## Phase 4: User Story 2 - Cadastrar despesa recorrente pausada ou com início futuro, sem gerar ocorrência (Priority: P2)

**Goal**: A Paused despesa, or an Active despesa whose start date is in a future competência, produces no occurrence at creation time.

**Independent Test**: Construct (a) a despesa with status Paused and (b) an Active despesa with a start date in a competência after the given current `ReferencePeriod`; assert `GetOccurrences()` is empty in both cases.

### Tests for User Story 2 (write first, confirm they fail before implementing)

- [X] T030 [US2] Write failing test in `backend/Domain.Tests/Aggregates/RecurringExpenseTests.cs`: despesa created with status Paused → `GetOccurrences()` is empty (spec User Story 2, Scenario 1; FR-012)
- [X] T031 [US2] Write failing test in `backend/Domain.Tests/Aggregates/RecurringExpenseTests.cs`: Active despesa whose start date is in a competência after `currentReferencePeriod` → `GetOccurrences()` is empty (spec User Story 2, Scenario 2; FR-013)

### Implementation for User Story 2

- [X] T032 [US2] Verify/complete the Paused and future-start-date branches of the occurrence-generation conditional in `backend/Domain/Aggregates/RecurringExpense.cs` so both T030 and T031 pass without generating an `Occurrence` (FR-012, FR-013) (depends on T029)

**Checkpoint**: User Stories 1 AND 2 both work independently — the full status × competência decision matrix (FR-011-FR-013) is covered.

---

## Phase 5: User Story 3 - Recuperar despesas recorrentes para uso posterior (Priority: P3)

**Goal**: A `RecurringExpense` can be stored and retrieved by identifier, and the set of Active despesas can be located, with no loss of originally-supplied data.

**Independent Test**: Save a despesa via a repository fake and retrieve it by id, asserting all originally-supplied fields round-trip; save a mixed-status set of despesas and assert `GetActiveAsync()` returns only the Active ones.

### Tests for User Story 3 (write first, confirm they fail before implementing)

- [X] T033 [US3] Write failing test in `backend/Domain.Tests/Repositories/InMemoryRecurringExpenseRepositoryTests.cs`: `AddAsync` then `GetByIdAsync` returns a despesa with name, category, monthly amount, due day, start date, frequency, status, and note all intact (spec User Story 3, Scenario 1; FR-017)
- [X] T034 [US3] Write failing test in `backend/Domain.Tests/Repositories/InMemoryRecurringExpenseRepositoryTests.cs`: given a mixed-status set of despesas (some Active, some Paused), `GetActiveAsync()` returns only the Active ones (spec User Story 3, Scenario 2; FR-017)

### Implementation for User Story 3

- [X] T035 [US3] Define `IRecurringExpenseRepository` interface in `backend/Domain/Repositories/IRecurringExpenseRepository.cs`: `Task AddAsync(RecurringExpense)`, `Task<RecurringExpense?> GetByIdAsync(Guid)`, `Task<IReadOnlyCollection<RecurringExpense>> GetActiveAsync()` (FR-017)
- [X] T036 [US3] Implement `InMemoryRecurringExpenseRepository` test fake in `backend/Domain.Tests/Repositories/InMemoryRecurringExpenseRepository.cs`, backed by a `Dictionary<Guid, RecurringExpense>`, implementing `IRecurringExpenseRepository` (test infrastructure only, per [quickstart.md](quickstart.md)) (depends on T035; satisfies T033/T034)

**Checkpoint**: All three user stories are independently functional — the Domain project is complete per this feature's scope.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation against the plan's quality gates

- [X] T037 [P] Run `dotnet build backend/ContasEmDia.sln` and confirm it builds with zero warnings (Nullable enable + TreatWarningsAsErrors, Constitution Principle III)
- [X] T038 [P] Run `dotnet test backend/ContasEmDia.sln` and confirm every test in [quickstart.md](quickstart.md)'s scenario table passes
- [X] T039 Review `RecurringExpense`, `Occurrence`, and every Value Object against Constitution Principle VI and [contracts/domain-public-api.md](contracts/domain-public-api.md): no primitive-typed public properties (other than `ReferencePeriod.Year`/`Month`), constructor-only creation, internally-generated GUIDs, `GetX()` read accessors only, no Domain Events

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories (every despesa construction needs all 9 Value Objects)
- **User Story 1 (Phase 3)**: Depends on Foundational completion only
- **User Story 2 (Phase 4)**: Depends on Foundational completion; its implementation task (T032) also depends on T029 (US1's constructor), since both stories exercise the same occurrence-generation conditional
- **User Story 3 (Phase 5)**: Depends on Foundational completion and on `RecurringExpense` existing (T029); independent of US2
- **Polish (Phase 6)**: Depends on all user story phases being complete

### User Story Dependencies

- **User Story 1 (P1)**: No dependencies on other stories — pure MVP
- **User Story 2 (P2)**: Shares production code (the occurrence-generation conditional in `RecurringExpense`) with US1's T029; its own tests (T030, T031) are independently written and independently verifiable
- **User Story 3 (P3)**: Independently testable via the repository fake; only needs `RecurringExpense` to exist (from US1)

### Within Each User Story

- Tests are written first and must fail before the corresponding implementation task
- Entity before Aggregate (T028 before T029) since the aggregate constructs the entity
- Value Objects (Phase 2) before any Aggregate/Entity/Repository work

### Parallel Opportunities

- T003 and T004 (Setup) can run in parallel
- All 10 Value Object test tasks (T005-T014) can run in parallel — 10 different files
- All 10 Value Object implementation tasks (T015-T024) can run in parallel once their corresponding test exists — 10 different files
- T025-T027, T030-T031, and T033-T034 each append to a shared test file per story and so run sequentially within their group
- T037 and T038 (Polish) can run in parallel

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Launch all Value Object tests together:
Task: "Write failing tests for ExpenseName in backend/Domain.Tests/ValueObjects/ExpenseNameTests.cs"
Task: "Write failing tests for ExpenseCategory in backend/Domain.Tests/ValueObjects/ExpenseCategoryTests.cs"
Task: "Write failing tests for Money in backend/Domain.Tests/ValueObjects/MoneyTests.cs"
Task: "Write failing tests for DueDay in backend/Domain.Tests/ValueObjects/DueDayTests.cs"
Task: "Write failing tests for CalendarDate in backend/Domain.Tests/ValueObjects/CalendarDateTests.cs"
Task: "Write failing tests for ReferencePeriod in backend/Domain.Tests/ValueObjects/ReferencePeriodTests.cs"
Task: "Write failing tests for Frequency in backend/Domain.Tests/ValueObjects/FrequencyTests.cs"
Task: "Write failing tests for RecurringExpenseStatus in backend/Domain.Tests/ValueObjects/RecurringExpenseStatusTests.cs"
Task: "Write failing tests for OccurrenceStatus in backend/Domain.Tests/ValueObjects/OccurrenceStatusTests.cs"
Task: "Write failing tests for Note in backend/Domain.Tests/ValueObjects/NoteTests.cs"

# Then, once each test exists, launch all Value Object implementations together:
Task: "Implement ExpenseName in backend/Domain/ValueObjects/ExpenseName.cs"
Task: "Implement ExpenseCategory in backend/Domain/ValueObjects/ExpenseCategory.cs"
# ... (remaining 8 in parallel)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Run `dotnet test` and confirm T025-T027 pass
5. This alone delivers the spec's central flow: active despesa → automatic first occurrence

### Incremental Delivery

1. Setup + Foundational → Value Objects ready
2. Add User Story 1 → validate independently (MVP!)
3. Add User Story 2 → validate independently (no-occurrence branches)
4. Add User Story 3 → validate independently (persistence round-trip)
5. Polish → build/test clean, Constitution VI compliance review

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Verify each test fails (or fails to compile) before writing its implementation
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- No status-transition methods (`Pausar()`/`Reativar()`) and no non-Monthly `Frequency` support are in scope for any task above (spec Clarification #1; FR-008)
