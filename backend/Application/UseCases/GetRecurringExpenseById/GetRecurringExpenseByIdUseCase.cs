using ContasEmDia.Domain.Repositories;

namespace ContasEmDia.Application.UseCases.GetRecurringExpenseById;

public sealed class GetRecurringExpenseByIdUseCase : IGetRecurringExpenseByIdUseCase
{
    private readonly IRepositoryManager _repositoryManager;

    public GetRecurringExpenseByIdUseCase(IRepositoryManager repositoryManager)
    {
        _repositoryManager = repositoryManager;
    }

    public async Task<GetRecurringExpenseByIdUseCaseOutput> ExecuteAsync(GetRecurringExpenseByIdUseCaseInput input)
    {
        var recurringExpense = await _repositoryManager.RecurringExpenseRepository.GetByIdAsync(input.Id)
            ?? throw new KeyNotFoundException("Despesa recorrente não encontrada.");

        var data = new RecurringExpenseData(
            recurringExpense.GetId(),
            recurringExpense.GetName().GetValue(),
            recurringExpense.GetCategory().GetValue().ToString(),
            recurringExpense.GetMonthlyAmount().GetValue(),
            recurringExpense.GetDueDay().GetValue(),
            recurringExpense.GetStartDate().GetValue(),
            recurringExpense.GetFrequency().GetValue().ToString(),
            recurringExpense.GetStatus().GetValue().ToString(),
            recurringExpense.GetNote().GetValue());

        return new GetRecurringExpenseByIdUseCaseOutput(data);
    }
}
