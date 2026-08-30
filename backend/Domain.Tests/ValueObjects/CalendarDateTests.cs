using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Tests.ValueObjects;

public class CalendarDateTests
{
    [Fact]
    public void Constructor_ValidDateOnly_RoundTripsViaGetValue()
    {
        var date = new DateOnly(2026, 3, 15);

        var calendarDate = new CalendarDate(date);

        Assert.Equal(date, calendarDate.GetValue());
    }
}
