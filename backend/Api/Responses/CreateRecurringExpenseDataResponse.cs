namespace ContasEmDia.Api.Responses;

public sealed record CreateRecurringExpenseDataResponse(
    Guid Id,
    string Name,
    string Category,
    decimal MonthlyAmount,
    int DueDay,
    DateOnly StartDate,
    string Frequency,
    string Status,
    string? Note,
    IReadOnlyCollection<OccurrenceDataResponse> Occurrences);
