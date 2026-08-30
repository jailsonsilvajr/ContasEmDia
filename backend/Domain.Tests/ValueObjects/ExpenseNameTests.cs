using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Tests.ValueObjects;

public class ExpenseNameTests
{
    [Fact]
    public void Constructor_ValidNonEmptyName_IsAccepted()
    {
        var name = new ExpenseName("Aluguel");

        Assert.Equal("Aluguel", name.GetValue());
    }

    [Fact]
    public void Constructor_NullName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ExpenseName(null!));
    }

    [Fact]
    public void Constructor_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ExpenseName(""));
    }

    [Fact]
    public void Constructor_WhitespaceOnlyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ExpenseName("   "));
    }
}
