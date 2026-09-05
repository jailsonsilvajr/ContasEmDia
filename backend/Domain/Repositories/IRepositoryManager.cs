namespace ContasEmDia.Domain.Repositories;

public interface IRepositoryManager
{
    IRecurringExpenseRepository RecurringExpenseRepository { get; }
}
