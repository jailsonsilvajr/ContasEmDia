namespace ContasEmDia.Application.UseCases.GetRecurringExpenseById;

public interface IGetRecurringExpenseByIdUseCase
{
    Task<GetRecurringExpenseByIdUseCaseOutput> ExecuteAsync(GetRecurringExpenseByIdUseCaseInput input);
}
