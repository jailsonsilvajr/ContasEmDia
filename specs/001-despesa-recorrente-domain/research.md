# Research: Domínio de Despesa Recorrente

**Feature**: `001-despesa-recorrente-domain` | **Date**: 2026-08-29

All items below were resolvable from the feature spec (already clarified),
the project constitution (`.specify/memory/constitution.md`, principles I-VI
and Technology Stack Requirements), and the explicit user instruction on
project layout. No `NEEDS CLARIFICATION` markers remain.

## 1. Target framework and project layout

- **Decision**: A new `/backend` folder at the repository root hosts one
  folder per backend project. This feature creates two projects:
  `backend/Domain/ContasEmDia.Domain.csproj` (the Domain class library, C#,
  targeting `net10.0`) and `backend/Domain.Tests/ContasEmDia.Domain.Tests.csproj`
  (its unit test project). A `backend/ContasEmDia.sln` solution file
  references both.
- **Rationale**: The user explicitly requested `/backend/<ProjectName>` as
  the layout, with the Domain project named `ContasEmDia.Domain` and living
  in `/backend/Domain`. The constitution's Technology Stack Requirements
  mandate .NET 10 for all backend projects. The test project mirrors the
  same one-folder-per-project convention for consistency.
- **Alternatives considered**: A single flat `backend/` project with no
  per-project subfolder (rejected — contradicts the explicit user
  instruction); a `src/`/`tests/` split inside one project folder (rejected
  — user asked for one folder per project, not per layer).

## 2. Test framework

- **Decision**: xUnit for the Domain unit tests.
- **Rationale**: xUnit is the de facto standard test framework for modern
  .NET class libraries, has first-class .NET 10 SDK support, and there is
  no existing backend code in the repository that would dictate a different
  convention. Constitution Principle II requires tests to be written before
  implementation for non-trivial domain logic (occurrence generation, due
  date adjustment, status-based branching).
- **Alternatives considered**: NUnit (comparable feature set, no advantage
  here); MSTest (weaker parameterized-test ergonomics for the many
  boundary cases this domain needs — e.g. day-31-in-February, status ×
  competência matrix).

## 3. DDD building-block structure

- **Decision**: Inside `backend/Domain/`, use four top-level folders exactly
  as mandated by Constitution Principle VI: `/Aggregates`, `/Entities`,
  `/ValueObjects`, `/Repositories`.
- **Rationale**: Non-negotiable constitution rule; keeps the structure
  predictable as more aggregates are added in future features.
- **Alternatives considered**: None — this is a fixed constitutional
  constraint, not a design choice.

## 4. Representing "competência" (reference month/year)

- **Decision**: Model a `ReferencePeriod` Value Object holding a year and a
  month (no day component), comparable by chronological order
  (`year * 12 + month`). It is derived from a calendar date via a factory
  (`ReferencePeriod.FromDate(...)`) and is the type used both for (a) the
  `currentReferencePeriod` explicit input to the creation operation
  (FR-021) and (b) the competência each `Occurrence` belongs to.
- **Rationale**: FR-011/FR-013 compare "competência do mês corrente" against
  the despesa's start-date competência; both sides need the same
  month/year-only granularity and a well-defined ordering. Deriving it from
  a full date via a factory avoids duplicating year/month validation logic.
- **Alternatives considered**: Using two raw `int` fields (year, month)
  directly on the aggregate/entity (rejected — violates Constitution VI's
  ban on primitive-typed domain properties); reusing `DateOnly` truncated to
  the first day of the month (rejected — leaks a meaningless "day" value
  into a concept that has none, inviting incorrect day-based comparisons).

## 5. Due-date adjustment for short months (FR-006)

- **Decision**: The `DueDay` Value Object stores a validated day number
  (1-31). When the aggregate derives an `Occurrence`'s due date for a given
  `ReferencePeriod`, it clamps the day to `DateTime.DaysInMonth(year, month)`
  when the configured day exceeds the number of days in that month.
- **Rationale**: Directly implements FR-006 / SC-005 (day 31 in February →
  last day of February) using the .NET BCL's calendar-aware
  `DateTime.DaysInMonth`, avoiding a hand-rolled leap-year table.
- **Alternatives considered**: Rejecting despesa creation when due day > 28
  (rejected — spec explicitly requires automatic adjustment, not rejection).

## 6. Identifier generation

- **Decision**: `RecurringExpense` and `Occurrence` each generate their own
  `Guid` internally (`Guid.NewGuid()`) inside their constructors; callers
  never supply an id.
- **Rationale**: Constitution VI mandates GUID identifiers for every
  Aggregate and Entity but does not require external id assignment; nothing
  in the spec calls for caller-supplied ids, so generating internally is the
  simplest option consistent with Principle V (Simplicity & Incremental
  Delivery).
- **Alternatives considered**: Caller-supplied `Guid` parameter (rejected —
  no requirement drives it; adds an unused parameter to every constructor).

## 7. Closed-set fields as enum-backed Value Objects

- **Decision**: `ExpenseCategory`, `Frequency`, `RecurringExpenseStatus`, and
  `OccurrenceStatus` are each a Value Object wrapping a private C# `enum`,
  validated in the constructor, with `GetValue()` returning the enum value.
- **Rationale**: FR-003 (closed category set), FR-008 (Ativa/Pausada only),
  FR-008/FR-019 (Pendente/Paga only), and FR-008's "apenas mensal aceito
  nesta etapa" for `Frequency` are all closed-set validations best expressed
  as compiler-checked enums wrapped in a constructor-validating Value
  Object, satisfying Constitution VI's "no primitive properties" and
  "hand-rolled validation" rules simultaneously.
- **Alternatives considered**: Raw `string` categories validated against a
  set at each use site (rejected — violates the no-primitive-property rule
  and duplicates validation across call sites).

## 8. Optional free-text observação

- **Decision**: `Note` Value Object wraps a nullable `string?` internally;
  `GetValue()` returns `string?`. An empty/no-note case is represented as a
  `Note` whose `GetValue()` is `null`, not as a `null` `Note` reference.
- **Rationale**: FR-010 makes the observação fully optional with no format
  rule. Keeping `Note` always non-null as a VO (never a null property)
  keeps the "no primitive properties" rule uniform, while its internal
  value can still be absent.
- **Alternatives considered**: `Note?` nullable reference property on the
  aggregate (rejected — inconsistent with treating every domain-meaningful
  property as a non-null Value Object; would also need null-checks at every
  read site instead of one internal null-check inside `Note`).

## 9. Scope boundary confirmed

- No persistence technology, API, or frontend concerns are part of this
  feature (Assumptions section of the spec). `Storage: N/A` and
  `Project Type: backend domain library` in the Technical Context reflect
  that boundary; the repository is interface-only.
- No Domain Events, no status-transition methods (`Pausar`/`Reativar`), and
  no support for non-monthly frequencies are implemented, per the spec's
  Clarifications and Constitution VI's Domain Events prohibition.
