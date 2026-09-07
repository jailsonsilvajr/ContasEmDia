using ContasEmDia.Application.UseCases.GetMonthlyPanel;

namespace ContasEmDia.Application.UseCases.UndoOccurrencePayment;

public sealed class UndoOccurrencePaymentUseCaseOutput
{
    public UndoOccurrencePaymentUseCaseOutput(PanelOccurrenceData occurrence)
    {
        Occurrence = occurrence;
    }

    public PanelOccurrenceData Occurrence { get; }
}
