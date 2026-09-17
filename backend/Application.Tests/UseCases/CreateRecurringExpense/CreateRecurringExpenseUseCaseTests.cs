using ContasEmDia.Application.UseCases.CreateRecurringExpense;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Application.Tests.UseCases.CreateRecurringExpense;

public class CreateRecurringExpenseUseCaseTests
{
    private static CreateRecurringExpenseUseCaseInput CreateValidInput(
        string name = "Aluguel",
        string category = "Housing",
        decimal monthlyAmount = 1500m,
        int dueDay = 10,
        string startDate = "2026-08-01",
        string endDate = "2026-08-31",
        string frequency = "Monthly",
        string status = "Active",
        string? note = null) => new()
    {
        Name = name,
        Category = category,
        MonthlyAmount = monthlyAmount,
        DueDay = dueDay,
        StartDate = startDate,
        EndDate = endDate,
        Frequency = frequency,
        Status = status,
        Note = note,
    };

    private static (CreateRecurringExpenseUseCase UseCase, InMemoryRecurringExpenseRepository Repository) CreateSut(DateOnly currentDate)
    {
        var repository = new InMemoryRecurringExpenseRepository();
        var repositoryManager = new FakeRepositoryManager(repository);
        var currentDateProvider = new FixedCurrentDateProvider(currentDate);
        var useCase = new CreateRecurringExpenseUseCase(repositoryManager, currentDateProvider);

        return (useCase, repository);
    }

    [Fact]
    public async Task ExecuteAsync_ActiveExpenseStartingInCurrentCompetencia_ReturnsSuccessWithGeneratedOccurrenceAndPersists()
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        var input = CreateValidInput(status: "Active", startDate: "2026-08-01");

        var output = await useCase.ExecuteAsync(input);

        Assert.True(output.IsSuccess);
        Assert.Single(repository.StoredExpenses);
        var createdExpense = repository.StoredExpenses.Single();

        Assert.Equal(createdExpense.GetId(), output.Id);
        Assert.Equal(createdExpense.GetName().GetValue(), output.Name);
        Assert.Equal(createdExpense.GetCategory().GetValue().ToString(), output.Category);
        Assert.Equal(createdExpense.GetMonthlyAmount().GetValue(), output.MonthlyAmount);
        Assert.Equal(createdExpense.GetDueDay().GetValue(), output.DueDay);
        Assert.Equal(createdExpense.GetStartDate().GetValue(), output.StartDate);
        Assert.Equal(createdExpense.GetFrequency().GetValue().ToString(), output.Frequency);
        Assert.Equal(createdExpense.GetStatus().GetValue().ToString(), output.Status);
        Assert.Equal(createdExpense.GetNote().GetValue(), output.Note);

        var createdOccurrence = createdExpense.GetOccurrences().Single();
        var outputOccurrence = Assert.Single(output.Occurrences!);

        Assert.Equal(createdOccurrence.GetId(), outputOccurrence.Id);
        Assert.Equal(createdOccurrence.GetReferencePeriod().Year, outputOccurrence.ReferenceYear);
        Assert.Equal(createdOccurrence.GetReferencePeriod().Month, outputOccurrence.ReferenceMonth);
        Assert.Equal(createdOccurrence.GetDueDate().GetValue(), outputOccurrence.DueDate);
        Assert.Equal(createdOccurrence.GetStatus().GetValue().ToString(), outputOccurrence.Status);
        Assert.Equal(createdOccurrence.GetName().GetValue(), outputOccurrence.Name);
        Assert.Equal(createdOccurrence.GetCategory().GetValue().ToString(), outputOccurrence.Category);
        Assert.Equal(createdOccurrence.GetExpectedAmount().GetValue(), outputOccurrence.ExpectedAmount);
    }

    [Theory]
    [InlineData("Active", "2026-09-01", "2026-09-30")]
    [InlineData("Paused", "2026-09-01", "2026-09-30")]
    public async Task ExecuteAsync_FutureStartExpense_ReturnsSuccessWithEmptyOccurrenceList(string status, string startDate, string endDate)
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        var input = CreateValidInput(status: status, startDate: startDate, endDate: endDate);

        var output = await useCase.ExecuteAsync(input);

        Assert.True(output.IsSuccess);
        Assert.Single(repository.StoredExpenses);
        Assert.Empty(output.Occurrences!);
    }

    [Fact]
    public async Task ExecuteAsync_PausedExpenseStartingInCurrentCompetencia_ReturnsSuccessWithGeneratedOccurrence()
    {
        // Decision 1 (refinamento data-fim-despesa-recorrente): generation at cadastro no longer
        // checks status — a Paused despesa generates its vigência's occurrences just like Active.
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        var input = CreateValidInput(status: "Paused", startDate: "2026-08-01", endDate: "2026-08-31");

        var output = await useCase.ExecuteAsync(input);

        Assert.True(output.IsSuccess);
        Assert.Single(repository.StoredExpenses);
        Assert.Single(output.Occurrences!);
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("Paused")]
    public async Task ExecuteAsync_ValidVigenciaSpanningMultipleCompetencias_ReturnsOneOccurrencePerCompetencia(string status)
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        var input = CreateValidInput(status: status, startDate: "2026-08-01", endDate: "2026-11-15");

        var output = await useCase.ExecuteAsync(input);

        Assert.True(output.IsSuccess);
        Assert.Equal(new DateOnly(2026, 11, 15), output.EndDate);
        Assert.Equal(4, output.Occurrences!.Count);
    }

    [Fact]
    public async Task ExecuteAsync_MalformedEndDate_ReturnsFieldErrorForEndDate()
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        var input = CreateValidInput(endDate: "not-a-date");

        var output = await useCase.ExecuteAsync(input);

        Assert.False(output.IsSuccess);
        var error = Assert.Single(output.Errors);
        Assert.Equal("endDate", error.Field);
        Assert.Equal("Data de fim inválida.", error.Message);
        Assert.Empty(repository.StoredExpenses);
    }

    [Fact]
    public async Task ExecuteAsync_EndDateOnOrBeforeStartDate_ReturnsFieldErrorForEndDateWithDomainMessage()
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        var input = CreateValidInput(startDate: "2026-08-01", endDate: "2026-08-01");

        var output = await useCase.ExecuteAsync(input);

        Assert.False(output.IsSuccess);
        var error = Assert.Single(output.Errors);
        Assert.Equal("endDate", error.Field);
        Assert.Equal("A data de fim deve ser posterior à data de início.", error.Message);
        Assert.Empty(repository.StoredExpenses);
    }

    [Fact]
    public async Task ExecuteAsync_EndDateBeyondOneYearFromStartDate_ReturnsFieldErrorForEndDateWithDomainMessage()
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        var input = CreateValidInput(startDate: "2026-08-01", endDate: "2027-08-02");

        var output = await useCase.ExecuteAsync(input);

        Assert.False(output.IsSuccess);
        var error = Assert.Single(output.Errors);
        Assert.Equal("endDate", error.Field);
        Assert.Equal("A vigência não pode ultrapassar 1 ano a partir da data de início.", error.Message);
        Assert.Empty(repository.StoredExpenses);
    }

    [Fact]
    public async Task ExecuteAsync_EndDateCompetenciaBeforeCurrentReferencePeriod_ReturnsFieldErrorForEndDateWithDomainMessage()
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        var input = CreateValidInput(startDate: "2026-01-01", endDate: "2026-07-15");

        var output = await useCase.ExecuteAsync(input);

        Assert.False(output.IsSuccess);
        var error = Assert.Single(output.Errors);
        Assert.Equal("endDate", error.Field);
        Assert.Equal("A data de fim não pode estar no passado.", error.Message);
        Assert.Empty(repository.StoredExpenses);
    }

    [Fact]
    public async Task ExecuteAsync_SingleInvalidField_ReturnsSingleFieldErrorWithDomainMessageAndDoesNotPersist()
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        var input = CreateValidInput(name: "");
        var expectedMessage = Assert.Throws<ArgumentException>(() => new ExpenseName("")).Message;

        var output = await useCase.ExecuteAsync(input);

        Assert.False(output.IsSuccess);
        var error = Assert.Single(output.Errors);
        Assert.Equal("name", error.Field);
        Assert.Equal(expectedMessage, error.Message);
        Assert.Empty(repository.StoredExpenses);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleInvalidFieldsSimultaneously_ReturnsOneFieldErrorPerInvalidFieldAndDoesNotPersist()
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        var input = CreateValidInput(name: "", category: "Inexistente");

        var output = await useCase.ExecuteAsync(input);

        Assert.False(output.IsSuccess);
        Assert.Equal(2, output.Errors.Count);
        Assert.Contains(output.Errors, e => e.Field == "name");
        Assert.Contains(output.Errors, e => e.Field == "category");
        Assert.Empty(repository.StoredExpenses);
    }

    [Fact]
    public async Task ExecuteAsync_CategoryOutsideSupportedValues_ReturnsFieldErrorWithoutConstructingExpenseCategory()
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        var input = CreateValidInput(category: "Inexistente");

        var output = await useCase.ExecuteAsync(input);

        Assert.False(output.IsSuccess);
        var error = Assert.Single(output.Errors);
        Assert.Equal("category", error.Field);
        Assert.Empty(repository.StoredExpenses);
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2026-02-31")]
    public async Task ExecuteAsync_MalformedOrCalendariallyInvalidStartDate_ReturnsFieldErrorForStartDate(string startDate)
    {
        var (useCase, repository) = CreateSut(new DateOnly(2026, 8, 15));
        var input = CreateValidInput(startDate: startDate);

        var output = await useCase.ExecuteAsync(input);

        Assert.False(output.IsSuccess);
        var error = Assert.Single(output.Errors);
        Assert.Equal("startDate", error.Field);
        Assert.Empty(repository.StoredExpenses);
    }

    [Fact]
    public async Task ExecuteAsync_RepositoryThrowsOnAddAsync_PropagatesExceptionInsteadOfReturningOutput()
    {
        var repositoryManager = new FakeRepositoryManager(new ThrowingRecurringExpenseRepository());
        var currentDateProvider = new FixedCurrentDateProvider(new DateOnly(2026, 8, 15));
        var useCase = new CreateRecurringExpenseUseCase(repositoryManager, currentDateProvider);
        var input = CreateValidInput();

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(input));
    }
}
