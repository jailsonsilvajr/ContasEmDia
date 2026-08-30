# Quickstart: Validating the Despesa Recorrente Domain

**Feature**: `001-despesa-recorrente-domain` | **Date**: 2026-08-29

This guide validates the Domain project in isolation — there is no API or UI
in this feature (see spec Assumptions). Validation is done by building the
class library and running its unit test suite against the acceptance
scenarios in `spec.md`. See [data-model.md](data-model.md) for full
type/field details and [contracts/domain-public-api.md](contracts/domain-public-api.md)
for exact signatures.

## Prerequisites

- .NET 10 SDK installed (`dotnet --version` reports `10.x`).
- Repository cloned; working directory is the repo root.

## Setup

```bash
cd backend
dotnet new sln -n ContasEmDia
dotnet new classlib -n ContasEmDia.Domain -o Domain
dotnet new xunit -n ContasEmDia.Domain.Tests -o Domain.Tests
dotnet sln ContasEmDia.sln add Domain/ContasEmDia.Domain.csproj Domain.Tests/ContasEmDia.Domain.Tests.csproj
dotnet add Domain.Tests/ContasEmDia.Domain.Tests.csproj reference Domain/ContasEmDia.Domain.csproj
```

(Exact commands are illustrative; the implementation phase produces the
final `.csproj`/`.sln` files under source control. This is the expected
one-time scaffolding shape per the plan's Structure Decision.)

## Build

```bash
dotnet build backend/ContasEmDia.sln
```

Expected: builds with zero warnings (nullable reference types + warnings-as-
errors per Constitution Principle III).

## Run tests

```bash
dotnet test backend/ContasEmDia.sln
```

Expected: all unit tests pass. At minimum, the suite MUST cover — see
`spec.md` for full scenario text:

| Scenario | Spec reference | Expected result |
|---|---|---|
| Active despesa, start date ≤ current competência | User Story 1, Scenario 1 | Despesa created; exactly 1 `Pending` occurrence for the current competência, `ExpectedAmount` equal to `MonthlyAmount`. |
| Due day 31, competência is February | User Story 1, Scenario 2 | Occurrence's due date falls on the last day of February. |
| Occurrence display data | User Story 1, Scenario 3 | Occurrence's name/category/amount match the despesa's values at generation time. |
| Paused despesa | User Story 2, Scenario 1 | Despesa created with status Paused; `GetOccurrences()` is empty. |
| Active despesa, start date in a future competência | User Story 2, Scenario 2 | Despesa created; `GetOccurrences()` is empty. |
| Retrieve by id | User Story 3, Scenario 1 | Repository fake returns a despesa with all originally supplied data intact. |
| Retrieve active only | User Story 3, Scenario 2 | Repository fake's `GetActiveAsync()` returns only `Active`-status despesas from a mixed-status set. |
| Empty/whitespace name | Edge Cases | `ExpenseName` constructor throws. |
| Invalid category | Edge Cases | `ExpenseCategory` constructor throws. |
| Amount ≤ 0 or > 2 decimals | Edge Cases | `Money` constructor throws. |
| Due day outside 1-31 | Edge Cases | `DueDay` constructor throws. |
| Frequency other than Monthly | Edge Cases | `Frequency` constructor throws. |

For User Story 3 scenarios, tests implement `IRecurringExpenseRepository`
with a simple in-memory `Dictionary<Guid, RecurringExpense>`-backed fake
local to the test project — this is test infrastructure, not a production
implementation, and does not violate the "interfaces only" scope of this
feature.

## Manual smoke check (optional)

A short `Program.cs`-free way to sanity check interactively is not
applicable here since this is a class library with no entry point; the
`dotnet test` run above is the complete validation path for this feature.
