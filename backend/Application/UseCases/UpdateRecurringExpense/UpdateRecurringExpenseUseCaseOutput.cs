using ContasEmDia.Application.UseCases.CreateRecurringExpense;
using ContasEmDia.Application.UseCases.GetRecurringExpenseById;

namespace ContasEmDia.Application.UseCases.UpdateRecurringExpense;

public sealed class UpdateRecurringExpenseUseCaseOutput
{
    private UpdateRecurringExpenseUseCaseOutput(
        bool isSuccess,
        RecurringExpenseData? recurringExpense,
        IReadOnlyCollection<FieldError> errors)
    {
        IsSuccess = isSuccess;
        RecurringExpense = recurringExpense;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public RecurringExpenseData? RecurringExpense { get; }

    public IReadOnlyCollection<FieldError> Errors { get; }

    public static UpdateRecurringExpenseUseCaseOutput Success(RecurringExpenseData recurringExpense) =>
        new(isSuccess: true, recurringExpense: recurringExpense, errors: []);

    public static UpdateRecurringExpenseUseCaseOutput Failure(IReadOnlyCollection<FieldError> errors) =>
        new(isSuccess: false, recurringExpense: null, errors: errors);
}
