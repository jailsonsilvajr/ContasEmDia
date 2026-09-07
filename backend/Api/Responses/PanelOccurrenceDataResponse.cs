namespace ContasEmDia.Api.Responses;

public sealed record PanelOccurrenceDataResponse(
    Guid Id,
    string Name,
    string Category,
    decimal ExpectedAmount,
    DateOnly DueDate,
    string Status,
    decimal? PaidAmount,
    DateOnly? PaymentDate);
