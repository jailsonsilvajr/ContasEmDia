namespace ContasEmDia.Domain.ValueObjects;

public sealed class ExpenseName
{
    private readonly string _value;

    public ExpenseName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("O nome da despesa não pode ser nulo, vazio ou conter apenas espaços em branco.", nameof(value));
        }

        _value = value;
    }

    public string GetValue() => _value;
}
