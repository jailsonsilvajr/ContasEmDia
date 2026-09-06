namespace ContasEmDia.Application.UseCases.CreateRecurringExpense;

public interface ICreateRecurringExpenseUseCase
{
    Task<CreateRecurringExpenseUseCaseOutput> ExecuteAsync(CreateRecurringExpenseUseCaseInput input);
}
