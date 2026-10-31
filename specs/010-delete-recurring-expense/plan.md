# Implementation Plan: Deletar Despesa Recorrente (Backend)

**Branch**: `010-delete-recurring-expense` | **Date**: 2026-10-31 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/010-delete-recurring-expense/spec.md`,
grounded in the pre-existing technical refinement
`refinements/backend/deletar-despesa-recorrente.md`.

## Summary

Add the ability to delete an already-registered recurring expense in one of
two mandatory scopes: `pending-only` (removes every unpaid occurrence,
preserves every paid occurrence untouched, and permanently blocks any future
occurrence generation for that expense — the expense row survives
internally) or `all` (removes the recurring expense and every occurrence,
paid or not, so nothing about it remains discoverable). The technical
approach — already fully reasoned in the backend refinement doc — adds one
new Domain business method (`DeleteKeepingPaidHistory()`), one new
Domain-orthogonal persisted field (`_deletedAt`), one new Infrastructure
repository method (`RemoveAsync`) plus a `GetByIdAsync` filter adjustment, one
new Application UseCase (`DeleteRecurringExpenseUseCase`), and one new `DELETE`
action on the existing `RecurringExpensesController`, reusing every existing
response-envelope, mapping, and exception-middleware convention.

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: ASP.NET Core 10 (Web API, Swagger/OpenAPI), Entity
Framework Core 10, Npgsql (PostgreSQL provider) — all already in use; no new
dependency introduced.

**Storage**: PostgreSQL via EF Core 10, same `ContasEmDiaDbContext` /
`RecurringExpenses` + `Occurrences` tables already used by Create/Edit.

**Testing**: xUnit across `Domain.Tests`, `Application.Tests`,
`Infrastructure.Tests` (Testcontainers-backed `PostgreSqlContainerFixture`),
and `Api.Tests` (`WebApplicationFactory`) — same four test projects already
in the solution, per Principle II.

**Target Platform**: Linux-hosted ASP.NET Core API (trusted dev environment
only, per Principle IV's current-phase exception); no new deployment target.

**Project Type**: Web application — backend-only slice of the existing
`backend/` .NET solution (Domain / Application / Infrastructure / Api
layers). Frontend is explicitly out of scope (separate frontend feature per
spec Assumptions).

**Performance Goals**: No new goal beyond existing endpoints' implicit
single-request, single-aggregate scale; deleting one recurring expense is a
single-row (or small-collection) operation, not a batch/background job.

**Constraints**: Deletion must be definitive and irreversible (no undo, no
soft-reactivation) per FR-003/FR-004/FR-007; a `pending-only` deletion MUST
NOT remove or alter any paid occurrence (FR-003); it MUST NOT break the
Monthly Panel's or payment flow's ability to keep finding paid history of a
`pending-only`-deleted expense (FR-008).

**Scale/Scope**: One new endpoint action, one new UseCase, one new Domain
method + field, one new repository method, one new EF Core migration. No new
screens (frontend is a separate feature).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. API-First Backend/Frontend Separation** — PASS. The new behavior is
  exposed only via a new `DELETE /api/v1/recurring-expenses/{id}` action on
  the existing versioned controller; no direct DB/internal access is added.
- **II. Test-First Development** — PASS (gate honored at `/speckit-tasks` +
  `/speckit-implement` time). One test per declared response scenario
  (`200`, `400`, `404`, `500`) is required for the new action, plus Domain
  tests for `DeleteKeepingPaidHistory()`, Application tests for the new
  UseCase, and an `Infrastructure.Tests` integration test confirming
  cascade-delete for the `all` scope (flagged as unconfirmed-by-reading in
  the refinement doc — must be proven by a real Postgres-container test, not
  assumed).
- **III. Type Safety & Static Analysis** — PASS. No `any`/suppressions;
  pure C# with nullable reference types, consistent with existing code.
- **IV. Secure Handling of Financial Data** — PASS under the existing
  current-phase exception (no auth/CORS yet, same as every other endpoint);
  no new exception requested.
- **V. Simplicity & Incremental Delivery** — PASS. Reuses `UpdateAsync`'s
  existing orphan-removal mechanism for the `pending-only` scope (same
  mechanism `ChangeEndDate` already relies on) instead of inventing a new
  persistence path; no new Value Object is introduced for `_deletedAt`
  (reuses `CalendarDate?`, already nullable elsewhere e.g.
  `Occurrence._paymentDate`); the `all`/`pending-only` scope is kept as a
  plain validated string in the Application layer rather than a new Domain
  enum, because the Domain layer never needs to know which scope was
  requested (see data-model.md).
- **VI. Domain-Driven Design in the Domain Layer** — PASS. The new
  `DeleteKeepingPaidHistory()` method is a business-intent method (not a
  setter) on the `RecurringExpense` Aggregate; `_deletedAt` is read only via
  `IsDeleted()`/`GetDeletedAt()`, never exposed as a settable property; the
  Aggregate still requires construction through its constructors; no new
  Domain exception message is needed for this method (it never blocks).
- **VII. Infrastructure Layer Implementation** — PASS. `RemoveAsync` is
  added to `IRecurringExpenseRepository` and implemented in
  `RecurringExpenseRepository`; the `pending-only` scope needs zero new
  Infrastructure code beyond the existing `UpdateAsync`; a new EF Core
  migration is additive only (no edit to prior migrations); access stays
  through `RepositoryManager`.
- **VIII–X (Frontend)** — N/A, this plan is backend-only.
- **XI. Application Layer Implementation** — PASS. New
  `Application/UseCases/DeleteRecurringExpense/` folder with
  `IDeleteRecurringExpenseUseCase`/`DeleteRecurringExpenseUseCase` and
  dedicated `DeleteRecurringExpenseUseCaseInput`/`Output`; the UseCase only
  orchestrates (validate scope shape → fetch aggregate → call the
  Aggregate's business method or the repository's `RemoveAsync` → persist);
  it implements no business rule itself.
- **XII. API Layer Implementation** — PASS. New action added to the
  existing versioned `RecurringExpensesController`; `scope` arrives via
  query string (no request body, matching the project's only other `DELETE`
  precedent, `OccurrencesController.UndoPayment`), so no new `DataRequest`
  is needed (same precedent as the bodyless `GetById` action); a new
  `DeleteRecurringExpenseDataResponse` + dedicated mapping file is added;
  all outcomes are declared via `ProducesResponseType` (`200`, `400`, `404`,
  `500`); the existing global exception middleware handles `400`/`404`/`500`
  with zero new middleware code.

No violations — Complexity Tracking table is not needed.

## Project Structure

### Documentation (this feature)

```text
specs/010-delete-recurring-expense/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
├── checklists/          # Already present (speckit-checklist output)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── Domain/
│   ├── Aggregates/
│   │   └── RecurringExpense.cs                 # + DeleteKeepingPaidHistory(), _deletedAt, IsDeleted()/GetDeletedAt()
│   └── Repositories/
│       └── IRecurringExpenseRepository.cs       # + RemoveAsync(RecurringExpense)
├── Application/
│   └── UseCases/
│       └── DeleteRecurringExpense/              # NEW
│           ├── IDeleteRecurringExpenseUseCase.cs
│           ├── DeleteRecurringExpenseUseCase.cs
│           ├── DeleteRecurringExpenseUseCaseInput.cs
│           └── DeleteRecurringExpenseUseCaseOutput.cs
├── Infrastructure/
│   ├── Repositories/
│   │   └── RecurringExpenseRepository.cs        # + RemoveAsync(); GetByIdAsync() filters _deletedAt
│   ├── Configs/
│   │   └── RecurringExpenseConfigurations.cs     # + _deletedAt column mapping
│   └── Migrations/
│       └── <timestamp>_AddRecurringExpenseDeletedAt.cs   # NEW, additive-only
├── Api/
│   ├── Controllers/
│   │   └── RecurringExpensesController.cs        # + Delete(Guid id, [FromQuery] string? scope)
│   ├── Responses/
│   │   └── DeleteRecurringExpenseDataResponse.cs # NEW
│   └── Mappings/
│       └── DeleteRecurringExpenseDataResponseMapping.cs  # NEW
├── Domain.Tests/
│   └── Aggregates/RecurringExpenseTests.cs       # + DeleteKeepingPaidHistory() cases
├── Application.Tests/
│   └── UseCases/DeleteRecurringExpense/          # NEW (Fake RepositoryManager + In-memory repo, per existing pattern)
├── Infrastructure.Tests/
│   └── Repositories/RecurringExpenseRepositoryTests.cs  # + RemoveAsync cascade-delete test, + GetByIdAsync filter test
└── Api.Tests/
    └── Controllers/RecurringExpensesControllerTests.cs  # + one test per 200/400/404/500 scenario
```

**Structure Decision**: Backend-only change inside the existing single .NET
solution under `backend/` (Domain, Application, Infrastructure, Api, and
their four matching `*.Tests` projects). No new project is created; every
new type lands in the folder its layer's constitution principle already
mandates (`/Aggregates`, `/UseCases/<Name>/`, `/Repositories`,
`/Migrations`, `/Configs`, `/Controllers`, `/Responses`, `/Mappings`). No
frontend directories are touched — the frontend entry point for this
deletion is explicitly a separate feature (spec.md Assumptions).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
