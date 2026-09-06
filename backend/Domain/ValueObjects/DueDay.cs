namespace ContasEmDia.Domain.ValueObjects;

public sealed class DueDay
{
    private readonly int _value;

    public DueDay(int value)
    {
        if (value is < 1 or > 31)
        {
            throw new ArgumentException("O dia de vencimento deve estar entre 1 e 31.", nameof(value));
        }

        _value = value;
    }

    public int GetValue() => _value;
}
