# Phase 0 Research: Deletar Despesa Recorrente (Backend)

All unknowns below were already resolved by the pre-existing technical
refinement `refinements/backend/deletar-despesa-recorrente.md`, cross-checked
against the current implementation of `RecurringExpense`,
`RecurringExpenseRepository`, `UpdateRecurringExpenseUseCase`, and
`RecurringExpensesController`. No NEEDS CLARIFICATION remained in spec.md, so
this phase focuses on confirming each decision is still consistent with the
code as it exists today, not on open-ended exploration.

## 1. How is the "definitive, no-reactivation" exclusion state represented?

- **Decision**: Add a new, orthogonal field to the `RecurringExpense`
  Aggregate — working name `_deletedAt` (`CalendarDate?`, null while not
  deleted) — rather than a third value on the existing
  `RecurringExpenseStatusType` enum (`Active`/`Paused`).
- **Rationale**: `RecurringExpenseStatusType` is a closed set of exactly the
  two values the edit screen/`UpdateRecurringExpenseUseCase` already accept
  as valid input. Adding a third ("Deleted") value would force that
  already-shipped edit path to explicitly reject a status value it should
  never have to know about. A separate field keeps the edit path completely
  unaware of deletion, matching FR-005 (deletion must work regardless of
  Active/Paused) and Principle V (simplest design for the current need).
  `CalendarDate?` needs no new Value Object — the type is already used
  nullable elsewhere (`Occurrence._paymentDate`).
- **Alternatives considered**: A third enum value — rejected for the reason
  above. A plain `bool _isDeleted` — rejected only because `_deletedAt`
  carries equivalent information (null = not deleted) while leaving room for
  a future "deleted when" read without a second field; this is not a new
  capability being built now, just a free byproduct of using `CalendarDate?`
  instead of `bool`.

## 2. How does "apenas pendentes/vencidas" remove occurrences without a new persistence mechanism?

- **Decision**: `DeleteKeepingPaidHistory()` removes every non-paid
  occurrence from the in-memory `_occurrences` collection
  (`_occurrences.RemoveAll(o => o.GetStatus().GetValue() !=
  OccurrenceStatusType.Paid)`) and sets `_deletedAt`; the existing
  `RecurringExpenseRepository.UpdateAsync()` — already relied on by
  `ChangeEndDate`'s vigência-reduction path — detects the occurrences that
  fell out of the collection and lets `SaveChangesAsync()` delete the now-
  orphaned rows, with zero new code in `UpdateAsync`.
- **Rationale**: This exact mechanism is already proven in production code
  (`ChangeEndDate`, `RecurringExpense.cs:132-168`) for the same kind of
  "collection shrinks → repository deletes the difference" operation. Reusing
  it avoids a second, parallel deletion mechanism (Principle V). Unlike
  `ChangeEndDate`, this method never needs to block the operation to protect
  a paid occurrence, because it only ever removes non-paid ones in the first
  place — there is nothing to validate.
- **Alternatives considered**: A new repository method that issues a
  targeted `DELETE ... WHERE Status != Paid` — rejected: it would duplicate
  `UpdateAsync`'s existing orphan-detection logic instead of reusing it, and
  would require the Domain method to not touch `_occurrences` at all, which
  contradicts Principle VI (the Aggregate must own this mutation, not SQL).

## 3. How does "todo o histórico" remove the expense and every occurrence?

- **Decision**: A new repository method, `RemoveAsync(RecurringExpense
  recurringExpense)`, calling `_context.RecurringExpenses.Remove(...)` then
  `SaveChangesAsync()`. No new Domain method is added for this scope.
- **Rationale**: `RecurringExpenseConfigurations` already declares
  `_occurrences` as a required (`IsRequired()`) 1:N relationship. EF Core's
  default behavior for removing the principal of an already-loaded
  (`Include`d) required relationship is to cascade-delete the dependents in
  the same `SaveChangesAsync()` call — no `OnDelete(DeleteBehavior.Cascade)`
  configuration or migration is needed purely for this. There is no business
  rule to validate for this scope (unlike `pending-only`), so the Domain
  Aggregate itself needs no new method — it only needs to still be loaded
  (via the existing `GetByIdAsync` + `Include`) so EF Core can see the
  dependents to cascade.
- **Verification required, not assumed**: The refinement explicitly flags
  this cascade behavior as something to prove with a real
  `Infrastructure.Tests` integration test against the project's
  `PostgreSqlContainerFixture` (Testcontainers), not something to trust from
  reading the EF Core docs alone — provider-specific quirks are possible.
  This becomes a mandatory task, not an assumption, per Principle II.
- **Alternatives considered**: Explicitly deleting each occurrence first,
  then the parent — rejected as redundant ceremony if the default cascade is
  confirmed to work; kept as a fallback only if the integration test above
  fails.

## 4. How does a `pending-only`-deleted expense become "not found" for direct queries/edits, without breaking paid-history visibility?

- **Decision**: Apply the `_deletedAt IS NULL` filter inside
  `RecurringExpenseRepository.GetByIdAsync()` itself — the single read method
  shared by `GetRecurringExpenseByIdUseCase`, `UpdateRecurringExpenseUseCase`,
  and the new `DeleteRecurringExpenseUseCase`. Do **not** apply this filter
  inside `GetByReferencePeriodAsync` (feeds the Monthly Panel) or
  `GetByOccurrenceIdAsync` (feeds `MarkOccurrenceAsPaid`/
  `UndoOccurrencePayment`).
- **Rationale**: `GetByIdAsync` already is the exact method all three
  "direct by id" use cases call and already `?? throw new
  KeyNotFoundException(...)` on a null result — filtering inside it makes
  a `pending-only`-deleted expense produce the standard 404 in all three
  places with **zero new code in any UseCase**, and automatically makes a
  second deletion attempt on the same id behave as "not found" too (FR-007),
  since the first deletion already set `_deletedAt`. The Monthly Panel and
  payment flows must keep finding a `pending-only`-deleted expense's paid
  occurrences (FR-008) — those two reads do not filter by id-equality at all
  today (they scan by period or by occurrence id), so leaving them
  unfiltered is not a special case to build, it is simply not touching code
  that was never filtering by `_deletedAt` to begin with.
- **Implementation note — one coupling to break**: today
  `GetByOccurrenceIdAsync` resolves the parent id and then **calls
  `GetByIdAsync(recurringExpenseId.Value)`** internally. Once `GetByIdAsync`
  gains the `_deletedAt` filter, that internal call would incorrectly hide a
  `pending-only`-deleted expense's paid occurrences from the payment flow.
  `GetByOccurrenceIdAsync` must therefore be changed to run its own
  unfiltered `Include`d lookup by id instead of delegating to the now-
  filtered `GetByIdAsync`. This is a necessary side-effect of decision #4,
  not a new feature.
- **Alternatives considered**: Filtering in each UseCase instead of in
  `GetByIdAsync` — rejected: three UseCases would each need the same
  `recurringExpense is null || recurringExpense.IsDeleted()` check,
  duplicating logic the refinement explicitly avoids; also would not, by
  itself, give FR-007's "second deletion is not-found" for free the way the
  repository-level filter does.

## 5. Where does the deletion-scope value get validated, and in what shape?

- **Decision**: `scope` arrives as a raw, optional query-string value
  (`?scope=all|pending-only`) with no request body and no dedicated
  `DataRequest` (same precedent as the bodyless `GET
  /recurring-expenses/{id}`). The API controller passes the raw string
  straight through to `DeleteRecurringExpenseUseCaseInput.Scope` (a plain
  `string?`), and `DeleteRecurringExpenseUseCase` validates it is exactly one
  of the two accepted literal values **before touching the repository**,
  returning a `FieldError("scope", ...)` failure otherwise — the same
  "build/validate everything first, only mutate state once everything is
  valid" structure `UpdateRecurringExpenseUseCase` already uses for its
  `category`/`status` enum-like string inputs.
- **Rationale**: Every other `DELETE` in this project
  (`OccurrencesController.UndoPayment`) takes its parameters from the
  route/query, never a body; mirroring that keeps the convention singular.
  Validating in the Application layer (not a Domain Value Object, not a new
  API-layer construct) matches the exact precedent already set for
  `status`/`category` in `UpdateRecurringExpenseUseCase`, and keeps the
  Domain layer unaware of "scope" entirely (the Domain never needs to choose
  between the two branches — the UseCase does).
- **Alternatives considered**: A Domain `RecurringExpenseDeletionScope`
  Value Object — rejected by the refinement itself: there is no business
  invariant for the Domain to protect here (the Domain doesn't even receive
  the scope), so a VO would be ceremony with no behavior, violating
  Principle V. A dedicated `DeleteRecurringExpenseDataRequest` — rejected:
  there is no request body to validate shape/presence of; the existing
  bodyless-`GET` precedent already establishes that a `DataRequest` is only
  introduced when there is a body.

## 6. What does a successful deletion return?

- **Decision**: A minimal payload — the recurring expense id and the scope
  that was actually applied — not the expense's editable fields, and not the
  remaining occurrence list.
- **Rationale**: In the `all` scope the expense no longer exists, so
  returning its former editable fields would be returning data about
  something that is now gone; in the `pending-only` scope, the screen that
  triggers deletion resolves its next state by reloading the Monthly Panel
  (a separate, already-existing read), not by reconstructing UI state from
  this endpoint's response — matching FR-009's minimum bar ("identify at
  least the affected expense and the scope actually applied") without
  inventing a response shape that would only be half-populated in one of the
  two branches.
- **Alternatives considered**: Returning the full
  `RecurringExpenseDataResponse` shape (null/former values for the `all`
  branch) — rejected as a response shape with fields that are meaningless in
  one of its two possible outcomes.
