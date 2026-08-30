namespace ContasEmDia.Domain.ValueObjects;

public sealed class ExpenseName
{
    private readonly string _value;

    public ExpenseName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Expense name must not be null, empty, or whitespace-only.", nameof(value));
        }

        _value = value;
    }

    public string GetValue() => _value;
}
