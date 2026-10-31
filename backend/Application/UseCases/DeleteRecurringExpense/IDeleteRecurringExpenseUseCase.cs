namespace ContasEmDia.Application.UseCases.DeleteRecurringExpense;

public interface IDeleteRecurringExpenseUseCase
{
    Task<DeleteRecurringExpenseUseCaseOutput> ExecuteAsync(DeleteRecurringExpenseUseCaseInput input);
}
