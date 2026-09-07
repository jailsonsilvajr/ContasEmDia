namespace ContasEmDia.Api.Requests;

public sealed record MarkOccurrenceAsPaidDataRequest
{
    public string? PaidAmount { get; init; }

    public string? PaymentDate { get; init; }
}
