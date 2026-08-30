using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Tests.ValueObjects;

public class DueDayTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(31)]
    [InlineData(15)]
    public void Constructor_ValueWithinRange_IsAccepted(int value)
    {
        var dueDay = new DueDay(value);

        Assert.Equal(value, dueDay.GetValue());
    }

    [Fact]
    public void Constructor_Zero_Throws()
    {
        Assert.Throws<ArgumentException>(() => new DueDay(0));
    }

    [Fact]
    public void Constructor_ThirtyTwo_Throws()
    {
        Assert.Throws<ArgumentException>(() => new DueDay(32));
    }
}
