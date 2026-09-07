using ContasEmDia.Api.Requests;
using ContasEmDia.Application.UseCases.MarkOccurrenceAsPaid;

namespace ContasEmDia.Api.Mappings;

public static class MarkOccurrenceAsPaidDataRequestMapping
{
    public static MarkOccurrenceAsPaidUseCaseInput ToUseCaseInput(this MarkOccurrenceAsPaidDataRequest request, Guid occurrenceId) =>
        new()
        {
            OccurrenceId = occurrenceId,
            PaidAmountRaw = request.PaidAmount,
            PaymentDateRaw = request.PaymentDate
        };
}
