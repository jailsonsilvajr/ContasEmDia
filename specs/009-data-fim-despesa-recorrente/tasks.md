---

description: "Task list for Data de Fim da Despesa Recorrente"
---

# Tasks: Data de Fim da Despesa Recorrente

**Input**: Design documents from `/specs/009-data-fim-despesa-recorrente/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contract.md, quickstart.md

**Tests**: Required by this project's constitution (Principle II — Test-First Development). `Domain.Tests`/`Application.Tests` (xUnit), `Api.Tests` (xUnit + `WebApplicationFactory<Program>`), and `Infrastructure.Tests` (xUnit against real PostgreSQL) tasks are written before the implementation task they verify, following Red-Green-Refactor — same pattern already used by `specs/007-edit-recurring-expense/tasks.md`. Vitest specs follow the same rule for the two Angular components and `recurring-expense-form.util.ts`.

**Organization**: Tasks are grouped by user story (spec.md priorities P1–P3). The `RecurringExpense` aggregate's constructor and `ChangeEndDate` method are shared by multiple stories by design (per data-model.md/research.md §5) — later phases extend the same Domain method an earlier phase creates, the same "later phase extends an earlier phase's file" pattern already used by `specs/007-edit-recurring-expense/tasks.md`, while each phase stays independently *testable* per its Independent Test criterion in spec.md.

**Design fidelity**: Every "Data de fim" markup/copy task MUST reproduce `design/Cadastro.dc.html`'s field (label, input, three validation messages) exactly, per Princípio VIII — `design/Editar.dc.html` does not have it yet (see T051).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Tasks that append scenarios to the same file are listed sequentially (not `[P]`) even when logically independent, to avoid edit conflicts
- Paths are relative to the repository root (`backend/...`, `frontend/...`, `design/...`)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization.

No setup tasks are required. This feature adds no new project — it extends the four backend projects (`Domain`, `Application`, `Infrastructure`, `Api`, and their `.Tests` counterparts) and the two Angular features (`cadastro-despesa-recorrente`, `editar-despesa-recorrente`) already scaffolded by features 001–008 (plan.md, "Project Structure").

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Thread `endDate` as a plain data field through every layer's shape (Application Input/Output, Api Request/Response, Infrastructure column/mapping, frontend model/util) *before* any Domain business rule exists for it. **No User Story's business behavior can be verified until this phase and User Story 1's Domain changes are both done** — Application/Api call sites that reference `RecurringExpense.GetEndDate()` are deliberately left broken until User Story 1 (T027) supplies it, consistent with this project's existing TDD red/green convention (tests — and here, a couple of call sites — are expected to fail to compile/pass until their paired implementation task lands).

- [X] T001 [P] Write failing Domain test for `ReferencePeriod.Next()` in `backend/Domain.Tests/ValueObjects/ReferencePeriodTests.cs`: returns the same year with `Month + 1` when `Month < 12`; returns `Year + 1`, `Month == 1` when `Month == 12` (research.md §1)
- [X] T002 [P] Implement `public ReferencePeriod Next()` in `backend/Domain/ValueObjects/ReferencePeriod.cs` (depends on T001)
- [X] T003 [P] Add `required string EndDate` to `CreateRecurringExpenseUseCaseInput` in `backend/Application/UseCases/CreateRecurringExpense/CreateRecurringExpenseUseCaseInput.cs` (data-model.md)
- [X] T004 [P] Add `required string EndDate` to `UpdateRecurringExpenseUseCaseInput` in `backend/Application/UseCases/UpdateRecurringExpense/UpdateRecurringExpenseUseCaseInput.cs` (data-model.md)
- [X] T005 [P] Add `EndDate: DateOnly?` to `CreateRecurringExpenseUseCaseOutput`'s private constructor/properties and an `endDate: DateOnly` parameter to its `Success(...)` static factory (`Failure` keeps passing `endDate: null`) in `backend/Application/UseCases/CreateRecurringExpense/CreateRecurringExpenseUseCaseOutput.cs` (data-model.md) — breaks `CreateRecurringExpenseUseCase.cs`'s existing `Success(...)` call until T027 adds the new argument; expected
- [X] T006 [P] Add `DateOnly EndDate` to the `RecurringExpenseData` positional record in `backend/Application/UseCases/GetRecurringExpenseById/GetRecurringExpenseByIdUseCaseOutput.cs` (data-model.md) — breaks both existing `new RecurringExpenseData(...)` call sites (`GetRecurringExpenseByIdUseCase.cs`, `UpdateRecurringExpenseUseCase.cs`) until T027/T038 add the new argument; expected
- [X] T007 [P] Add `[Required(ErrorMessage = "Data de fim é obrigatória.")] public string? EndDate { get; init; }` to `CreateRecurringExpenseDataRequest` in `backend/Api/Requests/CreateRecurringExpenseDataRequest.cs`, placed after `StartDate` (contracts/api-contract.md)
- [X] T008 [P] Add `[Required(ErrorMessage = "Data de fim é obrigatória.")] public string? EndDate { get; init; }` to `UpdateRecurringExpenseDataRequest` in `backend/Api/Requests/UpdateRecurringExpenseDataRequest.cs`, placed after `StartDate` (contracts/api-contract.md)
- [X] T009 [P] Add `DateOnly EndDate` to the `CreateRecurringExpenseDataResponse` record in `backend/Api/Responses/CreateRecurringExpenseDataResponse.cs`, placed after `StartDate` (contracts/api-contract.md)
- [X] T010 [P] Add `DateOnly EndDate` to the `RecurringExpenseDataResponse` record in `backend/Api/Responses/RecurringExpenseDataResponse.cs`, placed after `StartDate` (contracts/api-contract.md; shared by `GET`/`PUT`)
- [X] T011 Map `request.EndDate!` → `Input.EndDate` in `CreateRecurringExpenseDataRequestMapping.ToUseCaseInput()` (`backend/Api/Mappings/CreateRecurringExpenseDataRequestMapping.cs`) (depends on T003, T007)
- [X] T012 Map `request.EndDate!` → `Input.EndDate` in `UpdateRecurringExpenseDataRequestMapping.ToUseCaseInput(id)` (`backend/Api/Mappings/UpdateRecurringExpenseDataRequestMapping.cs`) (depends on T004, T008)
- [X] T013 Map `output.EndDate!.Value` → `EndDate` in `CreateRecurringExpenseDataResponseMapping.ToDataResponse()` (`backend/Api/Mappings/CreateRecurringExpenseDataResponseMapping.cs`) (depends on T005, T009)
- [X] T014 Map `data.EndDate` → `EndDate` in `RecurringExpenseDataResponseMapping.ToDataResponse()` (`backend/Api/Mappings/RecurringExpenseDataResponseMapping.cs`) (depends on T006, T010)
- [X] T015 [P] Add `_endDate` mapping to `RecurringExpenseConfigurations.Configure()` in `backend/Infrastructure/Configs/RecurringExpenseConfigurations.cs`: `builder.Property<CalendarDate>("_endDate").HasColumnName("EndDate").HasConversion(vo => vo.GetValue(), value => new CalendarDate(value)).HasColumnType("date").IsRequired();`, placed right after the `_startDate` block (data-model.md)
- [X] T016 Create EF Core migration `AddRecurringExpenseEndDate` in `backend/Infrastructure/Migrations/` (`dotnet ef migrations add AddRecurringExpenseEndDate --project Infrastructure --startup-project Api` or equivalent already-used command for this repo): `AddColumn<DateOnly>("EndDate", "RecurringExpenses", type: "date", nullable: true)` → `migrationBuilder.Sql("UPDATE \"RecurringExpenses\" SET \"EndDate\" = \"StartDate\" + INTERVAL '1 year' WHERE \"EndDate\" IS NULL;")` → `AlterColumn<DateOnly>("EndDate", "RecurringExpenses", type: "date", nullable: false, oldNullable: true)` (research.md §8, FR-011/User Story 4; depends on T015)
- [X] T017 [P] Add `endDate: string` to `CreateRecurringExpenseRequest`, `CreateRecurringExpenseResponse`, and `RecurringExpenseDetailResponse` in `frontend/src/app/features/despesa-recorrente/despesa-recorrente.model.ts` (`UpdateRecurringExpenseRequest` inherits it automatically via `Omit<CreateRecurringExpenseRequest, 'frequency'>`) (data-model.md) — breaks `cadastro-despesa-recorrente.component.ts`'s existing payload literal (missing required property) until T030 adds it; expected
- [X] T018 [P] Write failing Vitest tests in `frontend/src/app/shared/recurring-expense-form.util.spec.ts`: `addYearsIso('2026-09-01', 1) === '2027-09-01'` (and a leap-year edge case, e.g. `addYearsIso('2028-02-29', 1) === '2029-03-01'`); `getDataFimError` returns "Data de fim é obrigatória." when empty, "A data de fim deve ser posterior à data de início." when `dataFim <= dataInicio`, "A vigência não pode ultrapassar 1 ano a partir da data de início." when `dataFim > addYearsIso(dataInicio, 1)`, and `null` when valid (mirrors the existing `getDataInicioError` spec pattern)
- [X] T019 [P] Implement `addYearsIso(iso: string, years: number): string` and `getDataFimError(dataInicio: string, dataFim: string): string | null` in `frontend/src/app/shared/recurring-expense-form.util.ts`, same logic already prototyped in `design/Cadastro.dc.html` (lines 289-293, 347-352) (depends on T018)

**Checkpoint**: Every layer's data shape now carries `endDate`, and the EF migration exists. `dotnet build backend/ContasEmDia.sln` will **not** fully succeed yet (T005/T006's breaking call sites) — this is intentional and resolved by User Story 1 below, the same TDD red state this repo's other `tasks.md` files already treat as normal for not-yet-implemented behavior. `npm test` passes for `recurring-expense-form.util.spec.ts`.

---

## Phase 3: User Story 1 - Cadastrar despesa recorrente com data de fim (Priority: P1) 🎯 MVP

**Goal**: Registering a recurring expense with a valid `startDate`/`endDate` immediately generates one occurrence per competência of the vigência (current month through `endDate`'s month, inclusive) — regardless of `Active`/`Paused` status — and every required/cross-field validation blocks the submission before anything is saved.

**Independent Test**: Register a recurring expense with valid start/end dates and confirm every monthly occurrence between the current month and the end month appears immediately in the monthly panel, without navigating month by month.

### Tests for User Story 1

> Write these tests first; they must fail (the Domain/Application changes below do not exist yet) before the implementation tasks.

- [X] T020 [US1] Write failing Domain tests in `backend/Domain.Tests/Aggregates/RecurringExpenseTests.cs`: constructing with `endDate <= startDate` throws `DomainRuleViolationException("A data de fim deve ser posterior à data de início.")`; constructing with `endDate > startDate.AddYears(1)` throws `DomainRuleViolationException("A vigência não pode ultrapassar 1 ano a partir da data de início.")`; constructing with `endDate`'s competência earlier than `currentReferencePeriod` throws `DomainRuleViolationException("A data de fim não pode estar no passado.")` (data-model.md, research.md §2/§3)
- [X] T021 [US1] Add tests to `RecurringExpenseTests.cs`: constructing with `startDate` in the current competência and `endDate` N competências ahead (N ≤ 12) generates exactly N+1 occurrences (one per competência, current through `endDate` inclusive) for both `status: Active` and `status: Paused` (spec.md US1 AC4/AC5, EC05); constructing with `startDate` still in the future generates zero occurrences, unchanged from today's behavior (EC06) (same file, sequential)
- [X] T022 [US1] Write failing Application tests in `backend/Application.Tests/UseCases/CreateRecurringExpense/CreateRecurringExpenseUseCaseTests.cs`: malformed `EndDate` text (not `yyyy-MM-dd`) returns `Failure` with `FieldError("endDate", "Data de fim inválida.")` (mirrors the existing malformed-`StartDate` test); each of the three Domain rule violations thrown by the `RecurringExpense` constructor is caught and returned as `Failure` with a single `FieldError("endDate", <domain message>)`, never as an unhandled exception (research.md §7)
- [X] T023 [US1] Add a test to `CreateRecurringExpenseUseCaseTests.cs`: a valid create with `endDate` N competências ahead returns `Success` with `EndDate` set and `Occurrences.Count == N + 1`, for both `Active` and `Paused` status (spec.md US1 AC4/AC5) (same file, sequential)

### Implementation for User Story 1

- [X] T024 [US1] In `backend/Domain/Aggregates/RecurringExpense.cs`: add `private CalendarDate _endDate`; add `CalendarDate endDate` as a new parameter to the public constructor (immediately after `startDate`) and to the private EF-reconstruction constructor (assigned directly to `_endDate`, no validation — reidrated data is already trusted); add `public CalendarDate GetEndDate() => _endDate;` (data-model.md)
- [X] T025 [US1] In `backend/Domain/Aggregates/RecurringExpense.cs`: implement `private static void ValidateVigencia(CalendarDate startDate, CalendarDate endDate)` (throws the "posterior à início"/"teto de 1 ano" `DomainRuleViolationException`s) and `private static void ValidateEndDateNotInPast(CalendarDate endDate, ReferencePeriod currentReferencePeriod)` (throws the "não pode estar no passado" one), exact PT-BR messages per data-model.md; call both from the public constructor immediately before assigning `_endDate = endDate` (depends on T020, T024)
- [X] T026 [US1] In `backend/Domain/Aggregates/RecurringExpense.cs`: extract `GenerateOccurrenceForCurrentPeriodIfDue`'s body (dropping its `currentReferencePeriod >= startPeriod` guard) into `private void GenerateOccurrenceForPeriodIfMissing(ReferencePeriod period)`; add `private void GenerateOccurrencesForVigencia(ReferencePeriod currentReferencePeriod)`: no-op when `currentReferencePeriod < ReferencePeriod.FromDate(_startDate.GetValue())` (EC06), otherwise loop from `currentReferencePeriod` to `ReferencePeriod.FromDate(_endDate.GetValue())` inclusive via `.Next()`, calling `GenerateOccurrenceForPeriodIfMissing` each iteration; replace the constructor's `if (status == Active) GenerateOccurrenceForCurrentPeriodIfDue(...)` block with an unconditional call to `GenerateOccurrencesForVigencia(currentReferencePeriod)`; update `Reactivate` to call `GenerateOccurrenceForPeriodIfMissing(currentReferencePeriod)` directly, re-adding the `currentReferencePeriod >= startPeriod` guard inline so its external behavior is unchanged; delete the now-unused `GenerateOccurrenceForCurrentPeriodIfDue` (research.md §4; depends on T021, T024, T025, T002)
- [X] T027 [US1] In `backend/Application/UseCases/CreateRecurringExpense/CreateRecurringExpenseUseCase.cs`: parse `input.EndDate` the same way as `input.StartDate` (`DateOnly.TryParseExact`, `FieldError("endDate", "Data de fim inválida.")` on failure); wrap the `new RecurringExpense(...)` call in `try/catch (DomainRuleViolationException ex)` returning `Failure([new FieldError("endDate", ex.Message)])`; pass `endDate!` into the constructor (right after `startDate!`) and `recurringExpense.GetEndDate().GetValue()` into `Success(...)`'s new `endDate` argument (research.md §7; depends on T022, T003, T005, T024, T025). Also update `backend/Application/UseCases/GetRecurringExpenseById/GetRecurringExpenseByIdUseCase.cs`'s `new RecurringExpenseData(...)` call to pass `recurringExpense.GetEndDate().GetValue()` — this is the first point `GetEndDate()` exists, resolving T006's other pending call site
- [X] T028 [US1] Add Api.Tests to `backend/Api.Tests/Controllers/RecurringExpensesControllerTests.cs`: `POST` missing `endDate` → `400` with `FieldError("endDate", "Data de fim é obrigatória.")`; `POST` with `endDate` equal to or before `startDate`, with a vigência exceeding 1 year, and with `endDate`'s competência before the current one → each returns `400` with the matching message from contracts/api-contract.md; `POST` with a valid multi-competência vigência returns `201` with `occurrences.length` equal to the vigência's competência count and `endDate` echoed back, for both `status: "Active"` and `status: "Paused"` (contracts/api-contract.md; depends on T027, T007, T009, T011, T013)

### Frontend for User Story 1

- [X] T029 [US1] Write failing tests in `frontend/src/app/features/despesa-recorrente/cadastro-despesa-recorrente/cadastro-despesa-recorrente.component.spec.ts`: submitting without `dataFim` is blocked with the required-field error; the three client-side `dataFim` validations (obrigatória/posterior/teto) surface via `showDataFimError`/`dataFimErrorMsg`; `isFormValid` is `false` while `dataFimError()` is non-null; a successful submit includes `endDate: this.dataFim()` in the `CreateRecurringExpenseRequest` payload (mirrors the existing `dataInicio` spec cases)
- [X] T030 [US1] In `cadastro-despesa-recorrente.component.ts`: add `readonly dataFim = signal('')`; add `dataFim: boolean` to `touched`'s initial value and to its reset in `onNovaDespesa()`; add `onDataFimInput(event)`/`onDataFimBlur()` handlers (mirrors `onDataInicioInput`/`onDataInicioBlur`); add `readonly dataFimError = computed(() => getDataFimError(this.dataInicio(), this.dataFim()))` and `readonly showDataFimError = computed(...)` (same pattern as `dataInicio`); include `!this.dataFimError()` in `isFormValid`; include `this.dataFim().trim() !== ''` in `hasUnsavedData`; add `endDate: this.dataFim()` to the `CreateRecurringExpenseRequest` payload built in `onSalvar()`; add `'endDate'` to the `ApiField` type and `KNOWN_API_FIELDS` (depends on T029, T019, T017)
- [X] T031 [US1] Add the "Data de fim" field to `cadastro-despesa-recorrente.component.html`, reproducing `design/Cadastro.dc.html`'s field markup (label, `type="date"` input, error message span) exactly, placed immediately after "Data de início" (depends on T030)

**Checkpoint**: T020-T023 (Domain/Application) and T028-T029 pass; `dotnet build backend/ContasEmDia.sln` succeeds again. User Story 1 is fully functional end-to-end (cadastro screen → API → DB) and independently testable.

---

## Phase 4: User Story 2 - Estender a vigência editando a data de fim (Priority: P2)

**Goal**: Editing `endDate` to a later competência creates occurrences for every newly-covered competência — including ones already in the past relative to the previous `endDate` (retroactive gap fill) — without duplicating any existing occurrence.

**Independent Test**: Edit an existing recurring expense's `endDate` to a later date and confirm occurrences for the newly-covered competências appear, without duplicating the ones that already existed.

### Tests for User Story 2

- [X] T032 [US2] Add tests to `backend/Domain.Tests/Aggregates/RecurringExpenseTests.cs`: `ChangeEndDate(newEndDate, currentReferencePeriod)` with `newEndDate`'s competência later than the current `_endDate`'s updates `GetEndDate()` and adds one occurrence per competência strictly after the old `endDate` through the new one (inclusive), leaving every pre-existing occurrence untouched; calling it again with the same later date is idempotent (no duplicate occurrences, reusing the non-duplication check already built into `GenerateOccurrenceForPeriodIfMissing`); extending when the previous `endDate`'s competência is already in the past relative to `currentReferencePeriod` fills every competência in the gap, including past ones (spec.md US2 AC1/AC2/AC4, clarification Q2)
- [X] T033 [US2] Add tests to `RecurringExpenseTests.cs`: `ChangeEndDate` throws the same three `ValidateVigencia`/`ValidateEndDateNotInPast` exceptions as the constructor without mutating `_endDate` or `_occurrences` (spec.md US2 AC3, FR-014); `ChangeEndDate` to the same competência as the current `endDate` (different day) updates `GetEndDate()` but touches no occurrence (EC03) (same file, sequential)
- [X] T034 [US2] Add tests to `RecurringExpenseTests.cs`: `ChangeStartDate(newStartDate)` now throws `DomainRuleViolationException` (via `ValidateVigencia`) when the new `startDate` would no longer be strictly before `_endDate`, or when the resulting vigência would exceed 1 year — and never throws `ValidateEndDateNotInPast` regardless of how far in the past `_endDate` already is (FR-012, research.md §3) (same file, sequential)
- [X] T035 [US2] Add tests to `backend/Application.Tests/UseCases/UpdateRecurringExpense/UpdateRecurringExpenseUseCaseTests.cs`: extending `endDate` returns `Success` with the new occurrences visible via the aggregate's `GetOccurrences()`; a `ChangeEndDate` domain violation returns `Failure` with `FieldError("endDate", <message>)` without the repository's `UpdateAsync` ever being called; a `ChangeStartDate` domain violation returns `Failure` with `FieldError("startDate", <message>)`, also without calling `UpdateAsync` (spec.md US2 AC1-AC3, FR-009/FR-012, research.md §7)

### Implementation for User Story 2

- [X] T036 [US2] In `backend/Domain/Aggregates/RecurringExpense.cs`: implement `public void ChangeEndDate(CalendarDate newEndDate, ReferencePeriod currentReferencePeriod)` — call `ValidateVigencia(_startDate, newEndDate)` then `ValidateEndDateNotInPast(newEndDate, currentReferencePeriod)`; compare `newPeriod = ReferencePeriod.FromDate(newEndDate.GetValue())` against `oldPeriod = ReferencePeriod.FromDate(_endDate.GetValue())`: when `newPeriod > oldPeriod`, update `_endDate` then loop `oldPeriod.Next()` through `newPeriod` inclusive calling `GenerateOccurrenceForPeriodIfMissing`; when `newPeriod == oldPeriod`, just update `_endDate` (EC03); the `newPeriod < oldPeriod` (reduction) branch is a no-op placeholder for now, completed by T045 (research.md §5; depends on T032, T033, T025, T026)
- [X] T037 [US2] In `backend/Domain/Aggregates/RecurringExpense.cs`: update `ChangeStartDate(CalendarDate newStartDate)` to call `ValidateVigencia(newStartDate, _endDate)` before assigning `_startDate = newStartDate` (FR-012; depends on T034, T025)
- [X] T038 [US2] In `backend/Application/UseCases/UpdateRecurringExpense/UpdateRecurringExpenseUseCase.cs`: parse `input.EndDate` the same way as `input.StartDate` (`FieldError("endDate", "Data de fim inválida.")` on malformed text); after the existing "safe field" applications (name/category/monthlyAmount/dueDay/note), introduce a `crossFieldErrors` list; replace the direct `recurringExpense.ChangeStartDate(startDate)` call with `try/catch (DomainRuleViolationException ex)` adding `FieldError("startDate", ex.Message)` to it; add a guarded `recurringExpense.ChangeEndDate(endDate!, currentReferencePeriod)` call adding `FieldError("endDate", ex.Message)` on failure; if `crossFieldErrors.Count > 0`, `return Failure(crossFieldErrors)` before the status-change branch and before `UpdateAsync` is ever called (FR-009/FR-012, data-model.md); update this method's `new RecurringExpenseData(...)` construction to pass `recurringExpense.GetEndDate().GetValue()` — resolving T006's other pending call site (depends on T035, T004, T006, T036, T037)
- [X] T039 [US2] Add an Api.Test to `backend/Api.Tests/Controllers/RecurringExpenseEditingTests.cs`: `PUT` extending `endDate` to a later competência → `200` with the updated `endDate`, and the seeded aggregate reference shows the new occurrences added — including a retroactive-gap scenario where the previous `endDate`'s competência was already in the past (contracts/api-contract.md, spec.md US2 AC1/AC4; depends on T038)
- [X] T040 [US2] Add Api.Tests to `RecurringExpenseEditingTests.cs`: `PUT` with `endDate` violating the 1-year teto against the current `startDate` → `400` with `FieldError("endDate", ...)`; `PUT` with a new `startDate` that would no longer be before the current `endDate` → `400` with `FieldError("startDate", ...)` — neither case persists any field (contracts/api-contract.md, spec.md US2 AC3, EC12; depends on T038)

### Frontend for User Story 2

- [X] T041 [US2] Add `dataFim` support to `editar-despesa-recorrente.component.ts`/`.html`/`.spec.ts`, mirroring T029-T031's signal/handlers/`dataFimError`/`showDataFimError`/`isFormValid`/`hasUnsavedData`/`KNOWN_API_FIELDS`/payload additions in the cadastro component, plus pre-filling `dataFim: detail.endDate` in the component's existing load/patch-on-init logic (data-model.md; depends on T019, T017)

**Checkpoint**: T032-T035 pass; T039-T040 pass. Extending a vigência — including retroactive gap-fill — works end-to-end and is independently testable.

---

## Phase 5: User Story 3 - Reduzir a vigência editando a data de fim (Priority: P2)

**Goal**: Editing `endDate` to an earlier competência removes occurrences for competências that left the vigência, unless any of them is already paid — in which case the whole edit is rejected, nothing persisted.

**Independent Test**: Edit an existing recurring expense (no paid occurrence in the range being dropped) with an earlier `endDate` and confirm the occurrences of the excluded competências disappear.

### Tests for User Story 3

- [X] T042 [US3] Add tests to `backend/Domain.Tests/Aggregates/RecurringExpenseTests.cs`: `ChangeEndDate` to an earlier competência (no paid occurrence in the removed range) updates `GetEndDate()` and removes every occurrence whose competência is after the new `endDate`, leaving the rest untouched (spec.md US3 AC1); `ChangeEndDate` to an earlier competência where at least one occurrence in the removed range already has `OccurrenceStatusType.Paid` throws `DomainRuleViolationException` and leaves both `_endDate` and `_occurrences` completely unchanged (spec.md US3 AC2, FR-009) (research.md §5)
- [X] T043 [US3] Add tests to `backend/Application.Tests/UseCases/UpdateRecurringExpense/UpdateRecurringExpenseUseCaseTests.cs`: reducing `endDate` (no paid occurrence in range) returns `Success` with the trimmed occurrence list; reducing `endDate` where a paid occurrence would be excluded returns `Failure` with `FieldError("endDate", <block message>)`, `UpdateAsync` never called, aggregate state unaffected (spec.md US3 AC1/AC2; same file as T035, sequential)
- [X] T044 [US3] Write a failing Infrastructure.Tests regression test in `backend/Infrastructure.Tests/Repositories/RecurringExpenseRepositoryTests.cs`: persist a recurring expense with 3+ occurrences via `AddAsync` against real PostgreSQL, fetch it via `GetByIdAsync`, call `ChangeEndDate` to reduce the vigência (removing ≥1 occurrence), call `UpdateAsync`, then re-fetch via `GetByIdAsync` (or query `Occurrences` directly) and confirm the removed occurrence rows are actually gone from the database — not merely absent from the in-memory `_occurrences` list (research.md §6, plan.md Principle VII)

### Implementation for User Story 3

- [X] T045 [US3] In `backend/Domain/Aggregates/RecurringExpense.cs`, complete `ChangeEndDate`'s `newPeriod < oldPeriod` (reduction) branch left as a placeholder by T036: before mutating any state, check whether any occurrence with competência `> newPeriod` has `GetStatus().GetValue() == OccurrenceStatusType.Paid`; if so, `throw new DomainRuleViolationException(...)` with a dedicated block message and change nothing; otherwise update `_endDate` then `_occurrences.RemoveAll(...)` for every occurrence with competência `> newPeriod` (research.md §5; depends on T042, T036)
- [X] T046 [US3] Run T044 against the local PostgreSQL instance (`dotnet test --filter FullyQualifiedName~Infrastructure.Tests`, dev DB up via `deploy/`); if the removed rows are not actually `DELETE`d, fix `RecurringExpenseRepository.UpdateAsync` in `backend/Infrastructure/Repositories/RecurringExpenseRepository.cs` by explicitly marking any tracked `Occurrence` entry no longer present in `recurringExpense.GetOccurrences()` as `EntityState.Deleted` before `SaveChangesAsync()` — mirroring the existing `EntityState.Added` handling already in that method (research.md §6; depends on T044, T045)
- [X] T047 [US3] Add Api.Tests to `backend/Api.Tests/Controllers/RecurringExpenseEditingTests.cs`: `PUT` reducing `endDate` (no paid occurrence in range) → `200`, seeded aggregate shows the excluded occurrences gone; `PUT` reducing `endDate` where a paid occurrence would be excluded → `400` with `FieldError("endDate", <block message>)`, nothing changed; `PUT` reducing `endDate` to a competência before the current month (even with no paid occurrence in range) → `400` with `FieldError("endDate", "A data de fim não pode estar no passado.")` (contracts/api-contract.md, spec.md US3 AC1-AC3, FR-014; depends on T045)

**Checkpoint**: T042-T044 pass (T044 verified against real PostgreSQL). Reducing a vigência — including the paid-occurrence guard and the past-competência block — works end-to-end and is independently testable.

---

## Phase 6: User Story 4 - Despesas já cadastradas antes desta mudança continuam consistentes (Priority: P3)

**Goal**: Recurring expenses that existed before this feature's migration end up with `endDate = startDate + 1 year`, with no occurrence generated retroactively just because of the data migration.

**Independent Test**: Query a recurring expense that existed before this change and confirm its `endDate` equals `startDate` plus 1 year, with no new occurrence generated by the update itself.

### Tests for User Story 4

- [X] T048 [US4] Add a test to `backend/Infrastructure.Tests/Repositories/RecurringExpenseRepositoryTests.cs` (or a dedicated migration-focused test in the same project, matching whatever pattern that project already uses for schema-level assertions): seed a `RecurringExpenses` row directly via raw SQL with a known `StartDate` and simulate the pre-migration state (no `EndDate`, or apply against a snapshot taken before T016's migration), apply the migration, then confirm `EndDate == StartDate.AddYears(1)` and that `Occurrences` gained no new row for that recurring expense solely from the migration (spec.md US4 AC1/AC2, FR-011)

### Implementation for User Story 4

- [X] T049 [US4] Verify the migration from T016 already satisfies T048 as written (its backfill `Sql(...)` sets `EndDate = StartDate + 1 year` for every pre-existing row, and nothing in the migration inserts into `Occurrences`); if T048 fails, adjust T016's migration accordingly (research.md §8; depends on T016, T048)
- [X] T050 [US4] Manually verify, per `quickstart.md` User Story 4, that `GET /api/v1/recurring-expenses/{id}` for a recurring expense seeded before this feature's migration returns `endDate` equal to `startDate` plus 1 year (depends on T049)

**Checkpoint**: T048 passes. Pre-existing data is consistent with the new invariant without any user action.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Close the loop on the one design-fidelity gap noted in research.md §9 and run final end-to-end verification.

- [X] T051 [P] Update `design/Editar.dc.html` to add the "Data de fim" field (mirroring `design/Cadastro.dc.html`'s field markup, copy, and the three validation messages), per research.md §9 and plan.md's Project Structure note — keeps the edit screen's design source of truth in sync with T041's implementation
- [X] T052 [P] Verify no unused code remains in `backend/Domain/Aggregates/RecurringExpense.cs`: confirm `GenerateOccurrenceForCurrentPeriodIfDue` was fully removed (T026) with no leftover references anywhere in the file
- [X] T053 Execute `quickstart.md`'s full walkthrough (User Stories 1-4 + the Infrastructure regression check) end-to-end against a locally running `dotnet run` instance with real PostgreSQL and `npm start`, confirming `dotnet test` (all four backend test projects) and `npm test` pass in full

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No tasks — nothing to scaffold
- **Foundational (Phase 2)**: No dependencies — start immediately — establishes shape across every layer; two call sites remain intentionally broken until User Story 1
- **User Story 1 (Phase 3)**: Depends on Foundational — delivers the base cadastro flow (MVP) and the Domain aggregate's `_endDate`/`GetEndDate()`/validation/batch-generation, which every later phase reuses
- **User Story 2 (Phase 4)**: Depends on User Story 1 (extends `ChangeEndDate`'s extend/no-op branches, `ChangeStartDate`, and `UpdateRecurringExpenseUseCase`)
- **User Story 3 (Phase 5)**: Depends on User Story 2 (completes `ChangeEndDate`'s reduction branch left as a placeholder by T036)
- **User Story 4 (Phase 6)**: Depends on Foundational's migration (T016) only — independent of User Stories 1-3's Domain/Application work
- **Polish (Phase 7)**: Depends on all four User Stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends only on Foundational — delivers the base cadastro endpoint and screen (MVP)
- **User Story 2 (P2)**: Builds on User Story 1's `ChangeEndDate`/`ValidateVigencia`/`ValidateEndDateNotInPast` — not independently implementable in isolation, but independently *testable* per its Independent Test criterion
- **User Story 3 (P3)**: Builds on User Story 2's `ChangeEndDate` (same method, reduction branch) — same caveat as User Story 2
- **User Story 4 (P3)**: Independent of User Stories 1-3 — only needs the Foundational migration (T016)

### Within User Story 1

- T020-T021 (Domain tests) before T024-T026 (Domain implementation)
- T022-T023 (Application tests) before T027 (UseCase implementation)
- T024, T025 before T026 (loop needs both the field and the validators)
- T027 before T028 (Api.Tests)
- T029 before T030-T031 (frontend tests before implementation)

### Within User Stories 2/3

- T032-T034 before T036-T037; T036-T037 before T038; T038 before T039-T040
- T042 before T045; T044 (written) before T046 (run + fix); T045 before T047

### Parallel Opportunities

- T001 alongside any Foundational Application/Api task — different files (T002 depends on T001 itself)
- T003, T004, T005, T006, T007, T008, T009, T010 (Foundational Input/Output/Request/Response shapes) in parallel — 8 different files
- T015 in parallel with the above; T016 depends on T015 only
- T017, T018 in parallel with all backend Foundational tasks — different stack entirely
- T020, T022 (US1 tests, different files) in parallel
- T032, T034 (US2 Domain tests, same file — NOT parallel; listed sequential)
- T051, T052 (Polish) in parallel

---

## Parallel Example: Foundational Phase

```bash
# All different files, no shared dependency:
Task: "Add required string EndDate to CreateRecurringExpenseUseCaseInput"
Task: "Add required string EndDate to UpdateRecurringExpenseUseCaseInput"
Task: "Add EndDate to CreateRecurringExpenseDataRequest with [Required] PT-BR message"
Task: "Add EndDate to UpdateRecurringExpenseDataRequest with [Required] PT-BR message"
Task: "Add EndDate to CreateRecurringExpenseDataResponse"
Task: "Add EndDate to RecurringExpenseDataResponse"
Task: "Add endDate to despesa-recorrente.model.ts request/response interfaces"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 2: Foundational (data shapes + migration threaded through every layer)
2. Complete Phase 3: User Story 1 — cadastro generates the full vigência's occurrences, for both Active and Paused
3. **STOP and VALIDATE**: Run `Domain.Tests`, `Application.Tests`, `Api.Tests`; confirm every US1 scenario passes; try the cadastro screen manually

### Incremental Delivery

1. Foundational → shapes and migration ready, one Domain getter still missing on purpose
2. User Story 1 → cadastro works end-to-end with the full vigência generated at once (MVP)
3. User Story 2 → extending `endDate` on an existing despesa creates the newly-covered occurrences, including retroactive gap-fill
4. User Story 3 → reducing `endDate` removes newly-excluded occurrences, guarded by the paid-occurrence check; the Infrastructure `DELETE` regression is confirmed against real PostgreSQL
5. User Story 4 → pre-existing despesas are confirmed consistent after the migration's backfill
6. Polish → `design/Editar.dc.html` catches up to the implemented edit screen, full quickstart run

---

## Notes

- `[P]` tasks = different files, no dependencies
- `[Story]` label maps task to specific user story for traceability
- User Stories 2 and 3 both extend the same `ChangeEndDate` method User Story 1's sibling validators make possible — by design, see the Organization note at the top
- Two Foundational call sites (`CreateRecurringExpenseUseCase.cs`'s `Success(...)` call, and both `new RecurringExpenseData(...)` call sites) are left deliberately broken until T027/T038 — same TDD red state already used throughout this repo's other `tasks.md` files
- Commit after each task or logical group
- No mocking library is used anywhere in this repository — any test double needed follows the existing hand-written pattern (`InMemoryRecurringExpenseRepository`, seeded `WebApplicationFactory`)
- Verify tests fail before implementing
- Stop at any checkpoint to validate a story independently
