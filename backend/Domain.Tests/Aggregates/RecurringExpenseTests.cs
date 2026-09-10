using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Tests.Aggregates;

public class RecurringExpenseTests
{
    private static RecurringExpense CreateExpense(
        string name = "Aluguel",
        ExpenseCategoryType category = ExpenseCategoryType.Housing,
        decimal monthlyAmount = 1500m,
        int dueDay = 10,
        DateOnly? startDate = null,
        RecurringExpenseStatusType status = RecurringExpenseStatusType.Active,
        string? note = null,
        ReferencePeriod? currentReferencePeriod = null)
    {
        return new RecurringExpense(
            new ExpenseName(name),
            new ExpenseCategory(category),
            new Money(monthlyAmount),
            new DueDay(dueDay),
            new CalendarDate(startDate ?? new DateOnly(2026, 8, 1)),
            new Frequency(FrequencyType.Monthly),
            new RecurringExpenseStatus(status),
            new Note(note),
            currentReferencePeriod ?? new ReferencePeriod(2026, 8));
    }

    [Fact]
    public void Constructor_ActiveDespesaWithStartDateOnOrBeforeCurrentCompetencia_GeneratesExactlyOnePendingOccurrence()
    {
        var expense = CreateExpense(
            monthlyAmount: 1500m,
            startDate: new DateOnly(2026, 8, 1),
            status: RecurringExpenseStatusType.Active,
            currentReferencePeriod: new ReferencePeriod(2026, 8));

        var occurrences = expense.GetOccurrences();

        Assert.Single(occurrences);
        var occurrence = occurrences.Single();
        Assert.Equal(OccurrenceStatusType.Pending, occurrence.GetStatus().GetValue());
        Assert.Equal(expense.GetMonthlyAmount().GetValue(), occurrence.GetExpectedAmount().GetValue());
    }

    [Theory]
    [InlineData(2026, 2, 28)] // February, non-leap year
    [InlineData(2028, 2, 29)] // February, leap year
    [InlineData(2026, 4, 30)] // April, short month
    [InlineData(2026, 6, 30)] // June, short month
    [InlineData(2026, 9, 30)] // September, short month
    [InlineData(2026, 11, 30)] // November, short month
    public void Constructor_DueDay31InShortMonth_ClampsDueDateToLastDayOfMonth(int year, int month, int expectedDay)
    {
        var expense = CreateExpense(
            dueDay: 31,
            startDate: new DateOnly(year, 1, 1),
            currentReferencePeriod: new ReferencePeriod(year, month));

        var occurrence = expense.GetOccurrences().Single();

        Assert.Equal(new DateOnly(year, month, expectedDay), occurrence.GetDueDate().GetValue());
    }

    [Fact]
    public void Constructor_GeneratedOccurrence_SnapshotsNameCategoryAndAmountFromExpense()
    {
        var expense = CreateExpense(
            name: "Internet",
            category: ExpenseCategoryType.Services,
            monthlyAmount: 120.90m,
            startDate: new DateOnly(2026, 8, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 8));

        var occurrence = expense.GetOccurrences().Single();

        Assert.Equal(expense.GetName().GetValue(), occurrence.GetName().GetValue());
        Assert.Equal(expense.GetCategory().GetValue(), occurrence.GetCategory().GetValue());
        Assert.Equal(expense.GetMonthlyAmount().GetValue(), occurrence.GetExpectedAmount().GetValue());
    }

    [Fact]
    public void Constructor_PausedDespesa_GeneratesNoOccurrence()
    {
        var expense = CreateExpense(
            status: RecurringExpenseStatusType.Paused,
            startDate: new DateOnly(2026, 1, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 8));

        Assert.Empty(expense.GetOccurrences());
    }

    [Fact]
    public void Constructor_ActiveDespesaWithFutureStartCompetencia_GeneratesNoOccurrence()
    {
        var expense = CreateExpense(
            status: RecurringExpenseStatusType.Active,
            startDate: new DateOnly(2026, 9, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 8));

        Assert.Empty(expense.GetOccurrences());
    }

    [Fact]
    public void GetOccurrencesForPeriod_PeriodMatchingGeneratedOccurrence_ReturnsThatOccurrence()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));

        var occurrences = expense.GetOccurrencesForPeriod(new ReferencePeriod(2026, 8));

        Assert.Single(occurrences);
        Assert.Equal(expense.GetOccurrences().Single().GetId(), occurrences.Single().GetId());
    }

    [Fact]
    public void GetOccurrencesForPeriod_PeriodWithNoOccurrences_ReturnsEmpty()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));

        var occurrences = expense.GetOccurrencesForPeriod(new ReferencePeriod(2026, 9));

        Assert.Empty(occurrences);
    }

    [Fact]
    public void FindOccurrence_ExistingId_ReturnsOccurrence()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrence = expense.GetOccurrences().Single();

        var found = expense.FindOccurrence(occurrence.GetId());

        Assert.NotNull(found);
        Assert.Equal(occurrence.GetId(), found.GetId());
    }

    [Fact]
    public void FindOccurrence_NonExistentId_ReturnsNull()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));

        var found = expense.FindOccurrence(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public void MarkOccurrenceAsPaid_ExistingId_DelegatesToOccurrence()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrence = expense.GetOccurrences().Single();
        var paidAmount = new Money(1500m);
        var paymentDate = new CalendarDate(new DateOnly(2026, 8, 5));

        expense.MarkOccurrenceAsPaid(occurrence.GetId(), paidAmount, paymentDate);

        Assert.Equal(paidAmount.GetValue(), occurrence.GetPaidAmount()!.GetValue());
        Assert.Equal(paymentDate.GetValue(), occurrence.GetPaymentDate()!.GetValue());
    }

    [Fact]
    public void MarkOccurrenceAsPaid_NonExistentId_ThrowsKeyNotFoundException()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));

        var exception = Assert.Throws<KeyNotFoundException>(
            () => expense.MarkOccurrenceAsPaid(Guid.NewGuid(), new Money(1500m), new CalendarDate(new DateOnly(2026, 8, 5))));

        Assert.Equal("Ocorrência não encontrada.", exception.Message);
    }

    [Fact]
    public void UndoOccurrencePayment_ExistingPaidId_DelegatesToOccurrence()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrence = expense.GetOccurrences().Single();
        expense.MarkOccurrenceAsPaid(occurrence.GetId(), new Money(1500m), new CalendarDate(new DateOnly(2026, 8, 5)));

        expense.UndoOccurrencePayment(occurrence.GetId());

        Assert.Null(occurrence.GetPaidAmount());
        Assert.Null(occurrence.GetPaymentDate());
    }

    [Fact]
    public void UndoOccurrencePayment_NonExistentId_ThrowsKeyNotFoundException()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));

        var exception = Assert.Throws<KeyNotFoundException>(() => expense.UndoOccurrencePayment(Guid.NewGuid()));

        Assert.Equal("Ocorrência não encontrada.", exception.Message);
    }

    [Fact]
    public void Rename_ReplacesNameOnly_DoesNotAffectExistingOccurrences()
    {
        var expense = CreateExpense(name: "Aluguel", currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrence = expense.GetOccurrences().Single();

        expense.Rename(new ExpenseName("Aluguel do apartamento"));

        Assert.Equal("Aluguel do apartamento", expense.GetName().GetValue());
        Assert.Single(expense.GetOccurrences());
        Assert.Equal("Aluguel", occurrence.GetName().GetValue());
    }

    [Fact]
    public void ChangeCategory_ReplacesCategoryOnly_DoesNotAffectExistingOccurrences()
    {
        var expense = CreateExpense(category: ExpenseCategoryType.Housing, currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrence = expense.GetOccurrences().Single();

        expense.ChangeCategory(new ExpenseCategory(ExpenseCategoryType.Services));

        Assert.Equal(ExpenseCategoryType.Services, expense.GetCategory().GetValue());
        Assert.Single(expense.GetOccurrences());
        Assert.Equal(ExpenseCategoryType.Housing, occurrence.GetCategory().GetValue());
    }

    [Fact]
    public void ChangeMonthlyAmount_ReplacesMonthlyAmountOnly_DoesNotAffectExistingOccurrences()
    {
        var expense = CreateExpense(monthlyAmount: 1500m, currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrence = expense.GetOccurrences().Single();

        expense.ChangeMonthlyAmount(new Money(1900m));

        Assert.Equal(1900m, expense.GetMonthlyAmount().GetValue());
        Assert.Single(expense.GetOccurrences());
        Assert.Equal(1500m, occurrence.GetExpectedAmount().GetValue());
    }

    [Fact]
    public void ChangeDueDay_ReplacesDueDayOnly_DoesNotAffectExistingOccurrences()
    {
        var expense = CreateExpense(dueDay: 10, currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrence = expense.GetOccurrences().Single();
        var originalDueDate = occurrence.GetDueDate().GetValue();

        expense.ChangeDueDay(new DueDay(20));

        Assert.Equal(20, expense.GetDueDay().GetValue());
        Assert.Single(expense.GetOccurrences());
        Assert.Equal(originalDueDate, occurrence.GetDueDate().GetValue());
    }

    [Fact]
    public void ChangeStartDate_ReplacesStartDateOnly_DoesNotAffectExistingOccurrences()
    {
        var expense = CreateExpense(startDate: new DateOnly(2026, 8, 1), currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrenceCountBefore = expense.GetOccurrences().Count;

        expense.ChangeStartDate(new CalendarDate(new DateOnly(2026, 7, 1)));

        Assert.Equal(new DateOnly(2026, 7, 1), expense.GetStartDate().GetValue());
        Assert.Equal(occurrenceCountBefore, expense.GetOccurrences().Count);
    }

    [Fact]
    public void ChangeNote_ReplacesNoteOnly_DoesNotAffectExistingOccurrences()
    {
        var expense = CreateExpense(note: "Nota original", currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrenceCountBefore = expense.GetOccurrences().Count;

        expense.ChangeNote(new Note("Nota atualizada"));

        Assert.Equal("Nota atualizada", expense.GetNote().GetValue());
        Assert.Equal(occurrenceCountBefore, expense.GetOccurrences().Count);
    }

    [Fact]
    public void Pause_SetsStatusToPaused_DoesNotAffectExistingOccurrences()
    {
        var expense = CreateExpense(status: RecurringExpenseStatusType.Active, currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrenceCountBefore = expense.GetOccurrences().Count;

        expense.Pause();

        Assert.Equal(RecurringExpenseStatusType.Paused, expense.GetStatus().GetValue());
        Assert.Equal(occurrenceCountBefore, expense.GetOccurrences().Count);
    }

    [Fact]
    public void Reactivate_StartDateBegunAndNoOccurrenceForCurrentPeriod_SetsActiveAndGeneratesPendingOccurrence()
    {
        var expense = CreateExpense(
            status: RecurringExpenseStatusType.Paused,
            monthlyAmount: 1500m,
            startDate: new DateOnly(2026, 1, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 8));

        expense.Reactivate(new ReferencePeriod(2026, 8));

        Assert.Equal(RecurringExpenseStatusType.Active, expense.GetStatus().GetValue());
        var occurrence = Assert.Single(expense.GetOccurrences());
        Assert.Equal(OccurrenceStatusType.Pending, occurrence.GetStatus().GetValue());
        Assert.Equal(1500m, occurrence.GetExpectedAmount().GetValue());
    }

    [Fact]
    public void Reactivate_OccurrenceAlreadyExistsForCurrentPeriod_SetsActiveAndGeneratesNoAdditionalOccurrence()
    {
        var expense = CreateExpense(
            status: RecurringExpenseStatusType.Active,
            startDate: new DateOnly(2026, 1, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 8));
        expense.Pause();

        expense.Reactivate(new ReferencePeriod(2026, 8));

        Assert.Equal(RecurringExpenseStatusType.Active, expense.GetStatus().GetValue());
        Assert.Single(expense.GetOccurrences());
    }

    [Fact]
    public void Reactivate_StartDateInFuture_SetsActiveButGeneratesNoOccurrence()
    {
        var expense = CreateExpense(
            status: RecurringExpenseStatusType.Paused,
            startDate: new DateOnly(2026, 9, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 8));

        expense.Reactivate(new ReferencePeriod(2026, 8));

        Assert.Equal(RecurringExpenseStatusType.Active, expense.GetStatus().GetValue());
        Assert.Empty(expense.GetOccurrences());
    }
}
