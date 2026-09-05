namespace ContasEmDia.Domain.ValueObjects;

public enum OccurrenceStatusType
{
    Pending,
    Paid
}

public sealed class OccurrenceStatus
{
    private readonly OccurrenceStatusType _value;

    public OccurrenceStatus(OccurrenceStatusType value)
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentException("O status da ocorrência deve ser um valor de status válido.", nameof(value));
        }

        _value = value;
    }

    public OccurrenceStatusType GetValue() => _value;
}
