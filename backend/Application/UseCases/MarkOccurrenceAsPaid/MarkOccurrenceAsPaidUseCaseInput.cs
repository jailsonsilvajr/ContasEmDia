namespace ContasEmDia.Application.UseCases.MarkOccurrenceAsPaid;

public sealed class MarkOccurrenceAsPaidUseCaseInput
{
    public required Guid OccurrenceId { get; init; }

    public string? PaidAmountRaw { get; init; }

    public string? PaymentDateRaw { get; init; }
}
