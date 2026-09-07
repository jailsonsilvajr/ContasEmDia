namespace ContasEmDia.Domain.ValueObjects;

public enum OccurrenceDerivedStatusType
{
    Paid,
    Overdue,
    DueSoon,
    Pending
}

public sealed class OccurrenceDerivedStatus
{
    private readonly OccurrenceDerivedStatusType _value;

    public OccurrenceDerivedStatus(OccurrenceDerivedStatusType value)
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentException("O status derivado da ocorrência deve ser um valor de status válido.", nameof(value));
        }

        _value = value;
    }

    public OccurrenceDerivedStatusType GetValue() => _value;
}
