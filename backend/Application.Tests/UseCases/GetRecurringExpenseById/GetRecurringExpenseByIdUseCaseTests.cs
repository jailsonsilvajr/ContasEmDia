using ContasEmDia.Application.UseCases.GetRecurringExpenseById;
using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Application.Tests.UseCases.GetRecurringExpenseById;

public class GetRecurringExpenseByIdUseCaseTests
{
    private static RecurringExpense CreateExpense() =>
        new(
            new ExpenseName("Aluguel"),
            new ExpenseCategory(ExpenseCategoryType.Housing),
            new Money(1500m),
            new DueDay(10),
            new CalendarDate(new DateOnly(2026, 8, 1)),
            new Frequency(FrequencyType.Monthly),
            new RecurringExpenseStatus(RecurringExpenseStatusType.Active),
            new Note("Nota"),
            new ReferencePeriod(2026, 8));

    private static (GetRecurringExpenseByIdUseCase UseCase, InMemoryRecurringExpenseRepository Repository) CreateSut()
    {
        var repository = new InMemoryRecurringExpenseRepository();
        var repositoryManager = new FakeRepositoryManager(repository);
        var useCase = new GetRecurringExpenseByIdUseCase(repositoryManager);

        return (useCase, repository);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingId_ReturnsAllEditableFieldsMatchingTheStoredExpense()
    {
        var expense = CreateExpense();
        var (useCase, repository) = CreateSut();
        repository.Seed(expense);

        var output = await useCase.ExecuteAsync(new GetRecurringExpenseByIdUseCaseInput { Id = expense.GetId() });

        Assert.Equal(expense.GetId(), output.RecurringExpense.Id);
        Assert.Equal(expense.GetName().GetValue(), output.RecurringExpense.Name);
        Assert.Equal(expense.GetCategory().GetValue().ToString(), output.RecurringExpense.Category);
        Assert.Equal(expense.GetMonthlyAmount().GetValue(), output.RecurringExpense.MonthlyAmount);
        Assert.Equal(expense.GetDueDay().GetValue(), output.RecurringExpense.DueDay);
        Assert.Equal(expense.GetStartDate().GetValue(), output.RecurringExpense.StartDate);
        Assert.Equal(expense.GetFrequency().GetValue().ToString(), output.RecurringExpense.Frequency);
        Assert.Equal(expense.GetStatus().GetValue().ToString(), output.RecurringExpense.Status);
        Assert.Equal(expense.GetNote().GetValue(), output.RecurringExpense.Note);
    }

    [Fact]
    public async Task ExecuteAsync_NonExistentId_ThrowsKeyNotFoundException()
    {
        var (useCase, _) = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => useCase.ExecuteAsync(new GetRecurringExpenseByIdUseCaseInput { Id = Guid.NewGuid() }));

        Assert.Equal("Despesa recorrente não encontrada.", exception.Message);
    }
}
