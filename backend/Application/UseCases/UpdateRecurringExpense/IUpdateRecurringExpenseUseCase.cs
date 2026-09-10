namespace ContasEmDia.Application.UseCases.UpdateRecurringExpense;

public interface IUpdateRecurringExpenseUseCase
{
    Task<UpdateRecurringExpenseUseCaseOutput> ExecuteAsync(UpdateRecurringExpenseUseCaseInput input);
}
