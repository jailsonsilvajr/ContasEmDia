using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.Repositories;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Application.Tests.UseCases.CreateRecurringExpense;

internal sealed class ThrowingRecurringExpenseRepository : IRecurringExpenseRepository
{
    public Task AddAsync(RecurringExpense recurringExpense) =>
        throw new InvalidOperationException("Database unavailable.");

    public Task<RecurringExpense?> GetByIdAsync(Guid id) =>
        throw new InvalidOperationException("Database unavailable.");

    public Task<IReadOnlyCollection<RecurringExpense>> GetActiveAsync() =>
        throw new InvalidOperationException("Database unavailable.");

    public Task<IReadOnlyCollection<RecurringExpense>> GetByReferencePeriodAsync(ReferencePeriod referencePeriod) =>
        throw new InvalidOperationException("Database unavailable.");

    public Task<RecurringExpense?> GetByOccurrenceIdAsync(Guid occurrenceId) =>
        throw new InvalidOperationException("Database unavailable.");

    public Task UpdateAsync(RecurringExpense recurringExpense) =>
        throw new InvalidOperationException("Database unavailable.");
}
