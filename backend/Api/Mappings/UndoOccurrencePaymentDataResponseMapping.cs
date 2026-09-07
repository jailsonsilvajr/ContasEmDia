using ContasEmDia.Api.Responses;
using ContasEmDia.Application.UseCases.UndoOccurrencePayment;

namespace ContasEmDia.Api.Mappings;

public static class UndoOccurrencePaymentDataResponseMapping
{
    public static UndoOccurrencePaymentDataResponse ToDataResponse(this UndoOccurrencePaymentUseCaseOutput output) =>
        new(output.Occurrence.ToPanelOccurrenceDataResponse());
}
