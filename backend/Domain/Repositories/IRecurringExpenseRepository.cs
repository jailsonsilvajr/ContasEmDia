using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Repositories;

public interface IRecurringExpenseRepository
{
    Task AddAsync(RecurringExpense recurringExpense);

    Task<RecurringExpense?> GetByIdAsync(Guid id);

    Task<IReadOnlyCollection<RecurringExpense>> GetActiveAsync();

    Task<IReadOnlyCollection<RecurringExpense>> GetByReferencePeriodAsync(ReferencePeriod referencePeriod);

    Task<RecurringExpense?> GetByOccurrenceIdAsync(Guid occurrenceId);

    Task UpdateAsync(RecurringExpense recurringExpense);
}
