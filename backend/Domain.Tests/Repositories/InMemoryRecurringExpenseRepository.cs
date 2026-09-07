using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.Repositories;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Tests.Repositories;

internal sealed class InMemoryRecurringExpenseRepository : IRecurringExpenseRepository
{
    private readonly Dictionary<Guid, RecurringExpense> _store = [];

    public Task AddAsync(RecurringExpense recurringExpense)
    {
        _store[recurringExpense.GetId()] = recurringExpense;
        return Task.CompletedTask;
    }

    public Task<RecurringExpense?> GetByIdAsync(Guid id)
    {
        _store.TryGetValue(id, out var recurringExpense);
        return Task.FromResult(recurringExpense);
    }

    public Task<IReadOnlyCollection<RecurringExpense>> GetActiveAsync()
    {
        IReadOnlyCollection<RecurringExpense> active = _store.Values
            .Where(expense => expense.GetStatus().GetValue() == RecurringExpenseStatusType.Active)
            .ToList();

        return Task.FromResult(active);
    }

    public Task<IReadOnlyCollection<RecurringExpense>> GetByReferencePeriodAsync(ReferencePeriod referencePeriod)
    {
        IReadOnlyCollection<RecurringExpense> matching = _store.Values
            .Where(expense => expense.GetOccurrencesForPeriod(referencePeriod).Count > 0)
            .ToList();

        return Task.FromResult(matching);
    }

    public Task<RecurringExpense?> GetByOccurrenceIdAsync(Guid occurrenceId)
    {
        var recurringExpense = _store.Values.FirstOrDefault(expense => expense.FindOccurrence(occurrenceId) is not null);
        return Task.FromResult(recurringExpense);
    }

    public Task UpdateAsync(RecurringExpense recurringExpense) => Task.CompletedTask;
}
