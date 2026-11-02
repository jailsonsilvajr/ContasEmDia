# Phase 1 Data Model: Deletar Despesa Recorrente (Backend)

This feature extends two entities that already exist (`RecurringExpense`,
`Occurrence`) rather than introducing new ones. No new Aggregate, Entity, or
Value Object is created.

## RecurringExpense (Aggregate — extended)

| Field | Type | Notes |
|---|---|---|
| `_deletedAt` *(new)* | `CalendarDate?` | `null` while not deleted. Set exactly once, by `DeleteKeepingPaidHistory()`, and never cleared (no "undo" per Assumptions). Orthogonal to `_status` (`RecurringExpenseStatus`, Active/Paused) — both can independently hold any combination. |

### New business method

```
DeleteKeepingPaidHistory() : void
```
- Preconditions: none (FR-005 — works regardless of Active/Paused status;
  also works if the expense has zero occurrences, or only paid ones, per the
  spec's Edge Cases).
- Effect:
  1. Removes every occurrence whose `OccurrenceStatus` is not `Paid` from
     `_occurrences` (in-memory only; persisted by the caller's subsequent
     `UpdateAsync`).
  2. Leaves every `Paid` occurrence completely untouched.
  3. Sets `_deletedAt` to the current date — note this method does not
     itself take a date parameter from the caller; it reads the already-
     Domain-owned pattern of **not** requiring a `CurrentDateProvider` for a
     value that is only ever used as a boolean marker today (`IsDeleted()`).
     If a concrete calendar value for `_deletedAt` is desired (vs. a plain
     `bool`), the UseCase supplies the current date the same way
     `UpdateRecurringExpenseUseCase` already supplies `currentReferencePeriod`
     via `ICurrentDateProvider` — see Application layer below.
- Post-conditions / invariants enforced going forward by **other** existing
  methods, not by this one: once `_deletedAt` is set, no code path may ever
  generate a new occurrence for this expense again. Concretely:
  - `GenerateOccurrenceForPeriodIfMissing` (called by `ChangeEndDate` when
    extending vigência, and by `Reactivate`) must become a no-op once
    `_deletedAt` is set.
  - This is the "sem possibilidade de reativação" guarantee from FR-003 —
    it is not a new validation that throws (deletion is terminal and
    doesn't need to reject anything after the fact, because the expense is
    no longer reachable for editing at all per decision #4 in research.md);
    it is a defensive invariant inside the Aggregate itself, kept so that
    even a direct in-process call (e.g. from a future batch job that
    doesn't go through the now-filtered `GetByIdAsync`) cannot resurrect
    occurrence generation for a deleted expense.
- Never throws: unlike `ChangeEndDate`'s `hasPaidOccurrenceBeingRemoved`
  guard, this method never blocks, because it only ever removes already-
  non-paid occurrences.

### New read accessors

```
IsDeleted() : bool          // _deletedAt is not null
GetDeletedAt() : CalendarDate?
```

### No change to `RecurringExpenseStatusType`

`Active`/`Paused` remains a closed two-value enum; deletion state is fully
orthogonal and invisible to `UpdateRecurringExpenseUseCase`'s status
handling.

## Occurrence (Entity — unchanged)

No new field or method. `pending-only` deletion only ever removes
non-`Paid` occurrences via the Aggregate's existing collection manipulation;
`all` deletion removes the Aggregate itself and lets the EF Core cascade
(required 1:N relationship, already configured) remove every dependent
`Occurrence` row, paid or not. The `Occurrence` entity itself needs no
awareness of either scope.

## Deletion scope (Application-layer concept — not a Domain type)

Represented as a plain, validated `string` inside
`DeleteRecurringExpenseUseCase` — not a Domain Value Object (see
research.md #5 for why). The two accepted literal values, and their
query-string spelling, are:

| Query value | Meaning | Domain/Infra action taken |
|---|---|---|
| `pending-only` | FR-003: remove unpaid occurrences, preserve paid, block future generation | `recurringExpense.DeleteKeepingPaidHistory()` + `repository.UpdateAsync(recurringExpense)` |
| `all` | FR-004: remove everything, expense becomes entirely unfindable | `repository.RemoveAsync(recurringExpense)` |
| *(absent, or any other value)* | FR-002: reject entirely, no mutation | `FieldError("scope", "<PT-BR message>")`, no repository call |

## IRecurringExpenseRepository (interface — extended)

| Method | Change |
|---|---|
| `GetByIdAsync(Guid id)` | **Behavior change**: now excludes a `RecurringExpense` whose `_deletedAt` is not null (returns `null` for it, same as "doesn't exist"). Used by `GetRecurringExpenseByIdUseCase`, `UpdateRecurringExpenseUseCase`, and the new `DeleteRecurringExpenseUseCase` — all three automatically gain FR-007's "not found" behavior for a `pending-only`-deleted expense with zero changes in those UseCases. |
| `GetByOccurrenceIdAsync(Guid occurrenceId)` | **Implementation change only, same contract**: must stop delegating to `GetByIdAsync` internally (that would now incorrectly apply the `_deletedAt` filter) and instead run its own unfiltered, `Include`d lookup by the resolved parent id. Behavior from the caller's perspective (`MarkOccurrenceAsPaid`, `UndoOccurrencePayment`) is unchanged — it must keep finding a `pending-only`-deleted expense that still has the requested (paid) occurrence. |
| `GetByReferencePeriodAsync(ReferencePeriod referencePeriod)` | **No change.** Must continue to surface a `pending-only`-deleted expense's paid occurrences for the Monthly Panel (FR-008). |
| `RemoveAsync(RecurringExpense recurringExpense)` *(new)* | `_context.RecurringExpenses.Remove(recurringExpense); await _context.SaveChangesAsync();` — relies on the already-`IsRequired()` 1:N relationship to `Occurrence` to cascade-delete dependents (to be confirmed by an `Infrastructure.Tests` integration test, not assumed — see research.md #3). |
| `AddAsync`, `GetActiveAsync`, `UpdateAsync` | No change. |

## Persistence mapping (`RecurringExpenseConfigurations`)

New column, following the exact pattern already used for
`Occurrence._paymentDate`:

```
builder.Property<CalendarDate?>("_deletedAt")
    .HasColumnName("DeletedAt")
    .HasConversion(vo => vo!.GetValue(), value => new CalendarDate(value))
    .HasColumnType("date")
    .IsRequired(false);
```

Requires one new additive EF Core migration (working name
`AddRecurringExpenseDeletedAt`), following the same pattern as
`AddOccurrencePaymentTracking`/`AddRecurringExpenseEndDate`. No existing
migration is edited, per Principle VII.

## Application layer types (new)

```
DeleteRecurringExpenseUseCaseInput
  - Id: Guid            (required)
  - Scope: string?       (raw query value; required-ness validated by the UseCase, not by a data annotation, since there is no DataRequest)

DeleteRecurringExpenseUseCaseOutput
  - IsSuccess: bool
  - Data: DeleteRecurringExpenseData?   (null on failure)
  - Errors: IReadOnlyCollection<FieldError>   (empty on success)

DeleteRecurringExpenseData   (plain record, mirrors RecurringExpenseData's role)
  - RecurringExpenseId: Guid
  - Scope: string        (the scope actually applied, echoed back — "pending-only" or "all")
```

State transition performed by `DeleteRecurringExpenseUseCase.ExecuteAsync`:

1. Validate `Scope` is exactly `"pending-only"` or `"all"`; if not (including
   absent) → `Failure([FieldError("scope", "<PT-BR message>")])`, no
   repository access (FR-002).
2. `GetByIdAsync(Id)` → if `null` (never existed, or already deleted in
   either scope) → `throw new KeyNotFoundException("Despesa recorrente não
   encontrada.")`, caught by the existing global exception middleware → 404
   (FR-006/FR-007).
3. If `Scope == "all"` → `repository.RemoveAsync(recurringExpense)`.
4. If `Scope == "pending-only"` →
   `recurringExpense.DeleteKeepingPaidHistory()` then
   `repository.UpdateAsync(recurringExpense)`.
5. Return `Success(new DeleteRecurringExpenseData(recurringExpense.GetId(),
   Scope))` (FR-009).

## API layer types (new)

```
DeleteRecurringExpenseDataResponse (record, in /Responses)
  - RecurringExpenseId: Guid
  - Scope: string
```

Mapped from `DeleteRecurringExpenseData` via a dedicated
`DeleteRecurringExpenseDataResponseMapping.ToDataResponse()` extension in
`/Mappings`, following the project's one-mapping-file-per-type convention.

No `DeleteRecurringExpenseDataRequest` is introduced (no request body — see
research.md #5).
