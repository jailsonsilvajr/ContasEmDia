using System.Reflection;
using ContasEmDia.Domain;
using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.Entities;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Tests.Entities;

public class OccurrenceTests
{
    // Occurrence exposes no public way to reach the Paid state at this phase
    // (MarkAsPaid is introduced later, alongside User Story 2). Reflection sets
    // the private backing field directly, purely to exercise GetDerivedStatus's
    // "already Paid" priority branch in isolation.
    private static void ForcePaidStatusViaReflection(Occurrence occurrence)
    {
        var statusField = typeof(Occurrence).GetField("_status", BindingFlags.NonPublic | BindingFlags.Instance)!;
        statusField.SetValue(occurrence, new OccurrenceStatus(OccurrenceStatusType.Paid));
    }

    private static Occurrence CreateOccurrence(DateOnly dueDate)
    {
        var startDate = new DateOnly(dueDate.Year, dueDate.Month, 1);
        var currentReferencePeriod = new ReferencePeriod(dueDate.Year, dueDate.Month);

        var expense = new RecurringExpense(
            new ExpenseName("Aluguel"),
            new ExpenseCategory(ExpenseCategoryType.Housing),
            new Money(1500m),
            new DueDay(dueDate.Day),
            new CalendarDate(startDate),
            new Frequency(FrequencyType.Monthly),
            new RecurringExpenseStatus(RecurringExpenseStatusType.Active),
            new Note(null),
            currentReferencePeriod);

        return expense.GetOccurrences().Single();
    }

    [Fact]
    public void GetDerivedStatus_PaidOccurrence_IsAlwaysPaidRegardlessOfDate()
    {
        // Due date is far in the past (would otherwise be Overdue) to prove Paid
        // takes priority regardless of the due date/reference date comparison.
        var occurrence = CreateOccurrence(new DateOnly(2020, 1, 1));
        ForcePaidStatusViaReflection(occurrence);

        var status = occurrence.GetDerivedStatus(new DateOnly(2026, 9, 7));

        Assert.Equal(OccurrenceDerivedStatusType.Paid, status.GetValue());
    }

    [Fact]
    public void GetDerivedStatus_DueDateInThePast_IsOverdue()
    {
        var referenceDate = new DateOnly(2026, 9, 7);
        var occurrence = CreateOccurrence(referenceDate.AddDays(-1));

        var status = occurrence.GetDerivedStatus(referenceDate);

        Assert.Equal(OccurrenceDerivedStatusType.Overdue, status.GetValue());
    }

    [Fact]
    public void GetDerivedStatus_DueToday_IsDueSoon()
    {
        var referenceDate = new DateOnly(2026, 9, 7);
        var occurrence = CreateOccurrence(referenceDate);

        var status = occurrence.GetDerivedStatus(referenceDate);

        Assert.Equal(OccurrenceDerivedStatusType.DueSoon, status.GetValue());
    }

    [Fact]
    public void GetDerivedStatus_DueInExactlySevenDays_IsDueSoon()
    {
        var referenceDate = new DateOnly(2026, 9, 7);
        var occurrence = CreateOccurrence(referenceDate.AddDays(7));

        var status = occurrence.GetDerivedStatus(referenceDate);

        Assert.Equal(OccurrenceDerivedStatusType.DueSoon, status.GetValue());
    }

    [Fact]
    public void GetDerivedStatus_DueInEightDays_IsPending()
    {
        var referenceDate = new DateOnly(2026, 9, 7);
        var occurrence = CreateOccurrence(referenceDate.AddDays(8));

        var status = occurrence.GetDerivedStatus(referenceDate);

        Assert.Equal(OccurrenceDerivedStatusType.Pending, status.GetValue());
    }

    [Fact]
    public void GetDerivedStatus_DueDateCrossesMonthBoundary_UsesFullDateNotIsolatedDay()
    {
        // Reference date is 2026-08-29; due date is 2026-09-03 (day-of-month 3, smaller
        // than reference day-of-month 29). A buggy isolated-day comparison would treat
        // this as far in the future or overdue; the correct full-date diff is 5 days → DueSoon.
        var referenceDate = new DateOnly(2026, 8, 29);
        var occurrence = CreateOccurrence(new DateOnly(2026, 9, 3));

        var status = occurrence.GetDerivedStatus(referenceDate);

        Assert.Equal(OccurrenceDerivedStatusType.DueSoon, status.GetValue());
    }

    [Fact]
    public void GetPaidAmountAndGetPaymentDate_WhilePending_AreNull()
    {
        var occurrence = CreateOccurrence(new DateOnly(2026, 9, 7));

        Assert.Null(occurrence.GetPaidAmount());
        Assert.Null(occurrence.GetPaymentDate());
    }

    [Fact]
    public void MarkAsPaid_PendingOccurrence_UpdatesStatusPaidAmountAndPaymentDate()
    {
        var occurrence = CreateOccurrence(new DateOnly(2026, 9, 7));
        var paidAmount = new Money(1500m);
        var paymentDate = new CalendarDate(new DateOnly(2026, 9, 5));

        occurrence.MarkAsPaid(paidAmount, paymentDate);

        Assert.Equal(OccurrenceDerivedStatusType.Paid, occurrence.GetDerivedStatus(new DateOnly(2026, 9, 7)).GetValue());
        Assert.Equal(paidAmount.GetValue(), occurrence.GetPaidAmount()!.GetValue());
        Assert.Equal(paymentDate.GetValue(), occurrence.GetPaymentDate()!.GetValue());
    }

    [Fact]
    public void MarkAsPaid_AlreadyPaidOccurrence_ThrowsDomainRuleViolationException()
    {
        var occurrence = CreateOccurrence(new DateOnly(2026, 9, 7));
        occurrence.MarkAsPaid(new Money(1500m), new CalendarDate(new DateOnly(2026, 9, 5)));

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => occurrence.MarkAsPaid(new Money(1500m), new CalendarDate(new DateOnly(2026, 9, 6))));

        Assert.Equal("Esta ocorrência já está paga.", exception.Message);
    }

    [Fact]
    public void UndoPayment_PaidOccurrence_RevertsStatusPaidAmountAndPaymentDate()
    {
        var occurrence = CreateOccurrence(new DateOnly(2026, 9, 7));
        occurrence.MarkAsPaid(new Money(1500m), new CalendarDate(new DateOnly(2026, 9, 5)));

        occurrence.UndoPayment();

        Assert.Equal(OccurrenceDerivedStatusType.DueSoon, occurrence.GetDerivedStatus(new DateOnly(2026, 9, 7)).GetValue());
        Assert.Null(occurrence.GetPaidAmount());
        Assert.Null(occurrence.GetPaymentDate());
    }

    [Fact]
    public void UndoPayment_NotYetPaidOccurrence_ThrowsDomainRuleViolationException()
    {
        var occurrence = CreateOccurrence(new DateOnly(2026, 9, 7));

        var exception = Assert.Throws<DomainRuleViolationException>(() => occurrence.UndoPayment());

        Assert.Equal("Esta ocorrência ainda não foi paga.", exception.Message);
    }
}
