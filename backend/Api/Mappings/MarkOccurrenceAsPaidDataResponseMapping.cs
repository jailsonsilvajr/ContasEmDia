using ContasEmDia.Api.Responses;
using ContasEmDia.Application.UseCases.MarkOccurrenceAsPaid;

namespace ContasEmDia.Api.Mappings;

public static class MarkOccurrenceAsPaidDataResponseMapping
{
    public static MarkOccurrenceAsPaidDataResponse ToDataResponse(this MarkOccurrenceAsPaidUseCaseOutput output) =>
        new(output.Occurrence.ToPanelOccurrenceDataResponse());
}
