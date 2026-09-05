namespace ContasEmDia.Domain.ValueObjects;

public sealed class Money
{
    private readonly decimal _value;

    public Money(decimal value)
    {
        if (value <= 0)
        {
            throw new ArgumentException("O valor monetário deve ser maior que zero.", nameof(value));
        }

        if (decimal.Round(value, 2) != value)
        {
            throw new ArgumentException("O valor monetário deve ter no máximo duas casas decimais.", nameof(value));
        }

        _value = value;
    }

    public decimal GetValue() => _value;
}
