using ContasEmDia.Application.Ports;

namespace ContasEmDia.Application.Tests.UseCases.DeleteRecurringExpense;

internal sealed class FixedCurrentDateProvider : ICurrentDateProvider
{
    private readonly DateOnly _currentDate;

    public FixedCurrentDateProvider(DateOnly currentDate)
    {
        _currentDate = currentDate;
    }

    public DateOnly GetCurrentDate() => _currentDate;
}
