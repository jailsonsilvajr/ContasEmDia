using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Tests.Repositories;

public class InMemoryRecurringExpenseRepositoryTests
{
    private static RecurringExpense CreateExpense(
        string name,
        RecurringExpenseStatusType status,
        string? note = "Pagar até o dia 10")
    {
        return new RecurringExpense(
            new ExpenseName(name),
            new ExpenseCategory(ExpenseCategoryType.Housing),
            new Money(1500m),
            new DueDay(10),
            new CalendarDate(new DateOnly(2026, 8, 1)),
            new Frequency(FrequencyType.Monthly),
            new RecurringExpenseStatus(status),
            new Note(note),
            new ReferencePeriod(2026, 8));
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsExpenseWithAllOriginallySuppliedDataIntact()
    {
        var repository = new InMemoryRecurringExpenseRepository();
        var expense = CreateExpense("Aluguel", RecurringExpenseStatusType.Active);

        await repository.AddAsync(expense);
        var retrieved = await repository.GetByIdAsync(expense.GetId());

        Assert.NotNull(retrieved);
        Assert.Equal(expense.GetName().GetValue(), retrieved!.GetName().GetValue());
        Assert.Equal(expense.GetCategory().GetValue(), retrieved.GetCategory().GetValue());
        Assert.Equal(expense.GetMonthlyAmount().GetValue(), retrieved.GetMonthlyAmount().GetValue());
        Assert.Equal(expense.GetDueDay().GetValue(), retrieved.GetDueDay().GetValue());
        Assert.Equal(expense.GetStartDate().GetValue(), retrieved.GetStartDate().GetValue());
        Assert.Equal(expense.GetFrequency().GetValue(), retrieved.GetFrequency().GetValue());
        Assert.Equal(expense.GetStatus().GetValue(), retrieved.GetStatus().GetValue());
        Assert.Equal(expense.GetNote().GetValue(), retrieved.GetNote().GetValue());
    }

    [Fact]
    public async Task GetActiveAsync_MixedStatusSet_ReturnsOnlyActiveExpenses()
    {
        var repository = new InMemoryRecurringExpenseRepository();
        var active1 = CreateExpense("Aluguel", RecurringExpenseStatusType.Active);
        var active2 = CreateExpense("Internet", RecurringExpenseStatusType.Active);
        var paused = CreateExpense("Academia", RecurringExpenseStatusType.Paused);

        await repository.AddAsync(active1);
        await repository.AddAsync(active2);
        await repository.AddAsync(paused);

        var activeExpenses = await repository.GetActiveAsync();

        Assert.Equal(2, activeExpenses.Count);
        Assert.All(activeExpenses, e => Assert.Equal(RecurringExpenseStatusType.Active, e.GetStatus().GetValue()));
        Assert.Contains(activeExpenses, e => e.GetId() == active1.GetId());
        Assert.Contains(activeExpenses, e => e.GetId() == active2.GetId());
        Assert.DoesNotContain(activeExpenses, e => e.GetId() == paused.GetId());
    }
}
