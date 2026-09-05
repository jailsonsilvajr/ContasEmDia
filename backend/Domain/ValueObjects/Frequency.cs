namespace ContasEmDia.Domain.ValueObjects;

public enum FrequencyType
{
    Monthly
}

public sealed class Frequency
{
    private readonly FrequencyType _value;

    public Frequency(FrequencyType value)
    {
        if (value != FrequencyType.Monthly)
        {
            throw new ArgumentException("Apenas a frequência mensal é suportada nesta fase.", nameof(value));
        }

        _value = value;
    }

    public FrequencyType GetValue() => _value;
}
