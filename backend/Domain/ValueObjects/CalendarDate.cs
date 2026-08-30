namespace ContasEmDia.Domain.ValueObjects;

public sealed class CalendarDate
{
    private readonly DateOnly _value;

    public CalendarDate(DateOnly value)
    {
        _value = value;
    }

    public DateOnly GetValue() => _value;
}
