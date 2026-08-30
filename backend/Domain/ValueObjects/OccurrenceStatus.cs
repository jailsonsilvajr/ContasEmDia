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
            throw new ArgumentException("Occurrence status must be a defined status value.", nameof(value));
        }

        _value = value;
    }

    public OccurrenceStatusType GetValue() => _value;
}
