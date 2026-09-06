namespace ContasEmDia.Api.Responses;

public sealed record OccurrenceDataResponse(
    Guid Id,
    ReferencePeriodDataResponse ReferencePeriod,
    DateOnly DueDate,
    string Status,
    string Name,
    string Category,
    decimal ExpectedAmount);
