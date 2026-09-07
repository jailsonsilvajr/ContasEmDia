using ContasEmDia.Api.Responses;
using ContasEmDia.Application.UseCases.GetMonthlyPanel;

namespace ContasEmDia.Api.Mappings;

public static class GetMonthlyPanelDataResponseMapping
{
    public static GetMonthlyPanelDataResponse ToDataResponse(this GetMonthlyPanelUseCaseOutput output) =>
        new(
            ReferencePeriod: new ReferencePeriodDataResponse(output.Year!.Value, output.Month!.Value),
            Occurrences: output.Occurrences!.Select(ToPanelOccurrenceDataResponse).ToList());

    public static PanelOccurrenceDataResponse ToPanelOccurrenceDataResponse(this PanelOccurrenceData occurrence) =>
        new(
            Id: occurrence.Id,
            Name: occurrence.Name,
            Category: occurrence.Category,
            ExpectedAmount: occurrence.ExpectedAmount,
            DueDate: occurrence.DueDate,
            Status: occurrence.DerivedStatus,
            PaidAmount: occurrence.PaidAmount,
            PaymentDate: occurrence.PaymentDate);
}
