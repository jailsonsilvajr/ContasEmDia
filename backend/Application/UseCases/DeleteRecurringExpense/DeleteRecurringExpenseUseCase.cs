using ContasEmDia.Application.Ports;
using ContasEmDia.Application.UseCases.CreateRecurringExpense;
using ContasEmDia.Domain.Repositories;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Application.UseCases.DeleteRecurringExpense;

public sealed class DeleteRecurringExpenseUseCase : IDeleteRecurringExpenseUseCase
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly ICurrentDateProvider _currentDateProvider;

    public DeleteRecurringExpenseUseCase(IRepositoryManager repositoryManager, ICurrentDateProvider currentDateProvider)
    {
        _repositoryManager = repositoryManager;
        _currentDateProvider = currentDateProvider;
    }

    public async Task<DeleteRecurringExpenseUseCaseOutput> ExecuteAsync(DeleteRecurringExpenseUseCaseInput input)
    {
        if (input.Scope != "pending-only" && input.Scope != "all")
        {
            return DeleteRecurringExpenseUseCaseOutput.Failure(
                [new FieldError("scope", "Informe a abrangência da exclusão: \"pending-only\" ou \"all\".")]);
        }

        var recurringExpense = await _repositoryManager.RecurringExpenseRepository.GetByIdAsync(input.Id)
            ?? throw new KeyNotFoundException("Despesa recorrente não encontrada.");

        if (input.Scope == "all")
        {
            await _repositoryManager.RecurringExpenseRepository.RemoveAsync(recurringExpense);
        }
        else
        {
            var currentDate = new CalendarDate(_currentDateProvider.GetCurrentDate());
            recurringExpense.DeleteKeepingPaidHistory(currentDate);
            await _repositoryManager.RecurringExpenseRepository.UpdateAsync(recurringExpense);
        }

        return DeleteRecurringExpenseUseCaseOutput.Success(new DeleteRecurringExpenseData(recurringExpense.GetId(), input.Scope));
    }
}
