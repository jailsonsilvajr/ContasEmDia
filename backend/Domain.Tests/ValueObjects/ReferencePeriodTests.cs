using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Tests.ValueObjects;

public class ReferencePeriodTests
{
    [Fact]
    public void Constructor_MonthBelowRange_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ReferencePeriod(2026, 0));
    }

    [Fact]
    public void Constructor_MonthAboveRange_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ReferencePeriod(2026, 13));
    }

    [Fact]
    public void Constructor_YearZeroOrNegative_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ReferencePeriod(0, 5));
    }

    [Fact]
    public void FromDate_DerivesCorrectYearAndMonth()
    {
        var period = ReferencePeriod.FromDate(new DateOnly(2026, 8, 30));

        Assert.Equal(2026, period.Year);
        Assert.Equal(8, period.Month);
    }

    [Fact]
    public void ComparisonOperators_OrderPeriodsAcrossYearBoundary()
    {
        var december2025 = new ReferencePeriod(2025, 12);
        var january2026 = new ReferencePeriod(2026, 1);

        Assert.True(december2025 < january2026);
        Assert.True(january2026 >= december2025);
        Assert.False(december2025 >= january2026);
    }

    [Fact]
    public void CompareTo_OrdersPeriodsWithinSameYear()
    {
        var march = new ReferencePeriod(2026, 3);
        var august = new ReferencePeriod(2026, 8);

        Assert.True(march.CompareTo(august) < 0);
        Assert.True(august.CompareTo(march) > 0);
        Assert.Equal(0, august.CompareTo(new ReferencePeriod(2026, 8)));
    }
}
