using ContasEmDia.Application.UseCases.CreateRecurringExpense;

namespace ContasEmDia.Application.UseCases.GetMonthlyPanel;

public sealed record PanelOccurrenceData(
    Guid Id,
    string Name,
    string Category,
    decimal ExpectedAmount,
    DateOnly DueDate,
    string DerivedStatus,
    decimal? PaidAmount,
    DateOnly? PaymentDate);

public sealed class GetMonthlyPanelUseCaseOutput
{
    private GetMonthlyPanelUseCaseOutput(
        bool isSuccess,
        int? year,
        int? month,
        IReadOnlyCollection<PanelOccurrenceData>? occurrences,
        IReadOnlyCollection<FieldError> errors)
    {
        IsSuccess = isSuccess;
        Year = year;
        Month = month;
        Occurrences = occurrences;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public int? Year { get; }

    public int? Month { get; }

    public IReadOnlyCollection<PanelOccurrenceData>? Occurrences { get; }

    public IReadOnlyCollection<FieldError> Errors { get; }

    public static GetMonthlyPanelUseCaseOutput Success(int year, int month, IReadOnlyCollection<PanelOccurrenceData> occurrences) =>
        new(isSuccess: true, year: year, month: month, occurrences: occurrences, errors: []);

    public static GetMonthlyPanelUseCaseOutput Failure(IReadOnlyCollection<FieldError> errors) =>
        new(isSuccess: false, year: null, month: null, occurrences: null, errors: errors);
}
