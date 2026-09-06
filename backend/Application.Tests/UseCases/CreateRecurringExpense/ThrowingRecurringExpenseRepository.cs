using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.Repositories;

namespace ContasEmDia.Application.Tests.UseCases.CreateRecurringExpense;

internal sealed class ThrowingRecurringExpenseRepository : IRecurringExpenseRepository
{
    public Task AddAsync(RecurringExpense recurringExpense) =>
        throw new InvalidOperationException("Database unavailable.");

    public Task<RecurringExpense?> GetByIdAsync(Guid id) =>
        throw new InvalidOperationException("Database unavailable.");

    public Task<IReadOnlyCollection<RecurringExpense>> GetActiveAsync() =>
        throw new InvalidOperationException("Database unavailable.");
}
