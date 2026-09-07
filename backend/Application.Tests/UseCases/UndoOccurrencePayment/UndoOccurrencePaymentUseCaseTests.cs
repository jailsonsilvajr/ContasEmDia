using ContasEmDia.Application.UseCases.UndoOccurrencePayment;
using ContasEmDia.Domain;
using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Application.Tests.UseCases.UndoOccurrencePayment;

public class UndoOccurrencePaymentUseCaseTests
{
    private static RecurringExpense CreateExpense(
        int dueDay = 10,
        DateOnly? startDate = null,
        ReferencePeriod? currentReferencePeriod = null) => new(
            new ExpenseName("Aluguel"),
            new ExpenseCategory(ExpenseCategoryType.Housing),
            new Money(1500m),
            new DueDay(dueDay),
            new CalendarDate(startDate ?? new DateOnly(2026, 8, 1)),
            new Frequency(FrequencyType.Monthly),
            new RecurringExpenseStatus(RecurringExpenseStatusType.Active),
            new Note(null),
            currentReferencePeriod ?? new ReferencePeriod(2026, 8));

    private static (UndoOccurrencePaymentUseCase UseCase, InMemoryRecurringExpenseRepository Repository) CreateSut(DateOnly currentDate)
    {
        var repository = new InMemoryRecurringExpenseRepository();
        var repositoryManager = new FakeRepositoryManager(repository);
        var currentDateProvider = new FixedCurrentDateProvider(currentDate);
        var useCase = new UndoOccurrencePaymentUseCase(repositoryManager, currentDateProvider);

        return (useCase, repository);
    }

    [Fact]
    public async Task ExecuteAsync_PaidOccurrenceDueInPast_RevertsToOverdueWithNoPaidAmountOrDate()
    {
        var currentDate = new DateOnly(2026, 8, 20);
        var (useCase, repository) = CreateSut(currentDate);
        var expense = CreateExpense(dueDay: 5, currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrence = expense.GetOccurrences().Single();
        occurrence.MarkAsPaid(new Money(1500m), new CalendarDate(new DateOnly(2026, 8, 4)));
        repository.Seed(expense);

        var output = await useCase.ExecuteAsync(new UndoOccurrencePaymentUseCaseInput { OccurrenceId = occurrence.GetId() });

        Assert.Equal("Overdue", output.Occurrence.DerivedStatus);
        Assert.Null(output.Occurrence.PaidAmount);
        Assert.Null(output.Occurrence.PaymentDate);
    }

    [Fact]
    public async Task ExecuteAsync_PaidOccurrenceDueFarInFuture_RevertsToPending()
    {
        var currentDate = new DateOnly(2026, 8, 1);
        var (useCase, repository) = CreateSut(currentDate);
        var expense = CreateExpense(dueDay: 28, currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrence = expense.GetOccurrences().Single();
        occurrence.MarkAsPaid(new Money(1500m), new CalendarDate(new DateOnly(2026, 8, 1)));
        repository.Seed(expense);

        var output = await useCase.ExecuteAsync(new UndoOccurrencePaymentUseCaseInput { OccurrenceId = occurrence.GetId() });

        Assert.Equal("Pending", output.Occurrence.DerivedStatus);
    }

    [Fact]
    public async Task ExecuteAsync_NonExistentOccurrenceId_ThrowsKeyNotFoundException()
    {
        var (useCase, _) = CreateSut(new DateOnly(2026, 8, 15));

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => useCase.ExecuteAsync(new UndoOccurrencePaymentUseCaseInput
        {
            OccurrenceId = Guid.NewGuid()
        }));

        Assert.Equal("Ocorrência não encontrada.", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_OccurrenceNotYetPaid_ThrowsDomainRuleViolationException()
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrence = expense.GetOccurrences().Single();
        repository.Seed(expense);

        var exception = await Assert.ThrowsAsync<DomainRuleViolationException>(() => useCase.ExecuteAsync(new UndoOccurrencePaymentUseCaseInput
        {
            OccurrenceId = occurrence.GetId()
        }));

        Assert.Equal("Esta ocorrência ainda não foi paga.", exception.Message);
    }
}
