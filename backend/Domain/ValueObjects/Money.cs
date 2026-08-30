namespace ContasEmDia.Domain.ValueObjects;

public sealed class Money
{
    private readonly decimal _value;

    public Money(decimal value)
    {
        if (value <= 0)
        {
            throw new ArgumentException("Money value must be greater than zero.", nameof(value));
        }

        if (decimal.Round(value, 2) != value)
        {
            throw new ArgumentException("Money value must have at most two decimal places.", nameof(value));
        }

        _value = value;
    }

    public decimal GetValue() => _value;
}
