namespace ContasEmDia.Application.UseCases.UpdateRecurringExpense;

public sealed class UpdateRecurringExpenseUseCaseInput
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Category { get; init; }

    public required decimal MonthlyAmount { get; init; }

    public required int DueDay { get; init; }

    public required string StartDate { get; init; }

    public required string Status { get; init; }

    public string? Note { get; init; }
}
