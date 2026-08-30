using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Theory]
    [InlineData(10)]
    [InlineData(10.5)]
    [InlineData(10.55)]
    [InlineData(0.01)]
    public void Constructor_PositiveValueWithUpToTwoDecimals_IsAccepted(decimal value)
    {
        var money = new Money(value);

        Assert.Equal(value, money.GetValue());
    }

    [Fact]
    public void Constructor_Zero_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Money(0m));
    }

    [Fact]
    public void Constructor_Negative_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Money(-10m));
    }

    [Fact]
    public void Constructor_MoreThanTwoDecimalPlaces_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Money(10.005m));
    }
}
