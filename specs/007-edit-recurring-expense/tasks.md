---

description: "Task list for Editar Despesa Recorrente (Backend)"
---

# Tasks: Editar Despesa Recorrente (Backend)

**Input**: Design documents from `/specs/007-edit-recurring-expense/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contract.md, quickstart.md

**Tests**: Required by this project's constitution (Principle II — Test-First Development). `Domain.Tests`/`Application.Tests` (xUnit) and `Api.Tests` (xUnit + `WebApplicationFactory<Program>`) tasks are written before the implementation task they verify, following Red-Green-Refactor, with one test per distinct response scenario each layer can produce.

**Organization**: Tasks are grouped by user story (spec.md priorities P1–P4). US1 (edit) and US2 (consult) share two Foundational DTOs but are otherwise independent additions to the same existing `RecurringExpensesController`. US3 (reactivate) and US4 (error handling) both extend the `UpdateRecurringExpenseUseCase`/`PUT` action created by US1 — the same "later phase extends an earlier phase's file" pattern already used by `004-api-despesa-recorrente/tasks.md` — so each phase remains independently *testable* per its Independent Test criterion in spec.md, even where it is not a fully isolated implementation.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Tasks that append scenarios to the same file are listed sequentially (not `[P]`) even when logically independent, to avoid edit conflicts
- Paths are relative to the repository root (`backend/...`, `specs/...`)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization.

No setup tasks are required. This feature adds no new project to `backend/ContasEmDia.sln` — it extends the four backend projects (`Domain`, `Application`, `Api`, and their `.Tests` counterparts) already scaffolded by features 001–004 (plan.md, "Project Structure").

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The two data-transport types shared by both `GET` (US2) and `PUT` (US1) — `RecurringExpenseData`/`RecurringExpenseDataResponse` and their mapping — so neither controller action has to invent its own shape. **No User Story implementation can begin until this phase is complete.**

- [X] T001 [P] Create `RecurringExpenseData` record (`Id`, `Name`, `Category`, `MonthlyAmount`, `DueDay`, `StartDate`, `Frequency`, `Status`, `Note`) and `GetRecurringExpenseByIdUseCaseOutput` class (single property `RecurringExpense: RecurringExpenseData`, no `IsSuccess`/`Errors` — "não encontrado" is signaled by exception, not by this Output, per research.md §4) in `backend/Application/UseCases/GetRecurringExpenseById/GetRecurringExpenseByIdUseCaseOutput.cs` (data-model.md)
- [X] T002 [P] Create `RecurringExpenseDataResponse` record (`Id`, `Name`, `Category`, `MonthlyAmount`, `DueDay`, `StartDate`, `Frequency`, `Status`, `Note`) in `backend/Api/Responses/RecurringExpenseDataResponse.cs` (data-model.md)
- [X] T003 Create `RecurringExpenseDataResponseMapping` with a single `ToDataResponse(this RecurringExpenseData data)` extension, reused by both the `GET` and `PUT` actions, in `backend/Api/Mappings/RecurringExpenseDataResponseMapping.cs` (depends on T001, T002)

**Checkpoint**: `dotnet build backend/ContasEmDia.sln` succeeds. Both new endpoints can now be implemented against a shared response shape.

---

## Phase 3: User Story 1 - Editar os dados de uma despesa recorrente com sucesso (Priority: P1) 🎯 MVP

**Goal**: A `PUT` request with valid values for every editable field updates the recurring expense and returns the updated data, without rewriting any already-generated occurrence, and without requiring any field to actually change (no-op edit) or being blocked by already-paid occurrences.

**Independent Test**: Send a `PUT` with all valid fields to an existing recurring expense via `WebApplicationFactory` and verify the response reflects the updated data, and that an occurrence generated before the edit keeps its old name/category/value/due-date.

### Tests for User Story 1

> Write these tests first; they must fail (methods/UseCase/route do not exist yet) before the implementation tasks below.

- [X] T004 [US1] Write failing Domain tests in `backend/Domain.Tests/Aggregates/RecurringExpenseTests.cs`: `Rename(ExpenseName)`, `ChangeCategory(ExpenseCategory)`, `ChangeMonthlyAmount(Money)`, `ChangeDueDay(DueDay)`, `ChangeStartDate(CalendarDate)`, `ChangeNote(Note)` each replace only their own field (asserted via the corresponding `GetXxx()`) and leave `GetOccurrences()` unchanged (FR-003)
- [X] T005 [US1] Add test to `RecurringExpenseTests.cs`: `Pause()` sets `GetStatus()` to `Paused` and leaves `GetOccurrences()` unchanged (FR-004) (same file as T004, sequential)
- [X] T006 [US1] Write failing Application test in new `backend/Application.Tests/UseCases/UpdateRecurringExpense/UpdateRecurringExpenseUseCaseTests.cs`: editing name/category/monthlyAmount/dueDay on a recurring expense with an existing occurrence returns success, and the existing occurrence (read back via `GetOccurrences()`) keeps its old name/category/expected amount/due date (spec.md US1 Acceptance Scenario 1, CA02)
- [X] T007 [US1] Add test to `UpdateRecurringExpenseUseCaseTests.cs`: submitting the exact same values already stored (no field differs) returns success and neither creates, changes, nor removes any occurrence (Acceptance Scenario 2, RF10 no-op) (same file, sequential)
- [X] T008 [US1] Add test to `UpdateRecurringExpenseUseCaseTests.cs`: editing a recurring expense that already has a paid occurrence succeeds and the paid occurrence's paid amount/payment date/status are unchanged (Acceptance Scenario 3, CA06/CA09) (same file, sequential)
- [X] T009 [US1] Add test to `UpdateRecurringExpenseUseCaseTests.cs`: changing status from `Active` to `Paused` succeeds and no existing occurrence is created, altered, or removed (FR-004) (same file, sequential)
- [X] T010 [US1] Add test to `UpdateRecurringExpenseUseCaseTests.cs`: calling `ExecuteAsync` with an id that does not correspond to any stored recurring expense throws `KeyNotFoundException` before any Value Object is built (same file, sequential)

### Implementation for User Story 1

- [X] T011 [P] [US1] Implement `Rename`, `ChangeCategory`, `ChangeMonthlyAmount`, `ChangeDueDay`, `ChangeStartDate`, `ChangeNote` in `backend/Domain/Aggregates/RecurringExpense.cs`, each replacing only its own backing field (`_name`/`_category`/`_monthlyAmount`/`_dueDay`/`_startDate`/`_note`) (depends on T004)
- [X] T012 [US1] Implement `Pause()` in `backend/Domain/Aggregates/RecurringExpense.cs`, replacing `_status` with `new RecurringExpenseStatus(RecurringExpenseStatusType.Paused)` (depends on T005; same file as T011, sequential)
- [X] T013 [P] [US1] Create `UpdateRecurringExpenseUseCaseInput` (`Id: Guid` required, `Name`/`Category`/`StartDate`/`Status: string` required, `MonthlyAmount: decimal` required, `DueDay: int` required, `Note: string?`) in `backend/Application/UseCases/UpdateRecurringExpense/UpdateRecurringExpenseUseCaseInput.cs` (data-model.md)
- [X] T014 [P] [US1] Create `UpdateRecurringExpenseUseCaseOutput` with the same private-constructor/`Success`/`Failure` static-factory shape as `CreateRecurringExpenseUseCaseOutput`, exposing `IsSuccess`, `RecurringExpense: RecurringExpenseData?` and `Errors: IReadOnlyCollection<FieldError>` (reusing `RecurringExpenseData` from T001 and `FieldError` from `ContasEmDia.Application.UseCases.CreateRecurringExpense`) in `backend/Application/UseCases/UpdateRecurringExpense/UpdateRecurringExpenseUseCaseOutput.cs` (depends on T001)
- [X] T015 [P] [US1] Create `IUpdateRecurringExpenseUseCase` (`Task<UpdateRecurringExpenseUseCaseOutput> ExecuteAsync(UpdateRecurringExpenseUseCaseInput input)`) in `backend/Application/UseCases/UpdateRecurringExpense/IUpdateRecurringExpenseUseCase.cs`
- [X] T016 [US1] Implement `UpdateRecurringExpenseUseCase` in `backend/Application/UseCases/UpdateRecurringExpense/UpdateRecurringExpenseUseCase.cs`: fetch the aggregate via `IRepositoryManager.RecurringExpenseRepository.GetByIdAsync(input.Id)`, throw `KeyNotFoundException("Despesa recorrente não encontrada.")` when `null`; build each Value Object in its own try/catch accumulating `FieldError` exactly like `CreateRecurringExpenseUseCase`; when `errors.Count > 0` return `Failure(errors)` without calling any aggregate method; otherwise compare each new VO's `GetValue()` to the aggregate's current value and call `Rename`/`ChangeCategory`/`ChangeMonthlyAmount`/`ChangeDueDay`/`ChangeStartDate`/`ChangeNote` only when it differs, call `Pause()` when status changes `Active`→`Paused` and `Reactivate(...)` when status changes `Paused`→`Active` (implemented together with T032/US3, since the `status` branch has to compile and be exercised by every call, including US1's own no-op/Pause tests); finish with `UpdateAsync(recurringExpense)` and return `Success(...)` built from the aggregate's now-updated getters (depends on T006-T010, T011, T012, T013, T014, T015, T032)
- [X] T017 [US1] Register `IUpdateRecurringExpenseUseCase` → `UpdateRecurringExpenseUseCase` in `backend/Api/Program.cs`'s DI composition (depends on T016)
- [X] T018 [P] [US1] Create `UpdateRecurringExpenseDataRequest` record in `backend/Api/Requests/UpdateRecurringExpenseDataRequest.cs`: `Name`/`Category`/`StartDate`/`Status` as `string?` with `[Required(ErrorMessage = "...")]` in PT-BR, `MonthlyAmount` as `decimal?` and `DueDay` as `int?` each with `[Required(ErrorMessage = "...")]`, `Note` optional with no annotation (data-model.md; no `Frequency` field, per FR-001)
- [X] T019 [P] [US1] Create `UpdateRecurringExpenseDataRequestMapping.ToUseCaseInput(this UpdateRecurringExpenseDataRequest request, Guid id)` in `backend/Api/Mappings/UpdateRecurringExpenseDataRequestMapping.cs` (depends on T018, T013)
- [X] T020 [US1] Add a `PUT("{id:guid}")` action to `RecurringExpensesController` in `backend/Api/Controllers/RecurringExpensesController.cs`: inject `IUpdateRecurringExpenseUseCase`, call `request.ToUseCaseInput(id)` → `ExecuteAsync`, on success return `200 OK` with `ApiResponse<RecurringExpenseDataResponse>.Success(output.RecurringExpense!.ToDataResponse())`; declare `[ProducesResponseType(typeof(ApiResponse<RecurringExpenseDataResponse>), StatusCodes.Status200OK)]` (FR-001, FR-002, FR-003, FR-004, FR-009, FR-010, FR-017, FR-019) (depends on T016, T017, T019, T003)
- [X] T021 [US1] Write Api.Tests in `backend/Api.Tests/Controllers/RecurringExpenseEditingTests.cs`: `PUT` success scenarios mirroring T006-T009 (field edit keeps old occurrence data, no-op edit, edit with a paid occurrence, `Active`→`Paused`) each asserting `200 OK` with the updated `data`. **Deviation from plan**: this is a new file, not an addition to `RecurringExpensesControllerTests.cs` — `GetByIdAsync`'s `.Include("_occurrences")` query (used by every new UseCase) hits the same EF Core InMemory-provider ComplexProperty limitation already documented/worked around in `OccurrencesControllerTests.cs`, so these tests use the same seeded-fake-repository technique via a new `RecurringExpenseSeededWebApplicationFactory` (depends on T020)

**Checkpoint**: T004-T010 (once implemented) and T021 pass. User Story 1 is fully functional and independently testable for every "successful edit" flow, including pausing.

---

## Phase 4: User Story 2 - Consultar os dados atuais de uma despesa recorrente para editar (Priority: P2)

**Goal**: A `GET` by id returns every editable field of an existing recurring expense with its current values, or a "not found" result for a nonexistent id — without the occurrence list.

**Independent Test**: `GET` an existing recurring expense's id via `WebApplicationFactory` and verify the response contains all editable fields with current values and no `occurrences`; `GET` a nonexistent id and verify `404`.

### Tests for User Story 2

- [X] T022 [US2] Write failing Application test in new `backend/Application.Tests/UseCases/GetRecurringExpenseById/GetRecurringExpenseByIdUseCaseTests.cs`: an existing id returns a `RecurringExpenseData` with all editable fields matching the stored aggregate (Acceptance Scenario 1)
- [X] T023 [US2] Add test to `GetRecurringExpenseByIdUseCaseTests.cs`: an id that does not correspond to any stored recurring expense throws `KeyNotFoundException` (Acceptance Scenario 2) (same file, sequential)

### Implementation for User Story 2

- [X] T024 [P] [US2] Create `GetRecurringExpenseByIdUseCaseInput` (`Id: Guid` required) in `backend/Application/UseCases/GetRecurringExpenseById/GetRecurringExpenseByIdUseCaseInput.cs`
- [X] T025 [P] [US2] Create `IGetRecurringExpenseByIdUseCase` (`Task<GetRecurringExpenseByIdUseCaseOutput> ExecuteAsync(GetRecurringExpenseByIdUseCaseInput input)`) in `backend/Application/UseCases/GetRecurringExpenseById/IGetRecurringExpenseByIdUseCase.cs`
- [X] T026 [US2] Implement `GetRecurringExpenseByIdUseCase` in `backend/Application/UseCases/GetRecurringExpenseById/GetRecurringExpenseByIdUseCase.cs`: fetch via `GetByIdAsync(input.Id)`, throw `KeyNotFoundException("Despesa recorrente não encontrada.")` when `null`, else map the aggregate's getters into a `RecurringExpenseData` and return it wrapped in `GetRecurringExpenseByIdUseCaseOutput` (depends on T022, T023, T024, T025, T001)
- [X] T027 [US2] Register `IGetRecurringExpenseByIdUseCase` → `GetRecurringExpenseByIdUseCase` in `backend/Api/Program.cs`'s DI composition (depends on T026)
- [X] T028 [US2] Add a `GET("{id:guid}")` action to `RecurringExpensesController` in `backend/Api/Controllers/RecurringExpensesController.cs`: inject `IGetRecurringExpenseByIdUseCase`, call `ExecuteAsync`, return `200 OK` with `ApiResponse<RecurringExpenseDataResponse>.Success(output.RecurringExpense.ToDataResponse())`; declare `[ProducesResponseType(typeof(ApiResponse<RecurringExpenseDataResponse>), StatusCodes.Status200OK)]` and `[ProducesResponseType(typeof(ApiResponse<RecurringExpenseDataResponse>), StatusCodes.Status404NotFound)]` (FR-011, FR-012, FR-013) (depends on T026, T027, T003)
- [X] T029 [US2] Add Api.Tests to `RecurringExpenseEditingTests.cs` (seeded-factory pattern, see T021's deviation note): `GET` an existing id → `200 OK` with all editable fields and no `occurrences` property populated; `GET` a nonexistent id → `404 Not Found` with the standard error envelope (depends on T028)

**Checkpoint**: T022-T023 (implemented) and T029 pass. User Stories 1 and 2 both work independently.

---

## Phase 5: User Story 3 - Reativar uma despesa recorrente pausada durante a edição (Priority: P3)

**Goal**: When an edit changes status from `Paused` to `Active`, the same `PUT` call evaluates and, when due, generates the current-period Pending occurrence — never a duplicate, never before the start date.

**Independent Test**: Edit a paused recurring expense's status to `Active` and verify occurrence generation (or its absence) according to the start-date and non-duplication conditions.

### Tests for User Story 3

- [X] T030 [US3] Write failing Domain tests in `backend/Domain.Tests/Aggregates/RecurringExpenseTests.cs`: `Reactivate(currentReferencePeriod)` (a) sets status to `Active` and adds a Pending occurrence for the current period with the current monthly amount when the start date has begun and no occurrence exists yet for that period (Acceptance Scenario 1); (b) adds no additional occurrence when one already exists for the current period (Acceptance Scenario 2); (c) sets status to `Active` but adds no occurrence when the start date is still in the future (Acceptance Scenario 3)
- [X] T031 [US3] Add tests to `UpdateRecurringExpenseUseCaseTests.cs` mirroring T030's three scenarios, driving them through `UpdateRecurringExpenseUseCase.ExecuteAsync` with `status` changing from `Paused` to `Active` (same file as T006-T010, sequential)

### Implementation for User Story 3

- [X] T032 [US3] In `backend/Domain/Aggregates/RecurringExpense.cs`: extract the occurrence-generation logic currently inline in the constructor (lines ~40-49) into a private `GenerateOccurrenceForCurrentPeriodIfDue(ReferencePeriod currentReferencePeriod)` method (condition: `currentReferencePeriod >= startPeriod` and `GetOccurrencesForPeriod(currentReferencePeriod).Count == 0`), call it from the constructor when `status == Active`, and implement `Reactivate(ReferencePeriod currentReferencePeriod)` as `_status = new RecurringExpenseStatus(RecurringExpenseStatusType.Active);` followed by a call to the new private method — without changing any existing constructor test's observed behavior (depends on T030; research.md §2)
- [X] T033 [US3] Extend `UpdateRecurringExpenseUseCase` in `backend/Application/UseCases/UpdateRecurringExpense/UpdateRecurringExpenseUseCase.cs`: inject `ICurrentDateProvider` (already registered in DI since feature 004) into the constructor, and add the `Paused`→`Active` branch that calls `recurringExpense.Reactivate(ReferencePeriod.FromDate(_currentDateProvider.GetCurrentDate()))` (depends on T031, T032, T016)
- [X] T034 [US3] Add an Api.Test to `RecurringExpenseEditingTests.cs` (seeded-factory pattern): `PUT` changing a paused recurring expense (with start date already begun, no current-period occurrence yet) to `Active` → `200 OK`, and the seeded aggregate reference confirms exactly one new Pending occurrence for the current period; repeating the same `PUT` a second time creates no additional occurrence (depends on T033, T028)

**Checkpoint**: T030-T031 (implemented) and T034 pass. All three functional user stories work independently.

---

## Phase 6: User Story 4 - Receber erros claros ao editar com dados inválidos ou identificador inexistente (Priority: P4)

**Goal**: A `PUT` with an invalid field, or targeting a nonexistent id, is rejected in full with the same error envelope already used by the registration endpoint — nothing is partially saved.

**Independent Test**: `PUT` a body with an invalid field, and separately `PUT` a nonexistent id, verifying no data is changed in either case.

### Tests for User Story 4

- [X] T035 [US4] Add an Api.Test to `RecurringExpenseEditingTests.cs`: `PUT` with a field violating a business rule (e.g. `monthlyAmount: -10`) → `400 Bad Request`, `errors` with one item (`field: "monthlyAmount"`, PT-BR message relayed as-is from the Domain) (spec.md US4 Acceptance Scenario 1, CA01)
- [X] T036 [US4] Add an Api.Test to `RecurringExpenseEditingTests.cs`: `PUT` with the required `name` field missing from the JSON body → `400 Bad Request`, same envelope as T035, `{ "field": "name", "message": "Nome é obrigatório." }`, never ASP.NET Core's native `ValidationProblemDetails` (FR-020)
- [X] T037 [US4] Add an Api.Test to `RecurringExpenseEditingTests.cs`: `PUT` targeting an id that does not correspond to any stored recurring expense → `404 Not Found`, no recurring expense created (Acceptance Scenario 2, FR-015)

### Implementation for User Story 4

- [X] T038 [US4] Extend the `PUT` action in `backend/Api/Controllers/RecurringExpensesController.cs`: on `!output.IsSuccess`, return `400 Bad Request` with `ApiResponse<RecurringExpenseDataResponse>.Failure(output.Errors.ToApiErrors())` (reusing the existing `ToApiErrors()` extension from `CreateRecurringExpenseDataResponseMapping.cs`); add `[ProducesResponseType(typeof(ApiResponse<RecurringExpenseDataResponse>), StatusCodes.Status400BadRequest)]` and `[ProducesResponseType(typeof(ApiResponse<RecurringExpenseDataResponse>), StatusCodes.Status404NotFound)]` to the `PUT` action (FR-014, FR-015, FR-016, FR-020) (depends on T035, T036, T037, T020)

**Checkpoint**: T035-T037 pass. All four user stories are independently functional with one consistent error envelope.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Cover the `500` safety net for both new actions and run final verification against `quickstart.md`.

- [X] T039 [P] Add `[ProducesResponseType(typeof(ApiResponse<RecurringExpenseDataResponse>), StatusCodes.Status500InternalServerError)]` to both the `GET` and `PUT` actions in `backend/Api/Controllers/RecurringExpensesController.cs`, completing the full response-outcome declaration for each
- [X] T040 [P] Add Api.Tests (reusing the existing `ThrowingRepositoryWebApplicationFactory`/`ThrowingRecurringExpenseRepository`, extending its `GetByIdAsync` to throw the same simulated failure) to `backend/Api.Tests/ExceptionHandlingMiddlewareTests.cs`: an unexpected failure on `GET` and on `PUT` each returns `500 Internal Server Error` with the generic `ApiResponse` envelope, no internal exception details in the body
- [X] T041 Execute `quickstart.md`'s 9 manual scenarios end-to-end against a locally running `dotnet run` instance (real PostgreSQL, not the test double), and confirm `dotnet test` passes in full for `Domain.Tests`, `Application.Tests`, `Api.Tests`, and `Infrastructure.Tests`. **This step found two real, pre-existing Infrastructure bugs invisible to every test double used earlier** (contradicting plan.md's "Infrastructure: no changes needed" — see Constitution Check re-evaluation below):
  1. `RecurringExpenseConfigurations`'s `_note` conversion is bypassed by EF Core for a `NULL` database column (converters don't run on nulls by default), leaving `_note` as a bare `null` instead of `new Note(null)` — crashing `GetNote().GetValue()` on any fetched expense with no note. Fixed in the Domain layer instead of Infrastructure: `RecurringExpense`'s private EF-reconstruction constructor now normalizes `note ?? new Note(null)`, restoring the same "never null" invariant the public constructor already guarantees (`backend/Domain/Aggregates/RecurringExpense.cs`). Regression test: `GetByIdAsync_ExpenseWithNullNote_ReturnsNonNullNoteValueObjectWithNullValue` in `backend/Infrastructure.Tests/Repositories/RecurringExpenseRepositoryTests.cs`.
  2. `RecurringExpenseRepository.UpdateAsync` let EF Core's automatic graph-based change detection decide the state of a newly-added `Occurrence` on an *already-tracked* `RecurringExpense` (the `Reactivate()` case) — because `Occurrence`'s id is a client-generated, already-non-default `Guid`, EF's default heuristic assumed it already existed and issued an `UPDATE` (affecting 0 rows) instead of an `INSERT`, throwing `DbUpdateConcurrencyException`. Fixed in `backend/Infrastructure/Repositories/RecurringExpenseRepository.cs`: `UpdateAsync` now explicitly sets `EntityState.Added` for any occurrence whose `_context.Entry(...)` is still `Detached` before calling `SaveChangesAsync()`. Regression test: `UpdateAsync_ReactivatedAggregateAddsNewOccurrence_PersistsTheNewOccurrenceAsAnInsert` in the same file.
  
  Both bugs were invisible to `Application.Tests` (hand-written in-memory repository fakes) and to the EF InMemory-provider-backed `Api.Tests` (which never reach real column-null / real-INSERT-vs-UPDATE semantics), and neither was exercised by any pre-existing endpoint — confirming quickstart validation against the real database, not just the test suite, is what caught them.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No tasks — nothing to scaffold
- **Foundational (Phase 2)**: No dependencies — start immediately — BLOCKS User Stories 1 and 2 (both need `RecurringExpenseData`/`RecurringExpenseDataResponse`)
- **User Story 1 (Phase 3)**: Depends on Foundational — delivers the base `PUT` success path (MVP)
- **User Story 2 (Phase 4)**: Depends on Foundational only — independent of User Story 1's files (separate UseCase, separate controller action), can be built in parallel with Phase 3 if staffed
- **User Story 3 (Phase 5)**: Depends on User Story 1 (extends `UpdateRecurringExpenseUseCase` and the `PUT` action's controller test file)
- **User Story 4 (Phase 6)**: Depends on User Story 1 (extends the same `PUT` action); independent of Phase 5's content, but shares `RecurringExpensesController.cs`/`RecurringExpensesControllerTests.cs` sequentially with it
- **Polish (Phase 7)**: Depends on all four User Stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: No dependency on other stories — delivers the base editing endpoint (success path)
- **User Story 2 (P2)**: Independent of User Story 1 — separate UseCase and controller action; only shares the Foundational DTOs
- **User Story 3 (P3)**: Builds on User Story 1's `UpdateRecurringExpenseUseCase`/`PUT` action — not independently implementable in isolation, but independently *testable* per its Independent Test criterion
- **User Story 4 (P4)**: Builds on User Story 1's `PUT` action — same caveat as User Story 3; independent of User Story 3's content

### Within User Story 1

- T004-T005 (Domain tests) before T011-T012 (Domain implementation)
- T006-T010 (Application tests) before T016 (UseCase implementation)
- T013, T014, T015 (Input/Output/Interface) before T016 (references them)
- T016, T017 (UseCase + DI) and T019 (request mapping) before T020 (controller action)
- T020 before T021 (Api.Tests)

### Parallel Opportunities

- T001, T002 (Foundational) in parallel — different files; T003 depends on both
- T011 (6 simple field-replace methods, one file) has no internal parallelism, but is independent of T013, T014, T015 (different files) — all four can proceed in parallel once their respective tests (T004-T010) are written
- T013, T014, T015, T018 (Phase 3 Input/Output/Interface/Request) in parallel — 4 different files
- T024, T025 (Phase 4 Input/Interface) in parallel — 2 different files
- Phase 3 (US1) and Phase 4 (US2) can be worked on in parallel by different developers once Phase 2 is done — they touch different UseCase folders and only meet at the shared controller file (`RecurringExpensesController.cs`), which needs a single merge for both new actions
- T039, T040 (Polish) in parallel — different files

---

## Parallel Example: Foundational + User Story 1 Input/Output/Interface

```bash
# Phase 2 (different files):
Task: "Create RecurringExpenseData record and GetRecurringExpenseByIdUseCaseOutput in backend/Application/UseCases/GetRecurringExpenseById/GetRecurringExpenseByIdUseCaseOutput.cs"
Task: "Create RecurringExpenseDataResponse record in backend/Api/Responses/RecurringExpenseDataResponse.cs"

# Phase 3, once T004-T010 are written (different files):
Task: "Create UpdateRecurringExpenseUseCaseInput in backend/Application/UseCases/UpdateRecurringExpense/UpdateRecurringExpenseUseCaseInput.cs"
Task: "Create UpdateRecurringExpenseUseCaseOutput in backend/Application/UseCases/UpdateRecurringExpense/UpdateRecurringExpenseUseCaseOutput.cs"
Task: "Create IUpdateRecurringExpenseUseCase in backend/Application/UseCases/UpdateRecurringExpense/IUpdateRecurringExpenseUseCase.cs"
Task: "Create UpdateRecurringExpenseDataRequest record in backend/Api/Requests/UpdateRecurringExpenseDataRequest.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 2: Foundational (shared `RecurringExpenseData`/`RecurringExpenseDataResponse`)
2. Complete Phase 3: User Story 1 — a recurring expense can be edited end-to-end, including pausing, without touching existing occurrences
3. **STOP and VALIDATE**: Run `Domain.Tests`, `Application.Tests`, `Api.Tests`; confirm the success scenarios pass

### Incremental Delivery

1. Foundational → shared DTOs ready
2. User Story 1 → editing works end-to-end for every field, including pause (MVP)
3. User Story 2 → the edit screen's preload query is available, independent of User Story 1
4. User Story 3 → reactivation's occurrence-generation side effect is locked in by tests, extending User Story 1's UseCase
5. User Story 4 → the error envelope (business-rule `400`, shape/presence `400`, `404`) is locked in by tests, extending the same `PUT` action
6. Polish → `500` safety net for both new actions, quickstart verification

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- User Stories 3 and 4 extend the same `UpdateRecurringExpenseUseCase`/`PUT` action User Story 1 creates, by design — see the Organization note at the top
- Commit after each task or logical group
- No mocking library is used anywhere in this repository — any test double needed for T040 should be hand-written, matching the pattern already used by `Application.Tests`/`Domain.Tests`/`Api.Tests` (feature 004)
