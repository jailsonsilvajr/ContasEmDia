namespace ContasEmDia.Domain.ValueObjects;

public enum RecurringExpenseStatusType
{
    Active,
    Paused
}

public sealed class RecurringExpenseStatus
{
    private readonly RecurringExpenseStatusType _value;

    public RecurringExpenseStatus(RecurringExpenseStatusType value)
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentException("O status da despesa recorrente deve ser um valor de status válido.", nameof(value));
        }

        _value = value;
    }

    public RecurringExpenseStatusType GetValue() => _value;
}
