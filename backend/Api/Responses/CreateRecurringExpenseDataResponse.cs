namespace ContasEmDia.Api.Responses;

public sealed record CreateRecurringExpenseDataResponse(
    Guid Id,
    string Name,
    string Category,
    decimal MonthlyAmount,
    int DueDay,
    DateOnly StartDate,
    DateOnly EndDate,
    string Frequency,
    string Status,
    string? Note,
    IReadOnlyCollection<OccurrenceDataResponse> Occurrences);
