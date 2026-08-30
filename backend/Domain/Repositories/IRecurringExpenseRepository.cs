using ContasEmDia.Domain.Aggregates;

namespace ContasEmDia.Domain.Repositories;

public interface IRecurringExpenseRepository
{
    Task AddAsync(RecurringExpense recurringExpense);

    Task<RecurringExpense?> GetByIdAsync(Guid id);

    Task<IReadOnlyCollection<RecurringExpense>> GetActiveAsync();
}
