using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Tests.ValueObjects;

public class FrequencyTests
{
    [Fact]
    public void Constructor_Monthly_IsAccepted()
    {
        var frequency = new Frequency(FrequencyType.Monthly);

        Assert.Equal(FrequencyType.Monthly, frequency.GetValue());
    }

    [Fact]
    public void Constructor_UndefinedFrequencyType_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Frequency((FrequencyType)999));
    }
}
