namespace ContasEmDia.Application.UseCases.UndoOccurrencePayment;

public sealed class UndoOccurrencePaymentUseCaseInput
{
    public required Guid OccurrenceId { get; init; }
}
