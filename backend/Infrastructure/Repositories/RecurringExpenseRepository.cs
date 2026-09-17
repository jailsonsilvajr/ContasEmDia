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
        // A plain scalar projection (not `.Select(o => o)`) does not track any entities, so this
        // read-only lookup cannot interfere with the Occurrence entries GetByIdAsync already
        // attached to this context. Comparing against that separately-sourced id set — rather than
        // inspecting each occurrence's EntityEntry.State — avoids a known EF Core pitfall: a
        // never-before-seen entity reached via navigation from an already-tracked (Unchanged)
        // parent, with a client-generated (non-database-generated) key, gets auto-fixed-up as
        // Unchanged instead of Added, because EF cannot tell a "real" key value apart from a new
        // one — which then produces an UPDATE affecting 0 rows instead of an INSERT.
        var existingIds = await _context.Set<Occurrence>()
            .Where(o => EF.Property<Guid>(o, "RecurringExpenseId") == recurringExpense.GetId())
            .Select(o => EF.Property<Guid>(o, "_id"))
            .ToListAsync();
        var existingIdSet = existingIds.ToHashSet();

        foreach (var occurrence in recurringExpense.GetOccurrences())
        {
            if (!existingIdSet.Contains(occurrence.GetId()))
            {
                _context.Add(occurrence);
            }
        }

        await _context.SaveChangesAsync();
    }
}
