namespace ContasEmDia.Application.UseCases.UndoOccurrencePayment;

public interface IUndoOccurrencePaymentUseCase
{
    Task<UndoOccurrencePaymentUseCaseOutput> ExecuteAsync(UndoOccurrencePaymentUseCaseInput input);
}
