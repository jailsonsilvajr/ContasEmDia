namespace ContasEmDia.Api.Responses;

public sealed record GetMonthlyPanelDataResponse(
    ReferencePeriodDataResponse ReferencePeriod,
    IReadOnlyCollection<PanelOccurrenceDataResponse> Occurrences);
