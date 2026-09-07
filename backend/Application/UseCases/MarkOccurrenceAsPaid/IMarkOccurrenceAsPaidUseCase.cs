namespace ContasEmDia.Application.UseCases.MarkOccurrenceAsPaid;

public interface IMarkOccurrenceAsPaidUseCase
{
    Task<MarkOccurrenceAsPaidUseCaseOutput> ExecuteAsync(MarkOccurrenceAsPaidUseCaseInput input);
}
