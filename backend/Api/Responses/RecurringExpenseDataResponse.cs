namespace ContasEmDia.Api.Responses;

public sealed record RecurringExpenseDataResponse(
    Guid Id,
    string Name,
    string Category,
    decimal MonthlyAmount,
    int DueDay,
    DateOnly StartDate,
    string Frequency,
    string Status,
    string? Note);
