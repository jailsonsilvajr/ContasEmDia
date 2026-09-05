using ContasEmDia.Application.UseCases.CreateRecurringExpense;

namespace ContasEmDia.Application.Tests.UseCases.CreateRecurringExpense;

public class ManualValidationQuickstartTests
{
    [Fact]
    public async Task ExecuteAsync_QuickstartManualValidationScenario_ReturnsThreeFieldErrorsAndDoesNotPersist()
    {
        var repository = new InMemoryRecurringExpenseRepository();
        var repositoryManager = new FakeRepositoryManager(repository);
        var currentDateProvider = new FixedCurrentDateProvider(new DateOnly(2026, 8, 15));
        var useCase = new CreateRecurringExpenseUseCase(repositoryManager, currentDateProvider);

        var input = new CreateRecurringExpenseUseCaseInput
        {
            Name = "",
            Category = "Inexistente",
            MonthlyAmount = 0,
            DueDay = 10,
            StartDate = "2026-08-01",
            Frequency = "Monthly",
            Status = "Active",
        };

        var output = await useCase.ExecuteAsync(input);

        Assert.False(output.IsSuccess);
        Assert.Equal(3, output.Errors.Count);
        Assert.Contains(output.Errors, e => e.Field == "name");
        Assert.Contains(output.Errors, e => e.Field == "category");
        Assert.Contains(output.Errors, e => e.Field == "monthlyAmount");
        Assert.Empty(repository.StoredExpenses);
    }
}
