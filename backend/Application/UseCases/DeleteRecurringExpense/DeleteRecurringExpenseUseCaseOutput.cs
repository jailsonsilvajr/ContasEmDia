using ContasEmDia.Application.UseCases.CreateRecurringExpense;

namespace ContasEmDia.Application.UseCases.DeleteRecurringExpense;

public sealed record DeleteRecurringExpenseData(Guid RecurringExpenseId, string Scope);

public sealed class DeleteRecurringExpenseUseCaseOutput
{
    private DeleteRecurringExpenseUseCaseOutput(
        bool isSuccess,
        DeleteRecurringExpenseData? data,
        IReadOnlyCollection<FieldError> errors)
    {
        IsSuccess = isSuccess;
        Data = data;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public DeleteRecurringExpenseData? Data { get; }

    public IReadOnlyCollection<FieldError> Errors { get; }

    public static DeleteRecurringExpenseUseCaseOutput Success(DeleteRecurringExpenseData data) =>
        new(isSuccess: true, data: data, errors: []);

    public static DeleteRecurringExpenseUseCaseOutput Failure(IReadOnlyCollection<FieldError> errors) =>
        new(isSuccess: false, data: null, errors: errors);
}
