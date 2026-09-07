using ContasEmDia.Application.UseCases.MarkOccurrenceAsPaid;
using ContasEmDia.Domain;
using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Application.Tests.UseCases.MarkOccurrenceAsPaid;

public class MarkOccurrenceAsPaidUseCaseTests
{
    private static RecurringExpense CreateExpense(
        decimal monthlyAmount = 1500m,
        int dueDay = 10,
        DateOnly? startDate = null,
        ReferencePeriod? currentReferencePeriod = null) => new(
            new ExpenseName("Aluguel"),
            new ExpenseCategory(ExpenseCategoryType.Housing),
            new Money(monthlyAmount),
            new DueDay(dueDay),
            new CalendarDate(startDate ?? new DateOnly(2026, 8, 1)),
            new Frequency(FrequencyType.Monthly),
            new RecurringExpenseStatus(RecurringExpenseStatusType.Active),
            new Note(null),
            currentReferencePeriod ?? new ReferencePeriod(2026, 8));

    private static (MarkOccurrenceAsPaidUseCase UseCase, InMemoryRecurringExpenseRepository Repository) CreateSut(DateOnly currentDate)
    {
        var repository = new InMemoryRecurringExpenseRepository();
        var repositoryManager = new FakeRepositoryManager(repository);
        var currentDateProvider = new FixedCurrentDateProvider(currentDate);
        var useCase = new MarkOccurrenceAsPaidUseCase(repositoryManager, currentDateProvider);

        return (useCase, repository);
    }

    [Fact]
    public async Task ExecuteAsync_ValidAmountAndDate_MarksOccurrenceAsPaidWithProvidedValues()
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrenceId = expense.GetOccurrences().Single().GetId();
        repository.Seed(expense);

        var output = await useCase.ExecuteAsync(new MarkOccurrenceAsPaidUseCaseInput
        {
            OccurrenceId = occurrenceId,
            PaidAmountRaw = "1500,00",
            PaymentDateRaw = "18/08/2026"
        });

        Assert.Equal("Paid", output.Occurrence.DerivedStatus);
        Assert.Equal(1500.00m, output.Occurrence.PaidAmount);
        Assert.Equal(new DateOnly(2026, 8, 18), output.Occurrence.PaymentDate);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not-a-number")]
    public async Task ExecuteAsync_EmptyOrInvalidAmount_FallsBackToExpectedAmount(string? rawAmount)
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        var expense = CreateExpense(monthlyAmount: 1850m, currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrenceId = expense.GetOccurrences().Single().GetId();
        repository.Seed(expense);

        var output = await useCase.ExecuteAsync(new MarkOccurrenceAsPaidUseCaseInput
        {
            OccurrenceId = occurrenceId,
            PaidAmountRaw = rawAmount,
            PaymentDateRaw = "18/08/2026"
        });

        Assert.Equal(1850m, output.Occurrence.PaidAmount);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not-a-date")]
    public async Task ExecuteAsync_EmptyOrInvalidDate_FallsBackToCurrentDate(string? rawDate)
    {
        var currentDate = new DateOnly(2026, 8, 15);
        var (useCase, repository) = CreateSut(currentDate);
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrenceId = expense.GetOccurrences().Single().GetId();
        repository.Seed(expense);

        var output = await useCase.ExecuteAsync(new MarkOccurrenceAsPaidUseCaseInput
        {
            OccurrenceId = occurrenceId,
            PaidAmountRaw = "1500,00",
            PaymentDateRaw = rawDate
        });

        Assert.Equal(currentDate, output.Occurrence.PaymentDate);
    }

    [Fact]
    public async Task ExecuteAsync_NonExistentOccurrenceId_ThrowsKeyNotFoundException()
    {
        var (useCase, _) = CreateSut(new DateOnly(2026, 8, 15));

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => useCase.ExecuteAsync(new MarkOccurrenceAsPaidUseCaseInput
        {
            OccurrenceId = Guid.NewGuid()
        }));

        Assert.Equal("Ocorrência não encontrada.", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_OccurrenceAlreadyPaid_ThrowsDomainRuleViolationException()
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrence = expense.GetOccurrences().Single();
        occurrence.MarkAsPaid(new Money(1500m), new CalendarDate(new DateOnly(2026, 8, 10)));
        repository.Seed(expense);

        var exception = await Assert.ThrowsAsync<DomainRuleViolationException>(() => useCase.ExecuteAsync(new MarkOccurrenceAsPaidUseCaseInput
        {
            OccurrenceId = occurrence.GetId()
        }));

        Assert.Equal("Esta ocorrência já está paga.", exception.Message);
    }
}
