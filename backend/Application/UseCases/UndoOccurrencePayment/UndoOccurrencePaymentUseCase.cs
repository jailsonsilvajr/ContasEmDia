using ContasEmDia.Application.Ports;
using ContasEmDia.Application.UseCases.GetMonthlyPanel;
using ContasEmDia.Domain.Repositories;

namespace ContasEmDia.Application.UseCases.UndoOccurrencePayment;

public sealed class UndoOccurrencePaymentUseCase : IUndoOccurrencePaymentUseCase
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly ICurrentDateProvider _currentDateProvider;

    public UndoOccurrencePaymentUseCase(IRepositoryManager repositoryManager, ICurrentDateProvider currentDateProvider)
    {
        _repositoryManager = repositoryManager;
        _currentDateProvider = currentDateProvider;
    }

    public async Task<UndoOccurrencePaymentUseCaseOutput> ExecuteAsync(UndoOccurrencePaymentUseCaseInput input)
    {
        var recurringExpense = await _repositoryManager.RecurringExpenseRepository.GetByOccurrenceIdAsync(input.OccurrenceId)
            ?? throw new KeyNotFoundException("Ocorrência não encontrada.");

        var occurrence = recurringExpense.FindOccurrence(input.OccurrenceId)!;

        recurringExpense.UndoOccurrencePayment(input.OccurrenceId);

        await _repositoryManager.RecurringExpenseRepository.UpdateAsync(recurringExpense);

        var today = _currentDateProvider.GetCurrentDate();

        var updatedOccurrence = new PanelOccurrenceData(
            occurrence.GetId(),
            occurrence.GetName().GetValue(),
            occurrence.GetCategory().GetValue().ToString(),
            occurrence.GetExpectedAmount().GetValue(),
            occurrence.GetDueDate().GetValue(),
            occurrence.GetDerivedStatus(today).GetValue().ToString(),
            occurrence.GetPaidAmount()?.GetValue(),
            occurrence.GetPaymentDate()?.GetValue());

        return new UndoOccurrencePaymentUseCaseOutput(updatedOccurrence);
    }
}
