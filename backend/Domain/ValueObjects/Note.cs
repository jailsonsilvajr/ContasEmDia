namespace ContasEmDia.Domain.ValueObjects;

public sealed class Note
{
    private readonly string? _value;

    public Note(string? value)
    {
        _value = value;
    }

    public string? GetValue() => _value;
}
