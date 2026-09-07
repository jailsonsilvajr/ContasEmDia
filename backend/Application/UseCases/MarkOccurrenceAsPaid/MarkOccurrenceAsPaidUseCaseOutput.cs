using ContasEmDia.Application.UseCases.GetMonthlyPanel;

namespace ContasEmDia.Application.UseCases.MarkOccurrenceAsPaid;

public sealed class MarkOccurrenceAsPaidUseCaseOutput
{
    public MarkOccurrenceAsPaidUseCaseOutput(PanelOccurrenceData occurrence)
    {
        Occurrence = occurrence;
    }

    public PanelOccurrenceData Occurrence { get; }
}
