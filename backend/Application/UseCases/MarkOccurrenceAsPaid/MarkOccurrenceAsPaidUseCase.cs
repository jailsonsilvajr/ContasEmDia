using System.Globalization;
using ContasEmDia.Application.Ports;
using ContasEmDia.Application.UseCases.GetMonthlyPanel;
using ContasEmDia.Domain.Repositories;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Application.UseCases.MarkOccurrenceAsPaid;

public sealed class MarkOccurrenceAsPaidUseCase : IMarkOccurrenceAsPaidUseCase
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly ICurrentDateProvider _currentDateProvider;

    public MarkOccurrenceAsPaidUseCase(IRepositoryManager repositoryManager, ICurrentDateProvider currentDateProvider)
    {
        _repositoryManager = repositoryManager;
        _currentDateProvider = currentDateProvider;
    }

    public async Task<MarkOccurrenceAsPaidUseCaseOutput> ExecuteAsync(MarkOccurrenceAsPaidUseCaseInput input)
    {
        var recurringExpense = await _repositoryManager.RecurringExpenseRepository.GetByOccurrenceIdAsync(input.OccurrenceId)
            ?? throw new KeyNotFoundException("Ocorrência não encontrada.");

        var occurrence = recurringExpense.FindOccurrence(input.OccurrenceId)!;

        var paidAmount = TryParseAmount(input.PaidAmountRaw, out var parsedAmount)
            ? new Money(parsedAmount)
            : occurrence.GetExpectedAmount();

        var paymentDate = TryParseDate(input.PaymentDateRaw, out var parsedDate)
            ? new CalendarDate(parsedDate)
            : new CalendarDate(_currentDateProvider.GetCurrentDate());

        recurringExpense.MarkOccurrenceAsPaid(input.OccurrenceId, paidAmount, paymentDate);

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

        return new MarkOccurrenceAsPaidUseCaseOutput(updatedOccurrence);
    }

    private static bool TryParseAmount(string? raw, out decimal amount)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            amount = default;
            return false;
        }

        var trimmed = raw.Trim();
        var normalized = trimmed.Contains(',')
            ? trimmed.Replace(".", string.Empty).Replace(',', '.')
            : trimmed;

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount) && amount > 0;
    }

    private static bool TryParseDate(string? raw, out DateOnly date) =>
        DateOnly.TryParseExact(raw, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
