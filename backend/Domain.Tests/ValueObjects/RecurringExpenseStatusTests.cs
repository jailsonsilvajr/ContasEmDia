using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Tests.ValueObjects;

public class RecurringExpenseStatusTests
{
    [Theory]
    [InlineData(RecurringExpenseStatusType.Active)]
    [InlineData(RecurringExpenseStatusType.Paused)]
    public void Constructor_DefinedStatusType_IsAccepted(RecurringExpenseStatusType type)
    {
        var status = new RecurringExpenseStatus(type);

        Assert.Equal(type, status.GetValue());
    }

    [Fact]
    public void Constructor_UndefinedStatusType_Throws()
    {
        Assert.Throws<ArgumentException>(() => new RecurringExpenseStatus((RecurringExpenseStatusType)999));
    }
}
