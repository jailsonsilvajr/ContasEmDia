using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Tests.ValueObjects;

public class OccurrenceDerivedStatusTests
{
    [Theory]
    [InlineData(OccurrenceDerivedStatusType.Paid)]
    [InlineData(OccurrenceDerivedStatusType.Overdue)]
    [InlineData(OccurrenceDerivedStatusType.DueSoon)]
    [InlineData(OccurrenceDerivedStatusType.Pending)]
    public void Constructor_DefinedStatusType_IsAccepted(OccurrenceDerivedStatusType type)
    {
        var status = new OccurrenceDerivedStatus(type);

        Assert.Equal(type, status.GetValue());
    }

    [Fact]
    public void Constructor_UndefinedStatusType_Throws()
    {
        Assert.Throws<ArgumentException>(() => new OccurrenceDerivedStatus((OccurrenceDerivedStatusType)999));
    }
}
