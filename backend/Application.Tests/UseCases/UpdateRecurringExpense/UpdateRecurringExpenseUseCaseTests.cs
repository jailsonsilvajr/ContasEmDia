using ContasEmDia.Application.UseCases.UpdateRecurringExpense;
using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Application.Tests.UseCases.UpdateRecurringExpense;

public class UpdateRecurringExpenseUseCaseTests
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

    private static UpdateRecurringExpenseUseCaseInput CreateInput(
        Guid id,
        string name = "Aluguel",
        string category = "Housing",
        decimal monthlyAmount = 1500m,
        int dueDay = 10,
        string startDate = "2026-08-01",
        string endDate = "2026-08-31",
        string status = "Active",
        string? note = null) => new()
    {
        Id = id,
        Name = name,
        Category = category,
        MonthlyAmount = monthlyAmount,
        DueDay = dueDay,
        StartDate = startDate,
        EndDate = endDate,
        Status = status,
        Note = note,
    };

    private static (UpdateRecurringExpenseUseCase UseCase, InMemoryRecurringExpenseRepository Repository) CreateSut(DateOnly currentDate)
    {
        var repository = new InMemoryRecurringExpenseRepository();
        var repositoryManager = new FakeRepositoryManager(repository);
        var currentDateProvider = new FixedCurrentDateProvider(currentDate);
        var useCase = new UpdateRecurringExpenseUseCase(repositoryManager, currentDateProvider);

        return (useCase, repository);
    }

    [Fact]
    public async Task ExecuteAsync_EditingFieldsWithExistingOccurrence_ReturnsUpdatedDataAndOccurrenceKeepsOldSnapshot()
    {
        var expense = CreateExpense(
            name: "Aluguel",
            category: ExpenseCategoryType.Housing,
            monthlyAmount: 1500m,
            dueDay: 10,
            startDate: new DateOnly(2026, 8, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrence = expense.GetOccurrences().Single();

        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        repository.Seed(expense);

        var input = CreateInput(
            expense.GetId(),
            name: "Aluguel do apartamento",
            category: "Services",
            monthlyAmount: 1900m,
            dueDay: 20,
            startDate: "2026-08-01",
            status: "Active");

        var output = await useCase.ExecuteAsync(input);

        Assert.True(output.IsSuccess);
        Assert.Equal("Aluguel do apartamento", output.RecurringExpense!.Name);
        Assert.Equal("Services", output.RecurringExpense.Category);
        Assert.Equal(1900m, output.RecurringExpense.MonthlyAmount);
        Assert.Equal(20, output.RecurringExpense.DueDay);

        Assert.Equal("Aluguel", occurrence.GetName().GetValue());
        Assert.Equal(ExpenseCategoryType.Housing, occurrence.GetCategory().GetValue());
        Assert.Equal(1500m, occurrence.GetExpectedAmount().GetValue());
    }

    [Fact]
    public async Task ExecuteAsync_NoFieldChanged_ReturnsSuccessAndTouchesNoOccurrence()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrenceCountBefore = expense.GetOccurrences().Count;
        var occurrence = expense.GetOccurrences().Single();

        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        repository.Seed(expense);

        var input = CreateInput(expense.GetId());

        var output = await useCase.ExecuteAsync(input);

        Assert.True(output.IsSuccess);
        Assert.Equal(occurrenceCountBefore, expense.GetOccurrences().Count);
        Assert.Equal(occurrence.GetId(), expense.GetOccurrences().Single().GetId());
    }

    [Fact]
    public async Task ExecuteAsync_ExpenseWithPaidOccurrence_SucceedsAndDoesNotAlterPaymentData()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrence = expense.GetOccurrences().Single();
        var paidAmount = new Money(1500m);
        var paymentDate = new CalendarDate(new DateOnly(2026, 8, 5));
        expense.MarkOccurrenceAsPaid(occurrence.GetId(), paidAmount, paymentDate);

        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        repository.Seed(expense);

        var input = CreateInput(expense.GetId(), name: "Aluguel renomeado");

        var output = await useCase.ExecuteAsync(input);

        Assert.True(output.IsSuccess);
        Assert.Equal(paidAmount.GetValue(), occurrence.GetPaidAmount()!.GetValue());
        Assert.Equal(paymentDate.GetValue(), occurrence.GetPaymentDate()!.GetValue());
    }

    [Fact]
    public async Task ExecuteAsync_StatusActiveToPaused_SucceedsAndDoesNotAlterOccurrences()
    {
        var expense = CreateExpense(status: RecurringExpenseStatusType.Active, currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrenceCountBefore = expense.GetOccurrences().Count;

        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        repository.Seed(expense);

        var input = CreateInput(expense.GetId(), status: "Paused");

        var output = await useCase.ExecuteAsync(input);

        Assert.True(output.IsSuccess);
        Assert.Equal("Paused", output.RecurringExpense!.Status);
        Assert.Equal(occurrenceCountBefore, expense.GetOccurrences().Count);
    }

    [Fact]
    public async Task ExecuteAsync_StatusPausedToActiveStartDateBegunNoOccurrenceForCurrentPeriod_GeneratesPendingOccurrence()
    {
        var expense = CreateExpense(
            status: RecurringExpenseStatusType.Paused,
            monthlyAmount: 1500m,
            startDate: new DateOnly(2026, 1, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 8));

        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        repository.Seed(expense);

        var input = CreateInput(expense.GetId(), monthlyAmount: 1500m, startDate: "2026-01-01", endDate: "2026-08-31", status: "Active");

        var output = await useCase.ExecuteAsync(input);

        Assert.True(output.IsSuccess);
        Assert.Equal("Active", output.RecurringExpense!.Status);
        var occurrence = Assert.Single(expense.GetOccurrences());
        Assert.Equal(1500m, occurrence.GetExpectedAmount().GetValue());
    }

    [Fact]
    public async Task ExecuteAsync_StatusPausedToActiveOccurrenceAlreadyExistsForCurrentPeriod_GeneratesNoAdditionalOccurrence()
    {
        var expense = CreateExpense(
            status: RecurringExpenseStatusType.Active,
            startDate: new DateOnly(2026, 1, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 8));
        expense.Pause();

        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        repository.Seed(expense);

        var input = CreateInput(expense.GetId(), startDate: "2026-01-01", endDate: "2026-08-31", status: "Active");

        var output = await useCase.ExecuteAsync(input);

        Assert.True(output.IsSuccess);
        Assert.Single(expense.GetOccurrences());
    }

    [Fact]
    public async Task ExecuteAsync_StatusPausedToActiveStartDateInFuture_GeneratesNoOccurrence()
    {
        var expense = CreateExpense(
            status: RecurringExpenseStatusType.Paused,
            startDate: new DateOnly(2026, 9, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 8));

        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        repository.Seed(expense);

        var input = CreateInput(expense.GetId(), startDate: "2026-09-01", endDate: "2026-09-30", status: "Active");

        var output = await useCase.ExecuteAsync(input);

        Assert.True(output.IsSuccess);
        Assert.Equal("Active", output.RecurringExpense!.Status);
        Assert.Empty(expense.GetOccurrences());
    }

    [Fact]
    public async Task ExecuteAsync_NonExistentId_ThrowsKeyNotFoundException()
    {
        var (useCase, _) = CreateSut(new DateOnly(2026, 8, 15));

        var input = CreateInput(Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => useCase.ExecuteAsync(input));

        Assert.Equal("Despesa recorrente não encontrada.", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ExtendingEndDate_ReturnsSuccessWithNewOccurrencesVisibleOnAggregate()
    {
        var expense = CreateExpense(
            startDate: new DateOnly(2026, 8, 1),
            endDate: new DateOnly(2026, 8, 31),
            currentReferencePeriod: new ReferencePeriod(2026, 8));

        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        repository.Seed(expense);

        var input = CreateInput(expense.GetId(), endDate: "2026-10-31");

        var output = await useCase.ExecuteAsync(input);

        Assert.True(output.IsSuccess);
        Assert.Equal(new DateOnly(2026, 10, 31), output.RecurringExpense!.EndDate);
        Assert.Equal(3, expense.GetOccurrences().Count); // Aug, Sep, Oct
    }

    [Fact]
    public async Task ExecuteAsync_ChangeEndDateDomainViolation_ReturnsFailureAndDoesNotMutateAggregate()
    {
        var expense = CreateExpense(
            startDate: new DateOnly(2026, 8, 1),
            endDate: new DateOnly(2026, 8, 31),
            currentReferencePeriod: new ReferencePeriod(2026, 8));

        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        repository.Seed(expense);

        // Beyond the 1-year vigência cap from startDate.
        var input = CreateInput(expense.GetId(), endDate: "2027-08-02");

        var output = await useCase.ExecuteAsync(input);

        Assert.False(output.IsSuccess);
        var error = Assert.Single(output.Errors);
        Assert.Equal("endDate", error.Field);
        Assert.Equal(new DateOnly(2026, 8, 31), expense.GetEndDate().GetValue());
        Assert.Single(expense.GetOccurrences());
    }

    [Fact]
    public async Task ExecuteAsync_ChangeStartDateDomainViolation_ReturnsFailureAndDoesNotMutateAggregate()
    {
        var expense = CreateExpense(
            startDate: new DateOnly(2026, 8, 1),
            endDate: new DateOnly(2026, 8, 31),
            currentReferencePeriod: new ReferencePeriod(2026, 8));

        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        repository.Seed(expense);

        // New startDate would no longer be before the current endDate.
        var input = CreateInput(expense.GetId(), startDate: "2026-09-01");

        var output = await useCase.ExecuteAsync(input);

        Assert.False(output.IsSuccess);
        var error = Assert.Single(output.Errors);
        Assert.Equal("startDate", error.Field);
        Assert.Equal(new DateOnly(2026, 8, 1), expense.GetStartDate().GetValue());
        Assert.Single(expense.GetOccurrences());
    }

    [Fact]
    public async Task ExecuteAsync_ReducingEndDateWithNoPaidOccurrenceInRange_ReturnsSuccessWithTrimmedOccurrenceList()
    {
        var expense = CreateExpense(
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 6, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 1));
        Assert.Equal(6, expense.GetOccurrences().Count);

        var (useCase, repository) = CreateSut(new DateOnly(2026, 1, 15));
        repository.Seed(expense);

        var input = CreateInput(expense.GetId(), startDate: "2026-01-01", endDate: "2026-03-15");

        var output = await useCase.ExecuteAsync(input);

        Assert.True(output.IsSuccess);
        Assert.Equal(3, expense.GetOccurrences().Count);
    }

    [Fact]
    public async Task ExecuteAsync_ReducingEndDateExcludingAPaidOccurrence_ReturnsFailureAndDoesNotMutateAggregate()
    {
        var expense = CreateExpense(
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 6, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 1));
        var mayOccurrence = expense.GetOccurrencesForPeriod(new ReferencePeriod(2026, 5)).Single();
        expense.MarkOccurrenceAsPaid(mayOccurrence.GetId(), new Money(1500m), new CalendarDate(new DateOnly(2026, 5, 5)));

        var (useCase, repository) = CreateSut(new DateOnly(2026, 1, 15));
        repository.Seed(expense);

        var input = CreateInput(expense.GetId(), startDate: "2026-01-01", endDate: "2026-03-15");

        var output = await useCase.ExecuteAsync(input);

        Assert.False(output.IsSuccess);
        var error = Assert.Single(output.Errors);
        Assert.Equal("endDate", error.Field);
        Assert.Equal(new DateOnly(2026, 6, 1), expense.GetEndDate().GetValue());
        Assert.Equal(6, expense.GetOccurrences().Count);
    }
}
