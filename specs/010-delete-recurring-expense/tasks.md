---

description: "Task list for Deletar Despesa Recorrente (Backend)"
---

# Tasks: Deletar Despesa Recorrente (Backend)

**Input**: Design documents from `/specs/010-delete-recurring-expense/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contract.md, quickstart.md

**Tests**: Required by this project's constitution (Principle II — Test-First Development). `Domain.Tests`/`Application.Tests` (xUnit) and `Api.Tests` (xUnit + `WebApplicationFactory<Program>`) tasks are written before the implementation task they verify, following Red-Green-Refactor; `Infrastructure.Tests` (xUnit against the real `PostgreSqlContainerFixture`) tasks prove the cascade-delete and `_deletedAt`-filter behaviors research.md explicitly flags as "verification required, not assumed" — same pattern already used by `specs/007-edit-recurring-expense/tasks.md` and `specs/009-data-fim-despesa-recorrente/tasks.md`.

**Organization**: Tasks are grouped by user story (spec.md priorities P1–P3, four stories). US1 and US2 are the two co-equal P1 branches of the *same* new `DeleteRecurringExpenseUseCase`/`DELETE` action — US1 builds the full `ExecuteAsync` method (scope validation, not-found fetch, and both scope branches) end-to-end for the `pending-only` path, since the method has to compile and branch correctly for either scope to work at all; US2 then proves the `all` branch's cascade-delete behavior against real PostgreSQL and wires its Api.Tests. US3 and US4 both extend the same controller action with their respective error-response wiring (`400`/`404`), the same "later phase extends an earlier phase's file" pattern `007-edit-recurring-expense/tasks.md` used for its own US3/US4 — so every phase remains independently *testable* per its Independent Test criterion in spec.md, even where it is not a fully isolated implementation.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Tasks that append scenarios to the same file are listed sequentially (not `[P]`) even when logically independent, to avoid edit conflicts
- Paths are relative to the repository root (`backend/...`)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization.

No setup tasks are required. This feature adds no new project to `backend/ContasEmDia.sln` — it extends the four backend projects (`Domain`, `Application`, `Infrastructure`, `Api`, and their `.Tests` counterparts) already scaffolded by features 001–009 (plan.md, "Project Structure").

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Thread the new `_deletedAt` persistence column and the shared `DeleteRecurringExpenseUseCase`/response shapes through every layer, and add the trivial (no-business-rule) `RemoveAsync` repository method the `"all"` branch will call — all before any deletion business behavior exists. **No User Story can begin until this phase is complete.**

- [X] T001 [P] Add `private CalendarDate? _deletedAt` field and `public bool IsDeleted() => _deletedAt is not null;` / `public CalendarDate? GetDeletedAt() => _deletedAt;` read accessors to `RecurringExpense` in `backend/Domain/Aggregates/RecurringExpense.cs` — defaults to `null` in both constructors, no mutator yet (data-model.md)
- [X] T002 [P] Add `Task RemoveAsync(RecurringExpense recurringExpense);` to `IRecurringExpenseRepository` in `backend/Domain/Repositories/IRecurringExpenseRepository.cs` (data-model.md)
- [X] T003 Implement `RemoveAsync` in `backend/Infrastructure/Repositories/RecurringExpenseRepository.cs`: `_context.RecurringExpenses.Remove(recurringExpense); await _context.SaveChangesAsync();` — relies on the existing required 1:N `Occurrence` relationship to cascade-delete dependents, to be proven (not assumed) by T021 (research.md §3; depends on T002)
- [X] T004 [P] Add the `_deletedAt` column mapping to `RecurringExpenseConfigurations.Configure()` in `backend/Infrastructure/Configs/RecurringExpenseConfigurations.cs`: `builder.Property<CalendarDate?>("_deletedAt").HasColumnName("DeletedAt").HasConversion(vo => vo!.GetValue(), value => new CalendarDate(value)).HasColumnType("date").IsRequired(false);` (data-model.md; depends on T001)
- [X] T005 Create EF Core migration `AddRecurringExpenseDeletedAt` in `backend/Infrastructure/Migrations/` (`dotnet ef migrations add AddRecurringExpenseDeletedAt --project Infrastructure --startup-project Api` or this repo's equivalent command): `AddColumn<DateOnly>("DeletedAt", "RecurringExpenses", type: "date", nullable: true)`, additive-only, no edit to any prior migration (data-model.md; depends on T004)
- [X] T006 [P] Create `IDeleteRecurringExpenseUseCase` (`Task<DeleteRecurringExpenseUseCaseOutput> ExecuteAsync(DeleteRecurringExpenseUseCaseInput input)`) in `backend/Application/UseCases/DeleteRecurringExpense/IDeleteRecurringExpenseUseCase.cs`
- [X] T007 [P] Create `DeleteRecurringExpenseUseCaseInput` (`required Guid Id`, `string? Scope`) in `backend/Application/UseCases/DeleteRecurringExpense/DeleteRecurringExpenseUseCaseInput.cs` (data-model.md)
- [X] T008 [P] Create `DeleteRecurringExpenseData` record (`Guid RecurringExpenseId`, `string Scope`) and `DeleteRecurringExpenseUseCaseOutput` (private-constructor/`Success`/`Failure` static-factory shape mirroring `UpdateRecurringExpenseUseCaseOutput`, exposing `IsSuccess`, `DeleteRecurringExpenseData? Data`, `IReadOnlyCollection<FieldError> Errors`) in `backend/Application/UseCases/DeleteRecurringExpense/DeleteRecurringExpenseUseCaseOutput.cs` (data-model.md)
- [X] T009 [P] Create `DeleteRecurringExpenseDataResponse` record (`Guid RecurringExpenseId`, `string Scope`) in `backend/Api/Responses/DeleteRecurringExpenseDataResponse.cs` (contracts/api-contract.md)
- [X] T010 Create `DeleteRecurringExpenseDataResponseMapping` with a single `ToDataResponse(this DeleteRecurringExpenseData data)` extension in `backend/Api/Mappings/DeleteRecurringExpenseDataResponseMapping.cs` (depends on T008, T009)

**Checkpoint**: `dotnet build backend/ContasEmDia.sln` succeeds. The deletion shapes and the `_deletedAt` column exist; no deletion behavior is implemented yet.

---

## Phase 3: User Story 1 - Excluir preservando o histórico pago (Priority: P1) 🎯 MVP

**Goal**: `DELETE ...?scope=pending-only` removes every unpaid occurrence, leaves every paid occurrence untouched, and permanently blocks any future occurrence generation for that expense.

**Independent Test**: Delete, with this scope, a recurring expense that has at least one paid and one pending occurrence, and verify the paid occurrence stays accessible in its competência while the pending one disappears, and that no new occurrence is generated on reaching a future competência.

### Tests for User Story 1

> Write these tests first; they must fail (the Domain method/UseCase/route do not exist yet) before the implementation tasks below.

- [X] T011 [US1] Write failing Domain tests in `backend/Domain.Tests/Aggregates/RecurringExpenseTests.cs`: `DeleteKeepingPaidHistory(currentDate)` removes every occurrence whose status is not `Paid` and leaves every `Paid` occurrence untouched; it sets `IsDeleted()` to `true` and `GetDeletedAt()` to `currentDate`; calling it on an expense with zero occurrences, or only `Paid` occurrences, removes nothing but still marks it deleted (spec.md US1 AC1/AC3; data-model.md)
- [X] T012 [US1] Add tests to `RecurringExpenseTests.cs`: after `DeleteKeepingPaidHistory(currentDate)`, calling `ChangeEndDate` with a later competência, or `Reactivate`, generates no new occurrence for any period, and neither call throws (the no-reactivation guarantee from FR-003/data-model.md) (same file, sequential)

- [X] T013 [US1] Write failing Application tests in new `backend/Application.Tests/UseCases/DeleteRecurringExpense/DeleteRecurringExpenseUseCaseTests.cs` (hand-written `FakeRepositoryManager`/`InMemoryRecurringExpenseRepository`/`FixedCurrentDateProvider`, mirroring the `UpdateRecurringExpense` test fixtures): `Scope: "pending-only"` on a seeded expense with one paid and one pending occurrence returns `Success` with `Data.Scope == "pending-only"`, the pending occurrence is gone from `GetOccurrences()`, the paid one is untouched, and the fake repository's `UpdateAsync` was called (never `RemoveAsync`) (spec.md US1 AC1)
- [X] T014 [US1] Add tests to `DeleteRecurringExpenseUseCaseTests.cs`: `Scope: "pending-only"` on an expense with only paid occurrences returns `Success`, removes nothing, and still marks the aggregate deleted; an `Id` that does not correspond to any stored recurring expense throws `KeyNotFoundException("Despesa recorrente não encontrada.")` before any repository mutation (spec.md US1 AC3, FR-006) (same file, sequential)

### Implementation for User Story 1

- [X] T015 [US1] Implement `public void DeleteKeepingPaidHistory(CalendarDate currentDate)` in `backend/Domain/Aggregates/RecurringExpense.cs`: `_occurrences.RemoveAll(o => o.GetStatus().GetValue() != OccurrenceStatusType.Paid); _deletedAt = currentDate;` — never throws (data-model.md; depends on T011, T001)
- [X] T016 [US1] In `backend/Domain/Aggregates/RecurringExpense.cs`: add an `if (_deletedAt is not null) return;` guard at the top of `GenerateOccurrenceForPeriodIfMissing`, making it a no-op once the expense is deleted — covers both `ChangeEndDate`'s extend branch and `Reactivate` without any new exception (data-model.md; depends on T012, T015)
- [X] T017 [US1] Implement `DeleteRecurringExpenseUseCase.ExecuteAsync` in `backend/Application/UseCases/DeleteRecurringExpense/DeleteRecurringExpenseUseCase.cs`: validate `input.Scope` is exactly `"pending-only"` or `"all"`, else return `Failure([new FieldError("scope", "Informe a abrangência da exclusão: \"pending-only\" ou \"all\".")])` before touching the repository (FR-002); fetch via `IRepositoryManager.RecurringExpenseRepository.GetByIdAsync(input.Id)`, `?? throw new KeyNotFoundException("Despesa recorrente não encontrada.")` (FR-006); when `"all"`, call `repository.RemoveAsync(recurringExpense)`; when `"pending-only"`, call `recurringExpense.DeleteKeepingPaidHistory(new CalendarDate(_currentDateProvider.GetCurrentDate()))` then `repository.UpdateAsync(recurringExpense)` (constructor takes `ICurrentDateProvider`, same pattern as `UpdateRecurringExpenseUseCase`); return `Success(new DeleteRecurringExpenseData(recurringExpense.GetId(), input.Scope))` (data-model.md "state transition"; depends on T013, T014, T006, T007, T008, T015, T003)
- [X] T018 [US1] Register `IDeleteRecurringExpenseUseCase` → `DeleteRecurringExpenseUseCase` in `backend/Api/Program.cs`'s DI composition (depends on T017)
- [X] T019 [US1] Add a `[HttpDelete("{id:guid}")]` action to `RecurringExpensesController` in `backend/Api/Controllers/RecurringExpensesController.cs`: inject `IDeleteRecurringExpenseUseCase`, accept `[FromQuery] string? scope`, call `ExecuteAsync(new DeleteRecurringExpenseUseCaseInput { Id = id, Scope = scope })`; on success return `200 OK` with `ApiResponse<DeleteRecurringExpenseDataResponse>.Success(output.Data!.ToDataResponse())`; declare `[ProducesResponseType(typeof(ApiResponse<DeleteRecurringExpenseDataResponse>), StatusCodes.Status200OK)]` (contracts/api-contract.md; depends on T017, T018, T010)
- [X] T020 [US1] Add Api.Tests in new `backend/Api.Tests/Controllers/RecurringExpenseDeletionTests.cs` (seeded-`WebApplicationFactory` pattern, same `.Include("_occurrences")` EF InMemory-provider workaround already used by `RecurringExpenseEditingTests.cs`): `DELETE ...?scope=pending-only` on a seeded expense with a paid and a pending occurrence → `200 OK` with `data.scope == "pending-only"`, the pending occurrence gone from the seeded aggregate, the paid one untouched; the same call on an expense with only paid occurrences → `200 OK`, nothing removed (contracts/api-contract.md, spec.md US1 AC1/AC3; depends on T019)

**Checkpoint**: T011-T014 pass; T020 passes. User Story 1 is fully functional end-to-end and independently testable.

---

## Phase 4: User Story 2 - Excluir todo o histórico (Priority: P1)

**Goal**: `DELETE ...?scope=all` removes absolutely every occurrence of the expense — paid or not — and the recurring expense itself ceases to exist.

**Independent Test**: Delete, with this scope, a recurring expense that has paid and pending occurrences, and verify none of them remain accessible in any query, and that a new query for the recurring expense itself responds "not found".

### Tests for User Story 2

- [X] T021 [US2] Write a failing Infrastructure.Tests integration test in `backend/Infrastructure.Tests/Repositories/RecurringExpenseRepositoryTests.cs` against the real `PostgreSqlContainerFixture`: persist a recurring expense with at least one paid and one pending occurrence via `AddAsync`, fetch it via `GetByIdAsync`, call `RemoveAsync`, then confirm — via a direct query, **not** `GetByIdAsync` — that both the `RecurringExpenses` row and every corresponding `Occurrences` row (paid and pending) are actually gone from the database (research.md §3, "verification required, not assumed"; spec.md US2 AC1/AC2)
- [X] T022 [US2] Add a test to `DeleteRecurringExpenseUseCaseTests.cs` (same file as T013/T014, sequential): `Scope: "all"` on an expense with paid and pending occurrences returns `Success` with `Data.Scope == "all"` and calls the fake repository's `RemoveAsync` (never `UpdateAsync`) (spec.md US2 AC1)

### Implementation for User Story 2

- [X] T023 [US2] Run T021 against the local PostgreSQL instance (`dotnet test --filter FullyQualifiedName~Infrastructure.Tests`, dev DB up per `deploy/`); if the default EF Core cascade does not delete the dependent `Occurrence` rows, explicitly remove every tracked occurrence from `_context` before removing the parent in `RemoveAsync` (`backend/Infrastructure/Repositories/RecurringExpenseRepository.cs`) — fallback only if research.md §3's assumed default cascade is confirmed insufficient (depends on T021, T003)
- [X] T024 [US2] Add Api.Tests to `RecurringExpenseDeletionTests.cs`: `DELETE ...?scope=all` on a seeded expense with paid and pending occurrences → `200 OK` with `data.scope == "all"`; a subsequent `GET .../{id}` on the same id → `404 Not Found` (contracts/api-contract.md, spec.md US2 AC1/AC2; depends on T019, T022)

**Checkpoint**: T021-T022 pass (T021 verified against real PostgreSQL). T024 passes. User Stories 1 and 2 both work independently.

---

## Phase 5: User Story 3 - Rejeitar uma exclusão sem abrangência válida (Priority: P2)

**Goal**: A deletion request missing `scope`, or with a value that isn't one of the two supported ones, is rejected in full — no occurrence removed, no field of the recurring expense changed.

**Independent Test**: Send a deletion request without the scope parameter, and separately with an unsupported value, and verify neither affects any occurrence or the recurring expense.

### Tests for User Story 3

- [X] T025 [US3] Add tests to `DeleteRecurringExpenseUseCaseTests.cs` (same file, sequential): `Scope: null` and `Scope: "invalido"` each return `Failure` with exactly one `FieldError("scope", "Informe a abrangência da exclusão: \"pending-only\" ou \"all\".")`, and neither the fake repository's `UpdateAsync` nor `RemoveAsync` is ever called (spec.md US3 AC1/AC2, FR-002)
- [X] T026 [US3] Add Api.Tests to `RecurringExpenseDeletionTests.cs`: `DELETE .../{id}` with no `scope` query parameter → `400 Bad Request`, `errors[0].field == "scope"`; `DELETE .../{id}?scope=invalido` → the same `400 Bad Request`; a subsequent `GET .../{id}` after either call still succeeds (`200 OK`, expense unaffected) (contracts/api-contract.md, spec.md US3 AC1/AC2)

### Implementation for User Story 3

- [X] T027 [US3] Extend the `DELETE` action in `backend/Api/Controllers/RecurringExpensesController.cs`: on `!output.IsSuccess`, return `400 Bad Request` with `ApiResponse<DeleteRecurringExpenseDataResponse>.Failure(output.Errors.ToApiErrors())` (reusing the existing `ToApiErrors()` extension); add `[ProducesResponseType(typeof(ApiResponse<DeleteRecurringExpenseDataResponse>), StatusCodes.Status400BadRequest)]` (contracts/api-contract.md; depends on T025, T026, T019)

**Checkpoint**: T025 passes; T026 passes. All three functional-so-far user stories work independently.

---

## Phase 6: User Story 4 - Tratar identificador inexistente ou já excluído como "não encontrado" (Priority: P3)

**Goal**: Deleting, querying, or editing an id that never corresponded to a recurring expense, or that corresponds to one already deleted (in either scope), always responds "not found" — never resurrects, never retries a deletion, never succeeds.

**Independent Test**: Try to delete an id that never existed; then successfully delete a real expense and try to delete (or edit, or query) it a second time; verify every one of these attempts responds "not found".

### Tests for User Story 4

- [X] T028 [US4] Add an Infrastructure.Tests test to `backend/Infrastructure.Tests/Repositories/RecurringExpenseRepositoryTests.cs`: after persisting a recurring expense and calling `DeleteKeepingPaidHistory(...)` + `UpdateAsync` (the `pending-only` scope), `GetByIdAsync(id)` for that same id returns `null` (FR-007, research.md §4)
- [X] T029 [US4] Add a regression test to the same file (sequential): for that same `pending-only`-deleted expense, `GetByOccurrenceIdAsync(paidOccurrenceId)` still returns the expense (non-null), so the payment flow keeps finding its paid occurrence (FR-008, research.md §4 "one coupling to break")
- [X] T030 [US4] Add Api.Tests to `RecurringExpenseDeletionTests.cs`: `DELETE` an id that never existed, with a valid `scope` → `404 Not Found`; reusing a `scope=all`-deleted expense from US2, each of `GET`/`PUT`/`DELETE` on the same id → `404 Not Found`; reusing a `scope=pending-only`-deleted expense from US1, each of `GET`/`PUT`/`DELETE` on the same id → `404 Not Found` (contracts/api-contract.md, spec.md US4 AC1-AC3)

### Implementation for User Story 4

- [X] T031 [US4] In `backend/Infrastructure/Repositories/RecurringExpenseRepository.cs`: add an `EF.Property<CalendarDate?>(e, "_deletedAt") == null` filter to `GetByIdAsync`'s query (research.md §4; depends on T028)
- [X] T032 [US4] In the same file: change `GetByOccurrenceIdAsync` to stop delegating to `GetByIdAsync` and instead run its own unfiltered `.Include("_occurrences")` lookup by the resolved `recurringExpenseId`, so it keeps finding a `pending-only`-deleted expense's paid occurrences (research.md §4; depends on T029, T031)
- [X] T033 [US4] Add `[ProducesResponseType(typeof(ApiResponse<DeleteRecurringExpenseDataResponse>), StatusCodes.Status404NotFound)]` to the `DELETE` action in `backend/Api/Controllers/RecurringExpensesController.cs` (contracts/api-contract.md; depends on T030, T031)

**Checkpoint**: T028-T029 pass; T030 passes. All four user stories are independently functional, with deletion now permanent and "not found forever" for any already-deleted id.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Close the `500` safety net and run final end-to-end verification.

- [X] T034 [P] Add `[ProducesResponseType(typeof(ApiResponse<DeleteRecurringExpenseDataResponse>), StatusCodes.Status500InternalServerError)]` to the `DELETE` action in `backend/Api/Controllers/RecurringExpensesController.cs`, completing its full response-outcome declaration
- [X] T035 [P] Add an Api.Test (reusing the existing `ThrowingRepositoryWebApplicationFactory`/`ThrowingRecurringExpenseRepository` pattern from `backend/Api.Tests/ExceptionHandlingMiddlewareTests.cs`, extending it to throw from `GetByIdAsync` for the `DELETE` action too): an unexpected failure on `DELETE` returns `500 Internal Server Error` with the generic `ApiResponse` envelope, no internal exception details in the body
- [X] T036 Execute `quickstart.md`'s full walkthrough (User Stories 1-4 + the Infrastructure regression checks) end-to-end against a locally running `dotnet run` instance with real PostgreSQL, confirming `dotnet test` passes in full for all four backend test projects (`Domain.Tests`, `Application.Tests`, `Infrastructure.Tests`, `Api.Tests`)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No tasks — nothing to scaffold
- **Foundational (Phase 2)**: No dependencies — start immediately — BLOCKS every User Story (all four extend the same new `DeleteRecurringExpenseUseCase`/`DELETE` action and the new `_deletedAt` column)
- **User Story 1 (Phase 3)**: Depends on Foundational — delivers the base `pending-only` success path and the full `ExecuteAsync` method skeleton (MVP)
- **User Story 2 (Phase 4)**: Depends on Foundational (`RemoveAsync`) and on User Story 1's `ExecuteAsync`/controller action existing to call the `"all"` branch and assert its Api.Tests; its own distinct deliverable is the mandatory cascade-delete proof against real PostgreSQL
- **User Story 3 (Phase 5)**: Depends on User Story 1 (extends the same `DELETE` action's error-response wiring; the validation logic itself already exists from T017)
- **User Story 4 (Phase 6)**: Depends on User Story 1 (same `DELETE` action) and is independent of Phase 5's content, though both sequentially touch `RecurringExpensesController.cs`/`RecurringExpenseDeletionTests.cs`
- **Polish (Phase 7)**: Depends on all four User Stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends only on Foundational — delivers the base `pending-only` deletion endpoint (success path)
- **User Story 2 (P1)**: Builds on User Story 1's `ExecuteAsync`/`DELETE` action (same method, `"all"` branch) — not independently implementable in isolation, but independently *testable* per its Independent Test criterion, and contributes the mandatory Infrastructure cascade-delete proof no other story provides
- **User Story 3 (P2)**: Builds on User Story 1's `DELETE` action — same caveat as User Story 2
- **User Story 4 (P3)**: Builds on User Story 1's `DELETE` action; independent of User Story 3's content

### Within User Story 1

- T011-T012 (Domain tests) before T015-T016 (Domain implementation)
- T013-T014 (Application tests) before T017 (UseCase implementation)
- T006, T007, T008 (Interface/Input/Output) before T017 (references them)
- T015 before T016 (guard needs the field `DeleteKeepingPaidHistory` sets)
- T017, T018 before T019 (controller action); T019 before T020 (Api.Tests)

### Within User Stories 2/3/4

- T021 (written) before T023 (run + fix if needed); T022 before T024
- T025 before T027; T026 before T027
- T028 before T031; T029 before T032; T031 before T032; T030 before T033

### Parallel Opportunities

- T001, T002 (Foundational, different files) in parallel; T003 depends on T002, T004 depends on T001
- T006, T007, T008, T009 (Foundational Interface/Input/Output/Response) in parallel — 4 different files; T010 depends on T008 and T009
- T011 alongside T013 (different files: Domain test vs. Application test) once each is independently written
- T034, T035 (Polish) in parallel — different files

---

## Parallel Example: Foundational Phase

```bash
# All different files, no shared dependency:
Task: "Add _deletedAt field + IsDeleted()/GetDeletedAt() to RecurringExpense.cs"
Task: "Add RemoveAsync to IRecurringExpenseRepository.cs"
Task: "Create IDeleteRecurringExpenseUseCase.cs"
Task: "Create DeleteRecurringExpenseUseCaseInput.cs"
Task: "Create DeleteRecurringExpenseDataResponse.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 2: Foundational (`_deletedAt` column, migration, and the shared Input/Output/Response/Interface shapes)
2. Complete Phase 3: User Story 1 — `pending-only` deletion works end-to-end (Domain method, full UseCase, controller `200` path)
3. **STOP and VALIDATE**: Run `Domain.Tests`, `Application.Tests`, `Api.Tests`; confirm every US1 scenario passes

### Incremental Delivery

1. Foundational → `_deletedAt` column and shared shapes ready
2. User Story 1 → `pending-only` deletion works end-to-end, including the no-reactivation guarantee (MVP)
3. User Story 2 → `all`-scope deletion works end-to-end, with the cascade-delete proven against real PostgreSQL
4. User Story 3 → missing/invalid `scope` is rejected with the standard field-error envelope, nothing mutated
5. User Story 4 → any already-deleted or never-existing id is "not found" everywhere, forever, without breaking paid-history visibility in the Monthly Panel/payment flow
6. Polish → `500` safety net, full quickstart run

---

## Notes

- `[P]` tasks = different files, no dependencies
- `[Story]` label maps task to specific user story for traceability
- User Stories 2, 3, and 4 all extend the same `DeleteRecurringExpenseUseCase.ExecuteAsync`/`DELETE` action User Story 1 creates, by design — see the Organization note at the top
- No mocking library is used anywhere in this repository — any test double needed follows the existing hand-written pattern (`InMemoryRecurringExpenseRepository`, seeded `WebApplicationFactory`)
- Commit after each task or logical group
- Verify tests fail before implementing
- Stop at any checkpoint to validate a story independently
