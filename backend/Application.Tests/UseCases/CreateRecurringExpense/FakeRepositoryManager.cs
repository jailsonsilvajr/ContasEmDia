using ContasEmDia.Domain.Repositories;

namespace ContasEmDia.Application.Tests.UseCases.CreateRecurringExpense;

internal sealed class FakeRepositoryManager : IRepositoryManager
{
    public FakeRepositoryManager(IRecurringExpenseRepository recurringExpenseRepository)
    {
        RecurringExpenseRepository = recurringExpenseRepository;
    }

    public IRecurringExpenseRepository RecurringExpenseRepository { get; }
}
