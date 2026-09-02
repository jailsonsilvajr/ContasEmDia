using ContasEmDia.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace ContasEmDia.Infrastructure.Contexts;

public sealed class ContasEmDiaDbContext : DbContext
{
    public ContasEmDiaDbContext(DbContextOptions<ContasEmDiaDbContext> options)
        : base(options)
    {
    }

    public DbSet<RecurringExpense> RecurringExpenses => Set<RecurringExpense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContasEmDiaDbContext).Assembly);
    }
}
