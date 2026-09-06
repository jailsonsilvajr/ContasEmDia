namespace ContasEmDia.Domain.ValueObjects;

public sealed class ReferencePeriod : IComparable<ReferencePeriod>
{
    public ReferencePeriod(int year, int month)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentException("O mês deve estar entre 1 e 12.", nameof(month));
        }

        if (year <= 0)
        {
            throw new ArgumentException("O ano deve ser maior que zero.", nameof(year));
        }

        Year = year;
        Month = month;
    }

    public int Year { get; }

    public int Month { get; }

    public static ReferencePeriod FromDate(DateOnly date) => new(date.Year, date.Month);

    public int CompareTo(ReferencePeriod? other)
    {
        if (other is null)
        {
            return 1;
        }

        var thisOrdinal = Year * 12 + Month;
        var otherOrdinal = other.Year * 12 + other.Month;
        return thisOrdinal.CompareTo(otherOrdinal);
    }

    public static bool operator >=(ReferencePeriod left, ReferencePeriod right) => left.CompareTo(right) >= 0;

    public static bool operator <=(ReferencePeriod left, ReferencePeriod right) => left.CompareTo(right) <= 0;

    public static bool operator <(ReferencePeriod left, ReferencePeriod right) => left.CompareTo(right) < 0;

    public static bool operator >(ReferencePeriod left, ReferencePeriod right) => left.CompareTo(right) > 0;
}
