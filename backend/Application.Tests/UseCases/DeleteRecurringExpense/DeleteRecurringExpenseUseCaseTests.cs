using ContasEmDia.Application.UseCases.DeleteRecurringExpense;
using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Application.Tests.UseCases.DeleteRecurringExpense;

public class DeleteRecurringExpenseUseCaseTests
{
    private static RecurringExpense CreateExpense(
        string name = "Aluguel",
        ExpenseCategoryType category = ExpenseCategoryType.Housing,
        decimal monthlyAmount = 1500m,
        int dueDay = 10,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        RecurringExpenseStatusType status = RecurringExpenseStatusType.Active,
        string? note = null,
        ReferencePeriod? currentReferencePeriod = null)
    {
        var resolvedStartDate = startDate ?? new DateOnly(2026, 8, 1);
        var resolvedCurrentReferencePeriod = currentReferencePeriod ?? new ReferencePeriod(2026, 8);
        var startPeriod = ReferencePeriod.FromDate(resolvedStartDate);
        var defaultEndPeriod = startPeriod > resolvedCurrentReferencePeriod ? startPeriod : resolvedCurrentReferencePeriod;
        var defaultEndDate = new DateOnly(
            defaultEndPeriod.Year,
            defaultEndPeriod.Month,
            DateTime.DaysInMonth(defaultEndPeriod.Year, defaultEndPeriod.Month));

        return new RecurringExpense(
            new ExpenseName(name),
            new ExpenseCategory(category),
            new Money(monthlyAmount),
            new DueDay(dueDay),
            new CalendarDate(resolvedStartDate),
            new CalendarDate(endDate ?? defaultEndDate),
            new Frequency(FrequencyType.Monthly),
            new RecurringExpenseStatus(status),
            new Note(note),
            resolvedCurrentReferencePeriod);
    }

    private static (DeleteRecurringExpenseUseCase UseCase, InMemoryRecurringExpenseRepository Repository) CreateSut(DateOnly currentDate)
    {
        var repository = new InMemoryRecurringExpenseRepository();
        var repositoryManager = new FakeRepositoryManager(repository);
        var currentDateProvider = new FixedCurrentDateProvider(currentDate);
        var useCase = new DeleteRecurringExpenseUseCase(repositoryManager, currentDateProvider);

        return (useCase, repository);
    }

    [Fact]
    public async Task ExecuteAsync_PendingOnlyScopeWithPaidAndPendingOccurrence_ReturnsSuccessRemovesPendingKeepsPaidAndCallsUpdateNotRemove()
    {
        var expense = CreateExpense(
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 2, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 1));
        var paidOccurrence = expense.GetOccurrencesForPeriod(new ReferencePeriod(2026, 1)).Single();
        expense.MarkOccurrenceAsPaid(paidOccurrence.GetId(), new Money(1500m), new CalendarDate(new DateOnly(2026, 1, 5)));

        var (useCase, repository) = CreateSut(new DateOnly(2026, 1, 15));
        repository.Seed(expense);

        var output = await useCase.ExecuteAsync(new DeleteRecurringExpenseUseCaseInput { Id = expense.GetId(), Scope = "pending-only" });

        Assert.True(output.IsSuccess);
        Assert.Equal("pending-only", output.Data!.Scope);
        Assert.Equal(expense.GetId(), output.Data.RecurringExpenseId);

        var remaining = Assert.Single(expense.GetOccurrences());
        Assert.Equal(paidOccurrence.GetId(), remaining.GetId());

        Assert.True(repository.UpdateAsyncCalled);
        Assert.False(repository.RemoveAsyncCalled);
    }

    [Fact]
    public async Task ExecuteAsync_PendingOnlyScopeWithOnlyPaidOccurrences_ReturnsSuccessRemovesNothingAndMarksDeleted()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrence = expense.GetOccurrences().Single();
        expense.MarkOccurrenceAsPaid(occurrence.GetId(), new Money(1500m), new CalendarDate(new DateOnly(2026, 8, 5)));

        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        repository.Seed(expense);

        var output = await useCase.ExecuteAsync(new DeleteRecurringExpenseUseCaseInput { Id = expense.GetId(), Scope = "pending-only" });

        Assert.True(output.IsSuccess);
        Assert.Single(expense.GetOccurrences());
        Assert.True(expense.IsDeleted());
    }

    [Fact]
    public async Task ExecuteAsync_NonExistentId_ThrowsKeyNotFoundExceptionBeforeAnyRepositoryMutation()
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            useCase.ExecuteAsync(new DeleteRecurringExpenseUseCaseInput { Id = Guid.NewGuid(), Scope = "pending-only" }));

        Assert.Equal("Despesa recorrente não encontrada.", exception.Message);
        Assert.False(repository.UpdateAsyncCalled);
        Assert.False(repository.RemoveAsyncCalled);
    }

    [Fact]
    public async Task ExecuteAsync_AllScopeWithPaidAndPendingOccurrences_ReturnsSuccessAndCallsRemoveNotUpdate()
    {
        var expense = CreateExpense(
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 2, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 1));
        var paidOccurrence = expense.GetOccurrencesForPeriod(new ReferencePeriod(2026, 1)).Single();
        expense.MarkOccurrenceAsPaid(paidOccurrence.GetId(), new Money(1500m), new CalendarDate(new DateOnly(2026, 1, 5)));

        var (useCase, repository) = CreateSut(new DateOnly(2026, 1, 15));
        repository.Seed(expense);

        var output = await useCase.ExecuteAsync(new DeleteRecurringExpenseUseCaseInput { Id = expense.GetId(), Scope = "all" });

        Assert.True(output.IsSuccess);
        Assert.Equal("all", output.Data!.Scope);

        Assert.True(repository.RemoveAsyncCalled);
        Assert.False(repository.UpdateAsyncCalled);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalido")]
    public async Task ExecuteAsync_MissingOrInvalidScope_ReturnsFailureWithScopeFieldErrorAndNoRepositoryMutation(string? scope)
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        repository.Seed(expense);

        var output = await useCase.ExecuteAsync(new DeleteRecurringExpenseUseCaseInput { Id = expense.GetId(), Scope = scope });

        Assert.False(output.IsSuccess);
        var error = Assert.Single(output.Errors);
        Assert.Equal("scope", error.Field);
        Assert.Equal("Informe a abrangência da exclusão: \"pending-only\" ou \"all\".", error.Message);

        Assert.False(repository.UpdateAsyncCalled);
        Assert.False(repository.RemoveAsyncCalled);
    }
}
