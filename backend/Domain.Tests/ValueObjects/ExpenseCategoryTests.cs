using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Tests.ValueObjects;

public class ExpenseCategoryTests
{
    [Theory]
    [InlineData(ExpenseCategoryType.Housing)]
    [InlineData(ExpenseCategoryType.Services)]
    [InlineData(ExpenseCategoryType.Transportation)]
    [InlineData(ExpenseCategoryType.Subscriptions)]
    [InlineData(ExpenseCategoryType.Other)]
    public void Constructor_DefinedCategoryType_IsAccepted(ExpenseCategoryType type)
    {
        var category = new ExpenseCategory(type);

        Assert.Equal(type, category.GetValue());
    }

    [Fact]
    public void Constructor_UndefinedCategoryType_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ExpenseCategory((ExpenseCategoryType)999));
    }
}
