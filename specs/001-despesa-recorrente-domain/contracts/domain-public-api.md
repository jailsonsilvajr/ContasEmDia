# Contract: `ContasEmDia.Domain` Public API

**Feature**: `001-despesa-recorrente-domain` | **Date**: 2026-08-29

This project is a backend Domain class library, not an HTTP service — it has
no endpoints. Its "contract" (per the plan workflow's "public APIs for
libraries" case) is the public surface that a future Application layer and
this feature's own unit tests are allowed to depend on. Signatures are
normative for implementation; exact parameter order may be adjusted during
implementation as long as behavior and types below are preserved.

Namespace root: `ContasEmDia.Domain`.

## Aggregate: `ContasEmDia.Domain.Aggregates.RecurringExpense`

```csharp
public sealed class RecurringExpense
{
    public RecurringExpense(
        ExpenseName name,
        ExpenseCategory category,
        Money monthlyAmount,
        DueDay dueDay,
        CalendarDate startDate,
        Frequency frequency,
        RecurringExpenseStatus status,
        Note note,
        ReferencePeriod currentReferencePeriod);

    public Guid GetId();
    public ExpenseName GetName();
    public ExpenseCategory GetCategory();
    public Money GetMonthlyAmount();
    public DueDay GetDueDay();
    public CalendarDate GetStartDate();
    public Frequency GetFrequency();
    public RecurringExpenseStatus GetStatus();
    public Note GetNote();
    public IReadOnlyCollection<Occurrence> GetOccurrences();
}
```

**Pre/postconditions**:
- Throws `ArgumentException` (or a domain-specific exception derived from
  it) if any constructor argument value object was itself impossible to
  construct — value objects validate at their own construction point, so by
  the time this constructor runs, arguments are individually valid; this
  constructor does not re-validate their internals.
- After construction, `GetOccurrences()` contains exactly one `Occurrence`
  if `status.GetValue() == Active` and `currentReferencePeriod >=
  ReferencePeriod.FromDate(startDate.GetValue())`; otherwise it is empty.

## Entity: `ContasEmDia.Domain.Entities.Occurrence`

```csharp
public sealed class Occurrence
{
    // internal constructor: only RecurringExpense may create an Occurrence.
    internal Occurrence(
        ReferencePeriod referencePeriod,
        CalendarDate dueDate,
        ExpenseName name,
        ExpenseCategory category,
        Money expectedAmount);

    public Guid GetId();
    public ReferencePeriod GetReferencePeriod();
    public CalendarDate GetDueDate();
    public OccurrenceStatus GetStatus();
    public ExpenseName GetName();
    public ExpenseCategory GetCategory();
    public Money GetExpectedAmount();
}
```

`GetStatus()` always returns `OccurrenceStatus` with value `Pending` for
occurrences produced by this feature.

## Value Objects: `ContasEmDia.Domain.ValueObjects`

```csharp
public sealed class ExpenseName
{
    public ExpenseName(string value); // throws if null/empty/whitespace-only
    public string GetValue();
}

public enum ExpenseCategoryType { Housing, Services, Transportation, Subscriptions, Other }
public sealed class ExpenseCategory
{
    public ExpenseCategory(ExpenseCategoryType value);
    public ExpenseCategoryType GetValue();
}

public sealed class Money
{
    public Money(decimal value); // throws if <= 0 or > 2 decimal places
    public decimal GetValue();
}

public sealed class DueDay
{
    public DueDay(int value); // throws if outside 1..31
    public int GetValue();
}

public sealed class CalendarDate
{
    public CalendarDate(DateOnly value);
    public DateOnly GetValue();
}

public sealed class ReferencePeriod : IComparable<ReferencePeriod>
{
    public ReferencePeriod(int year, int month); // throws if month outside 1..12 or year <= 0
    public static ReferencePeriod FromDate(DateOnly date);
    public int Year { get; } // exposed as read-only component, not settable state
    public int Month { get; }
    public int CompareTo(ReferencePeriod? other);
    public static bool operator >=(ReferencePeriod left, ReferencePeriod right);
    public static bool operator <(ReferencePeriod left, ReferencePeriod right);
}

public enum FrequencyType { Monthly }
public sealed class Frequency
{
    public Frequency(FrequencyType value); // throws if value != Monthly
    public FrequencyType GetValue();
}

public enum RecurringExpenseStatusType { Active, Paused }
public sealed class RecurringExpenseStatus
{
    public RecurringExpenseStatus(RecurringExpenseStatusType value);
    public RecurringExpenseStatusType GetValue();
}

public enum OccurrenceStatusType { Pending, Paid }
public sealed class OccurrenceStatus
{
    public OccurrenceStatus(OccurrenceStatusType value);
    public OccurrenceStatusType GetValue();
}

public sealed class Note
{
    public Note(string? value); // no validation; null/empty both mean "no note"
    public string? GetValue();
}
```

Note on `ReferencePeriod`: it exposes `Year`/`Month` as read-only
auto-properties rather than only a `GetValue()` because it is a compound
value (two components); consumers needing the raw pair read them directly,
while ordering/equality is expressed through `IComparable<T>` and the
comparison operators rather than manual field access.

## Repository interface: `ContasEmDia.Domain.Repositories.IRecurringExpenseRepository`

```csharp
public interface IRecurringExpenseRepository
{
    Task AddAsync(RecurringExpense recurringExpense);
    Task<RecurringExpense?> GetByIdAsync(Guid id);
    Task<IReadOnlyCollection<RecurringExpense>> GetActiveAsync();
}
```

No implementation of this interface is part of this feature (spec
Assumptions) — it exists purely as the contract a future persistence layer
must fulfill and that this feature's own tests may fulfill with an in-memory
fake for User Story 3 style scenarios.
