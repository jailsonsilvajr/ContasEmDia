# Implementation Plan: Domínio de Despesa Recorrente

**Branch**: `001-despesa-recorrente-domain` | **Date**: 2026-08-29 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-despesa-recorrente-domain/spec.md`

## Summary

Build the backend Domain model needed for the "Nova despesa recorrente"
screen: a `RecurringExpense` aggregate that can be created with validated
name/category/amount/due-day/start-date/frequency/status/note, and that
automatically generates a single `Pending` `Occurrence` entity for the
current competência at creation time when the despesa is Active and its
start date is not in the future — with no occurrence generated when the
despesa is Paused or its start date is in a future competência. The
technical approach is a pure, dependency-free `ContasEmDia.Domain` C# class
library (Aggregates / Entities / Value Objects / Repository interfaces
only, per Constitution Principle VI), with no persistence, API, or UI
layer in scope. Backend projects live under `/backend`, one folder per
project, per explicit user instruction.

## Technical Context

**Language/Version**: C# on .NET 10 (Constitution: Technology Stack Requirements)

**Primary Dependencies**: None in the Domain project itself (pure domain
library, no external packages); xUnit in the accompanying test project only

**Storage**: N/A — this feature defines only a repository interface
(`IRecurringExpenseRepository`); no persistence implementation is in scope

**Testing**: xUnit unit tests (`backend/Domain.Tests`), test-first per
Constitution Principle II

**Target Platform**: Cross-platform .NET 10 class library (no host process,
no OS-specific dependency)

**Project Type**: Backend-only domain library — no frontend or API work in
this feature

**Performance Goals**: N/A — in-memory object construction and validation
only; no throughput/latency targets apply to this feature

**Constraints**: Domain layer MUST NOT read system time internally (FR-021
— "competência do mês corrente" is an explicit constructor parameter); no
Domain Events (Constitution VI); no primitive-typed public properties on
Aggregates/Entities/Value Objects (Constitution VI); no status-transition
methods in this feature (spec Clarification #1)

**Scale/Scope**: 1 aggregate (`RecurringExpense`), 1 internal entity
(`Occurrence`), 9 Value Objects, 1 repository interface — see
[data-model.md](data-model.md)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applicability | Assessment |
|---|---|---|
| I. API-First Backend/Frontend Separation | N/A this feature | No HTTP API or frontend consumer is introduced; this feature is scoped to the Domain layer only, which a future Application/API feature will sit on top of. No boundary is bypassed because no boundary exists yet. |
| II. Test-First Development | Applies | Unit tests in `backend/Domain.Tests` (xUnit) MUST be written before/alongside each piece of domain logic and MUST fail before the implementation exists (Red-Green-Refactor), enforced during `/speckit-tasks` + `/speckit-implement`. |
| III. Type Safety & Static Analysis | Applies | `ContasEmDia.Domain.csproj` MUST enable `<Nullable>enable</Nullable>` and treat warnings as errors, per Technology Stack Requirements. |
| IV. Secure Handling of Financial Data | Mostly N/A | No secrets, connection strings, authentication, or data transmission exist at this layer. The relevant carry-over is input validation: Value Object constructors perform all validation (FR-002–FR-008) before any data is considered "valid domain state," which upholds the spirit of "validate before reaching business logic/persistence" even though no API boundary exists yet. |
| V. Simplicity & Incremental Delivery | Applies | Single class library + single test project; no repository implementation, no Application layer, no status-transition methods, no non-monthly frequency support — all deferred because nothing in this feature's scope needs them (see spec Assumptions). |
| VI. Domain-Driven Design in the Domain Layer | Applies — core gate | Design in [data-model.md](data-model.md) and [contracts/domain-public-api.md](contracts/domain-public-api.md) follows every VI rule: dedicated `/Aggregates`, `/Entities`, `/ValueObjects`, `/Repositories` folders; repository interface only for the Aggregate; no primitive-typed public properties (every field is a Value Object); constructor-only creation (no parameterless ctor, no object initializers); GUID ids generated internally; all reads via `GetX()` business-intent methods, no public gettable/settable properties (except `ReferencePeriod.Year`/`Month`, justified in contracts.md as read-only components of a compound value, not mutable state); immutable Value Objects with `GetValue()`; hand-rolled constructor validation, no validation libraries; no Domain Events. |

**Result**: PASS. No violations requiring justification — Complexity
Tracking table is not needed.

## Project Structure

### Documentation (this feature)

```text
specs/001-despesa-recorrente-domain/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md         # Phase 1 output (/speckit-plan command)
├── contracts/
│   └── domain-public-api.md  # Phase 1 output (/speckit-plan command)
└── tasks.md              # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── ContasEmDia.sln
├── Domain/
│   ├── ContasEmDia.Domain.csproj
│   ├── Aggregates/
│   │   └── RecurringExpense.cs
│   ├── Entities/
│   │   └── Occurrence.cs
│   ├── ValueObjects/
│   │   ├── ExpenseName.cs
│   │   ├── ExpenseCategory.cs
│   │   ├── Money.cs
│   │   ├── DueDay.cs
│   │   ├── CalendarDate.cs
│   │   ├── ReferencePeriod.cs
│   │   ├── Frequency.cs
│   │   ├── RecurringExpenseStatus.cs
│   │   ├── OccurrenceStatus.cs
│   │   └── Note.cs
│   └── Repositories/
│       └── IRecurringExpenseRepository.cs
└── Domain.Tests/
    ├── ContasEmDia.Domain.Tests.csproj
    ├── Aggregates/
    │   └── RecurringExpenseTests.cs
    └── ValueObjects/
        └── (one test file per Value Object with validation rules)
```

**Structure Decision**: Web/service-shaped Option 2 from the template does
not apply as-is (no frontend project touched by this feature). Instead,
each backend project gets its own top-level folder under `backend/`, per
explicit user instruction: the Domain project lives at `backend/Domain/`
with project name `ContasEmDia.Domain`, and its test project lives at
`backend/Domain.Tests/` with project name `ContasEmDia.Domain.Tests`. Both
are referenced from a root `backend/ContasEmDia.sln`. Inside
`backend/Domain/`, the four folders `/Aggregates`, `/Entities`,
`/ValueObjects`, `/Repositories` are mandated by Constitution Principle VI.
No frontend or other backend project (e.g., API, Application,
Infrastructure) is created by this feature — those are out of scope per
the spec's Assumptions and will be introduced by future features that
build on this Domain project.

## Complexity Tracking

*No entries — Constitution Check reported no violations.*
