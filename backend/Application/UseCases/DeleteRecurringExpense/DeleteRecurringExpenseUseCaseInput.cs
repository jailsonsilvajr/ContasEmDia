namespace ContasEmDia.Application.UseCases.DeleteRecurringExpense;

public sealed class DeleteRecurringExpenseUseCaseInput
{
    public required Guid Id { get; init; }

    public string? Scope { get; init; }
}
