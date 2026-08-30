using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Tests.ValueObjects;

public class OccurrenceStatusTests
{
    [Theory]
    [InlineData(OccurrenceStatusType.Pending)]
    [InlineData(OccurrenceStatusType.Paid)]
    public void Constructor_DefinedStatusType_IsAccepted(OccurrenceStatusType type)
    {
        var status = new OccurrenceStatus(type);

        Assert.Equal(type, status.GetValue());
    }

    [Fact]
    public void Constructor_UndefinedStatusType_Throws()
    {
        Assert.Throws<ArgumentException>(() => new OccurrenceStatus((OccurrenceStatusType)999));
    }
}
