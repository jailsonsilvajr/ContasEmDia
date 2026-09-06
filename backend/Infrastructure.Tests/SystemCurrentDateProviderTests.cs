namespace ContasEmDia.Infrastructure.Tests;

public sealed class SystemCurrentDateProviderTests
{
    [Fact]
    public void GetCurrentDate_ReturnsTodaysDate()
    {
        var provider = new SystemCurrentDateProvider();

        var result = provider.GetCurrentDate();

        Assert.Equal(DateOnly.FromDateTime(DateTime.Now), result);
    }
}
