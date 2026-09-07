using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.Repositories;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Application.Tests.UseCases.GetMonthlyPanel;

internal sealed class InMemoryRecurringExpenseRepository : IRecurringExpenseRepository
{
    private readonly List<RecurringExpense> _store = [];

    public void Seed(params RecurringExpense[] expenses) => _store.AddRange(expenses);

    public Task AddAsync(RecurringExpense recurringExpense)
    {
        _store.Add(recurringExpense);
        return Task.CompletedTask;
    }

    public Task<RecurringExpense?> GetByIdAsync(Guid id) =>
        Task.FromResult(_store.FirstOrDefault(expense => expense.GetId() == id));

    public Task<IReadOnlyCollection<RecurringExpense>> GetActiveAsync()
    {
        IReadOnlyCollection<RecurringExpense> active = _store
            .Where(expense => expense.GetStatus().GetValue() == RecurringExpenseStatusType.Active)
            .ToList();

        return Task.FromResult(active);
    }

    public Task<IReadOnlyCollection<RecurringExpense>> GetByReferencePeriodAsync(ReferencePeriod referencePeriod)
    {
        IReadOnlyCollection<RecurringExpense> matching = _store
            .Where(expense => expense.GetOccurrencesForPeriod(referencePeriod).Count > 0)
            .ToList();

        return Task.FromResult(matching);
    }

    public Task<RecurringExpense?> GetByOccurrenceIdAsync(Guid occurrenceId)
    {
        var recurringExpense = _store.FirstOrDefault(expense => expense.FindOccurrence(occurrenceId) is not null);
        return Task.FromResult(recurringExpense);
    }

    public Task UpdateAsync(RecurringExpense recurringExpense) => Task.CompletedTask;
}
