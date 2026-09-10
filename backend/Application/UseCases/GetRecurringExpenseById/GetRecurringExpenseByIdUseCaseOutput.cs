namespace ContasEmDia.Application.UseCases.GetRecurringExpenseById;

public sealed record RecurringExpenseData(
    Guid Id,
    string Name,
    string Category,
    decimal MonthlyAmount,
    int DueDay,
    DateOnly StartDate,
    string Frequency,
    string Status,
    string? Note);

public sealed class GetRecurringExpenseByIdUseCaseOutput
{
    public GetRecurringExpenseByIdUseCaseOutput(RecurringExpenseData recurringExpense)
    {
        RecurringExpense = recurringExpense;
    }

    public RecurringExpenseData RecurringExpense { get; }
}
