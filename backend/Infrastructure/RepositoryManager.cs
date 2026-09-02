using ContasEmDia.Domain.Repositories;
using ContasEmDia.Infrastructure.Contexts;
using ContasEmDia.Infrastructure.Repositories;

namespace ContasEmDia.Infrastructure;

public sealed class RepositoryManager
{
    private readonly Lazy<IRecurringExpenseRepository> _recurringExpenseRepository;

    public RepositoryManager(ContasEmDiaDbContext context)
    {
        _recurringExpenseRepository = new Lazy<IRecurringExpenseRepository>(
            () => new RecurringExpenseRepository(context));
    }

    public IRecurringExpenseRepository RecurringExpenseRepository => _recurringExpenseRepository.Value;
}
