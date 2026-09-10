using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.Entities;
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

    public async Task<IReadOnlyCollection<RecurringExpense>> GetByReferencePeriodAsync(ReferencePeriod referencePeriod)
    {
        var allExpenses = await _context.RecurringExpenses
            .Include("_occurrences")
            .ToListAsync();

        return allExpenses
            .Where(e => e.GetOccurrencesForPeriod(referencePeriod).Count > 0)
            .ToList();
    }

    public async Task<RecurringExpense?> GetByOccurrenceIdAsync(Guid occurrenceId)
    {
        var recurringExpenseId = await _context.Set<Occurrence>()
            .Where(o => EF.Property<Guid>(o, "_id") == occurrenceId)
            .Select(o => EF.Property<Guid>(o, "RecurringExpenseId"))
            .Cast<Guid?>()
            .FirstOrDefaultAsync();

        if (recurringExpenseId is null)
        {
            return null;
        }

        return await GetByIdAsync(recurringExpenseId.Value);
    }

    public async Task UpdateAsync(RecurringExpense recurringExpense)
    {
        foreach (var occurrence in recurringExpense.GetOccurrences())
        {
            var entry = _context.Entry(occurrence);

            if (entry.State == EntityState.Detached)
            {
                entry.State = EntityState.Added;
            }
        }

        await _context.SaveChangesAsync();
    }
}
