using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.Repositories;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Application.Tests.UseCases.CreateRecurringExpense;

internal sealed class InMemoryRecurringExpenseRepository : IRecurringExpenseRepository
{
    private readonly Dictionary<Guid, RecurringExpense> _store = [];

    public IReadOnlyCollection<RecurringExpense> StoredExpenses => _store.Values.ToList();

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
}
