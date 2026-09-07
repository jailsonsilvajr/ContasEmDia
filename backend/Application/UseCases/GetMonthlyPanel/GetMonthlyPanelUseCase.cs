using ContasEmDia.Application.Ports;
using ContasEmDia.Application.UseCases.CreateRecurringExpense;
using ContasEmDia.Domain.Repositories;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Application.UseCases.GetMonthlyPanel;

public sealed class GetMonthlyPanelUseCase : IGetMonthlyPanelUseCase
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly ICurrentDateProvider _currentDateProvider;

    public GetMonthlyPanelUseCase(IRepositoryManager repositoryManager, ICurrentDateProvider currentDateProvider)
    {
        _repositoryManager = repositoryManager;
        _currentDateProvider = currentDateProvider;
    }

    public async Task<GetMonthlyPanelUseCaseOutput> ExecuteAsync(GetMonthlyPanelUseCaseInput input)
    {
        var today = _currentDateProvider.GetCurrentDate();

        var yearProvided = !string.IsNullOrWhiteSpace(input.Year);
        var monthProvided = !string.IsNullOrWhiteSpace(input.Month);

        ReferencePeriod referencePeriod;

        if (!yearProvided && !monthProvided)
        {
            referencePeriod = ReferencePeriod.FromDate(today);
        }
        else if (yearProvided != monthProvided)
        {
            return GetMonthlyPanelUseCaseOutput.Failure([new FieldError("period", "Informe mês e ano juntos, ou nenhum dos dois.")]);
        }
        else if (!int.TryParse(input.Year, out var year) || !int.TryParse(input.Month, out var month))
        {
            return GetMonthlyPanelUseCaseOutput.Failure([new FieldError("period", "Mês e ano devem ser números válidos.")]);
        }
        else
        {
            try
            {
                referencePeriod = new ReferencePeriod(year, month);
            }
            catch (ArgumentException ex)
            {
                return GetMonthlyPanelUseCaseOutput.Failure([new FieldError("period", ex.Message)]);
            }
        }

        var recurringExpenses = await _repositoryManager.RecurringExpenseRepository.GetByReferencePeriodAsync(referencePeriod);

        var occurrences = recurringExpenses
            .SelectMany(expense => expense.GetOccurrencesForPeriod(referencePeriod))
            .Select(occurrence => new PanelOccurrenceData(
                occurrence.GetId(),
                occurrence.GetName().GetValue(),
                occurrence.GetCategory().GetValue().ToString(),
                occurrence.GetExpectedAmount().GetValue(),
                occurrence.GetDueDate().GetValue(),
                occurrence.GetDerivedStatus(today).GetValue().ToString(),
                occurrence.GetPaidAmount()?.GetValue(),
                occurrence.GetPaymentDate()?.GetValue()))
            .ToList();

        return GetMonthlyPanelUseCaseOutput.Success(referencePeriod.Year, referencePeriod.Month, occurrences);
    }
}
