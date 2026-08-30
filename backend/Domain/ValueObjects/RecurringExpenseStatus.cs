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
            throw new ArgumentException("Recurring expense status must be a defined status value.", nameof(value));
        }

        _value = value;
    }

    public RecurringExpenseStatusType GetValue() => _value;
}
