namespace ContasEmDia.Domain.ValueObjects;

public sealed class DueDay
{
    private readonly int _value;

    public DueDay(int value)
    {
        if (value is < 1 or > 31)
        {
            throw new ArgumentException("Due day must be between 1 and 31.", nameof(value));
        }

        _value = value;
    }

    public int GetValue() => _value;
}
