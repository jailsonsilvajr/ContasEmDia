using ContasEmDia.Application.UseCases.GetMonthlyPanel;
using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Application.Tests.UseCases.GetMonthlyPanel;

public class GetMonthlyPanelUseCaseTests
{
    private static RecurringExpense CreateExpense(
        string name = "Aluguel",
        ExpenseCategoryType category = ExpenseCategoryType.Housing,
        decimal monthlyAmount = 1500m,
        int dueDay = 10,
        DateOnly? startDate = null,
        RecurringExpenseStatusType status = RecurringExpenseStatusType.Active,
        ReferencePeriod? currentReferencePeriod = null) => new(
            new ExpenseName(name),
            new ExpenseCategory(category),
            new Money(monthlyAmount),
            new DueDay(dueDay),
            new CalendarDate(startDate ?? new DateOnly(2026, 8, 1)),
            new Frequency(FrequencyType.Monthly),
            new RecurringExpenseStatus(status),
            new Note(null),
            currentReferencePeriod ?? new ReferencePeriod(2026, 8));

    private static (GetMonthlyPanelUseCase UseCase, InMemoryRecurringExpenseRepository Repository) CreateSut(DateOnly currentDate)
    {
        var repository = new InMemoryRecurringExpenseRepository();
        var repositoryManager = new FakeRepositoryManager(repository);
        var currentDateProvider = new FixedCurrentDateProvider(currentDate);
        var useCase = new GetMonthlyPanelUseCase(repositoryManager, currentDateProvider);

        return (useCase, repository);
    }

    [Fact]
    public async Task ExecuteAsync_NoYearOrMonth_UsesCurrentPeriod()
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        repository.Seed(expense);

        var output = await useCase.ExecuteAsync(new GetMonthlyPanelUseCaseInput());

        Assert.True(output.IsSuccess);
        Assert.Equal(2026, output.Year);
        Assert.Equal(8, output.Month);
        Assert.Single(output.Occurrences!);
    }

    [Fact]
    public async Task ExecuteAsync_OnlyYearProvided_ReturnsPeriodFieldError()
    {
        var (useCase, _) = CreateSut(new DateOnly(2026, 8, 15));

        var output = await useCase.ExecuteAsync(new GetMonthlyPanelUseCaseInput { Year = "2026" });

        Assert.False(output.IsSuccess);
        var error = Assert.Single(output.Errors);
        Assert.Equal("period", error.Field);
        Assert.Equal("Informe mês e ano juntos, ou nenhum dos dois.", error.Message);
    }

    [Fact]
    public async Task ExecuteAsync_OnlyMonthProvided_ReturnsPeriodFieldError()
    {
        var (useCase, _) = CreateSut(new DateOnly(2026, 8, 15));

        var output = await useCase.ExecuteAsync(new GetMonthlyPanelUseCaseInput { Month = "8" });

        Assert.False(output.IsSuccess);
        var error = Assert.Single(output.Errors);
        Assert.Equal("period", error.Field);
        Assert.Equal("Informe mês e ano juntos, ou nenhum dos dois.", error.Message);
    }

    [Theory]
    [InlineData("abc", "8")]
    [InlineData("2026", "abc")]
    public async Task ExecuteAsync_NonNumericYearOrMonth_ReturnsPeriodFieldError(string year, string month)
    {
        var (useCase, _) = CreateSut(new DateOnly(2026, 8, 15));

        var output = await useCase.ExecuteAsync(new GetMonthlyPanelUseCaseInput { Year = year, Month = month });

        Assert.False(output.IsSuccess);
        var error = Assert.Single(output.Errors);
        Assert.Equal("period", error.Field);
        Assert.Equal("Mês e ano devem ser números válidos.", error.Message);
    }

    [Fact]
    public async Task ExecuteAsync_MonthOutOfRange_ReturnsPeriodFieldErrorWithDomainMessage()
    {
        var (useCase, _) = CreateSut(new DateOnly(2026, 8, 15));
        var expectedMessage = Assert.Throws<ArgumentException>(() => new ReferencePeriod(2026, 13)).Message;

        var output = await useCase.ExecuteAsync(new GetMonthlyPanelUseCaseInput { Year = "2026", Month = "13" });

        Assert.False(output.IsSuccess);
        var error = Assert.Single(output.Errors);
        Assert.Equal("period", error.Field);
        Assert.Equal(expectedMessage, error.Message);
    }

    [Fact]
    public async Task ExecuteAsync_OccurrenceDueInPast_MapsDerivedStatusAsOverdue()
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 20));
        var expense = CreateExpense(dueDay: 5, currentReferencePeriod: new ReferencePeriod(2026, 8));
        repository.Seed(expense);

        var output = await useCase.ExecuteAsync(new GetMonthlyPanelUseCaseInput { Year = "2026", Month = "8" });

        Assert.True(output.IsSuccess);
        var occurrence = Assert.Single(output.Occurrences!);
        Assert.Equal("Overdue", occurrence.DerivedStatus);
        Assert.Null(occurrence.PaidAmount);
        Assert.Null(occurrence.PaymentDate);
    }

    [Fact]
    public async Task ExecuteAsync_PeriodWithNoOccurrences_ReturnsEmptyList()
    {
        var (useCase, _) = CreateSut(new DateOnly(2026, 8, 15));

        var output = await useCase.ExecuteAsync(new GetMonthlyPanelUseCaseInput { Year = "2020", Month = "1" });

        Assert.True(output.IsSuccess);
        Assert.Empty(output.Occurrences!);
    }

    [Fact]
    public async Task ExecuteAsync_Occurrence_CarriesOwningRecurringExpenseId()
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        repository.Seed(expense);

        var output = await useCase.ExecuteAsync(new GetMonthlyPanelUseCaseInput { Year = "2026", Month = "8" });

        Assert.True(output.IsSuccess);
        var occurrence = Assert.Single(output.Occurrences!);
        Assert.Equal(expense.GetId(), occurrence.RecurringExpenseId);
    }
}
