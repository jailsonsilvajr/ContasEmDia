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
            throw new ArgumentException("A categoria da despesa deve ser um valor de categoria válido.", nameof(value));
        }

        _value = value;
    }

    public ExpenseCategoryType GetValue() => _value;
}
