using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.Repositories;
using ContasEmDia.Domain.ValueObjects;
using ContasEmDia.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ContasEmDia.Infrastructure.Repositories;

public sealed class RecurringExpenseRepository : IRecurringExpenseRepository
{
    private readonly ContasEmDiaDbContext _context;

    public RecurringExpenseRepository(ContasEmDiaDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RecurringExpense recurringExpense)
    {
        _context.RecurringExpenses.Add(recurringExpense);
        await _context.SaveChangesAsync();
    }

    public async Task<RecurringExpense?> GetByIdAsync(Guid id)
    {
        return await _context.RecurringExpenses
            .Include("_occurrences")
            .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "_id") == id);
    }

    public async Task<IReadOnlyCollection<RecurringExpense>> GetActiveAsync()
    {
        var activeStatus = new RecurringExpenseStatus(RecurringExpenseStatusType.Active);

        return await _context.RecurringExpenses
            .Include("_occurrences")
            .Where(e => EF.Property<RecurringExpenseStatus>(e, "_status") == activeStatus)
            .ToListAsync();
    }
}
