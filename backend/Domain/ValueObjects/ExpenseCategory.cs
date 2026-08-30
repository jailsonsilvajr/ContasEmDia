namespace ContasEmDia.Domain.ValueObjects;

public enum ExpenseCategoryType
{
    Housing,
    Services,
    Transportation,
    Subscriptions,
    Other
}

public sealed class ExpenseCategory
{
    private readonly ExpenseCategoryType _value;

    public ExpenseCategory(ExpenseCategoryType value)
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentException("Expense category must be a defined category value.", nameof(value));
        }

        _value = value;
    }

    public ExpenseCategoryType GetValue() => _value;
}
