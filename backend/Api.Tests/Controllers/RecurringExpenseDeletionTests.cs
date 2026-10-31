using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.Repositories;
using ContasEmDia.Domain.ValueObjects;
using ContasEmDia.Infrastructure.Contexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ContasEmDia.Api.Tests.Controllers;

// Same seeded-fake-repository technique as RecurringExpenseEditingTests.cs (the EF Core InMemory
// provider cannot shape the Include query GetByIdAsync needs). Unlike that fixture's repository,
// this one's GetByIdAsync mirrors the real RecurringExpenseRepository's "_deletedAt filter" behavior
// (research.md #4), since several scenarios here depend on a deleted expense becoming "not found".
public sealed class RecurringExpenseDeletionTests
{
    private const string Endpoint = "/api/v1/recurring-expenses";

    private static HttpClient CreateClient(params RecurringExpense[] seed) =>
        new RecurringExpenseDeletionSeededWebApplicationFactory(seed).CreateClient();

    private static RecurringExpense CreateExpense(
        string name = "Aluguel",
        ExpenseCategoryType category = ExpenseCategoryType.Housing,
        decimal monthlyAmount = 1500m,
        int dueDay = 10,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        RecurringExpenseStatusType status = RecurringExpenseStatusType.Active,
        string? note = null,
        ReferencePeriod? currentReferencePeriod = null)
    {
        var resolvedStartDate = startDate ?? new DateOnly(2026, 1, 1);
        var resolvedCurrentReferencePeriod = currentReferencePeriod ?? new ReferencePeriod(2026, 1);
        var startPeriod = ReferencePeriod.FromDate(resolvedStartDate);
        var defaultEndPeriod = startPeriod > resolvedCurrentReferencePeriod ? startPeriod : resolvedCurrentReferencePeriod;
        var defaultEndDate = new DateOnly(
            defaultEndPeriod.Year,
            defaultEndPeriod.Month,
            DateTime.DaysInMonth(defaultEndPeriod.Year, defaultEndPeriod.Month));

        return new(
            new ExpenseName(name),
            new ExpenseCategory(category),
            new Money(monthlyAmount),
            new DueDay(dueDay),
            new CalendarDate(resolvedStartDate),
            new CalendarDate(endDate ?? defaultEndDate),
            new Frequency(FrequencyType.Monthly),
            new RecurringExpenseStatus(status),
            new Note(note),
            resolvedCurrentReferencePeriod);
    }

    private static object ValidUpdateBody(
        string startDate,
        string endDate,
        string status = "Active",
        string name = "Aluguel",
        string category = "Housing",
        decimal monthlyAmount = 1500m,
        int dueDay = 10,
        string? note = null) => new
    {
        name,
        category,
        monthlyAmount,
        dueDay,
        startDate,
        endDate,
        status,
        note
    };

    [Fact]
    public async Task Delete_PendingOnlyScopeWithPaidAndPendingOccurrence_Returns200AndKeepsPaidRemovesPending()
    {
        var expense = CreateExpense(
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 2, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 1));
        var paidOccurrence = expense.GetOccurrencesForPeriod(new ReferencePeriod(2026, 1)).Single();
        expense.MarkOccurrenceAsPaid(paidOccurrence.GetId(), new Money(1500m), new CalendarDate(new DateOnly(2026, 1, 5)));
        var client = CreateClient(expense);

        var response = await client.DeleteAsync($"{Endpoint}/{expense.GetId()}?scope=pending-only");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var data = body!.RootElement.GetProperty("data");
        Assert.Equal("pending-only", data.GetProperty("scope").GetString());
        Assert.Equal(expense.GetId(), data.GetProperty("recurringExpenseId").GetGuid());

        var remaining = Assert.Single(expense.GetOccurrences());
        Assert.Equal(paidOccurrence.GetId(), remaining.GetId());
    }

    [Fact]
    public async Task Delete_PendingOnlyScopeWithOnlyPaidOccurrences_Returns200AndRemovesNothing()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 1));
        var occurrence = expense.GetOccurrences().Single();
        expense.MarkOccurrenceAsPaid(occurrence.GetId(), new Money(1500m), new CalendarDate(new DateOnly(2026, 1, 5)));
        var client = CreateClient(expense);

        var response = await client.DeleteAsync($"{Endpoint}/{expense.GetId()}?scope=pending-only");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(expense.GetOccurrences());
    }

    [Fact]
    public async Task Delete_AllScopeWithPaidAndPendingOccurrences_Returns200AndSubsequentGetReturns404()
    {
        var expense = CreateExpense(
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 2, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 1));
        var paidOccurrence = expense.GetOccurrencesForPeriod(new ReferencePeriod(2026, 1)).Single();
        expense.MarkOccurrenceAsPaid(paidOccurrence.GetId(), new Money(1500m), new CalendarDate(new DateOnly(2026, 1, 5)));
        var client = CreateClient(expense);

        var response = await client.DeleteAsync($"{Endpoint}/{expense.GetId()}?scope=all");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var data = body!.RootElement.GetProperty("data");
        Assert.Equal("all", data.GetProperty("scope").GetString());

        var getResponse = await client.GetAsync($"{Endpoint}/{expense.GetId()}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_MissingScope_Returns400AndExpenseRemainsAccessible()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 1));
        var client = CreateClient(expense);

        var response = await client.DeleteAsync($"{Endpoint}/{expense.GetId()}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var errors = body!.RootElement.GetProperty("errors");
        Assert.Equal(1, errors.GetArrayLength());
        Assert.Equal("scope", errors[0].GetProperty("field").GetString());

        var getResponse = await client.GetAsync($"{Endpoint}/{expense.GetId()}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_InvalidScope_Returns400AndExpenseRemainsAccessible()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 1));
        var client = CreateClient(expense);

        var response = await client.DeleteAsync($"{Endpoint}/{expense.GetId()}?scope=invalido");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var errors = body!.RootElement.GetProperty("errors");
        Assert.Equal(1, errors.GetArrayLength());
        Assert.Equal("scope", errors[0].GetProperty("field").GetString());

        var getResponse = await client.GetAsync($"{Endpoint}/{expense.GetId()}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistentId_Returns404()
    {
        var client = CreateClient();

        var response = await client.DeleteAsync($"{Endpoint}/{Guid.NewGuid()}?scope=all");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AllScopeDeletedExpense_EveryFollowUpOperationReturns404()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 1));
        var client = CreateClient(expense);

        var deleteResponse = await client.DeleteAsync($"{Endpoint}/{expense.GetId()}?scope=all");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"{Endpoint}/{expense.GetId()}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        var putResponse = await client.PutAsJsonAsync(
            $"{Endpoint}/{expense.GetId()}",
            ValidUpdateBody("2026-01-01", "2026-01-31"));
        Assert.Equal(HttpStatusCode.NotFound, putResponse.StatusCode);

        var secondDeleteResponse = await client.DeleteAsync($"{Endpoint}/{expense.GetId()}?scope=all");
        Assert.Equal(HttpStatusCode.NotFound, secondDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_PendingOnlyScopeDeletedExpense_EveryFollowUpOperationReturns404()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 1));
        var client = CreateClient(expense);

        var deleteResponse = await client.DeleteAsync($"{Endpoint}/{expense.GetId()}?scope=pending-only");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"{Endpoint}/{expense.GetId()}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        var putResponse = await client.PutAsJsonAsync(
            $"{Endpoint}/{expense.GetId()}",
            ValidUpdateBody("2026-01-01", "2026-01-31"));
        Assert.Equal(HttpStatusCode.NotFound, putResponse.StatusCode);

        var secondDeleteResponse = await client.DeleteAsync($"{Endpoint}/{expense.GetId()}?scope=pending-only");
        Assert.Equal(HttpStatusCode.NotFound, secondDeleteResponse.StatusCode);
    }
}

public sealed class RecurringExpenseDeletionSeededWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly IServiceProvider InMemoryProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

    private readonly RecurringExpense[] _seed;

    public RecurringExpenseDeletionSeededWebApplicationFactory(RecurringExpense[] seed)
    {
        _seed = seed;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ContasEmDiaDbContext>>();

            services.AddDbContext<ContasEmDiaDbContext>(options =>
                options
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .UseInternalServiceProvider(InMemoryProvider));

            services.RemoveAll<IRepositoryManager>();
            services.AddSingleton<IRepositoryManager>(new DeletionSeededRepositoryManager(_seed));
        });
    }
}

internal sealed class DeletionSeededRepositoryManager : IRepositoryManager
{
    public DeletionSeededRepositoryManager(IEnumerable<RecurringExpense> seed)
    {
        RecurringExpenseRepository = new DeletionSeededRecurringExpenseRepository(seed);
    }

    public IRecurringExpenseRepository RecurringExpenseRepository { get; }
}

internal sealed class DeletionSeededRecurringExpenseRepository : IRecurringExpenseRepository
{
    private readonly List<RecurringExpense> _store;

    public DeletionSeededRecurringExpenseRepository(IEnumerable<RecurringExpense> seed)
    {
        _store = seed.ToList();
    }

    public Task AddAsync(RecurringExpense recurringExpense) => throw new NotImplementedException();

    public Task<RecurringExpense?> GetByIdAsync(Guid id) =>
        Task.FromResult(_store.FirstOrDefault(expense => expense.GetId() == id && !expense.IsDeleted()));

    public Task<IReadOnlyCollection<RecurringExpense>> GetActiveAsync() => throw new NotImplementedException();

    public Task<IReadOnlyCollection<RecurringExpense>> GetByReferencePeriodAsync(ReferencePeriod referencePeriod) =>
        throw new NotImplementedException();

    public Task<RecurringExpense?> GetByOccurrenceIdAsync(Guid occurrenceId) => throw new NotImplementedException();

    public Task UpdateAsync(RecurringExpense recurringExpense) => Task.CompletedTask;

    public Task RemoveAsync(RecurringExpense recurringExpense)
    {
        _store.RemoveAll(expense => expense.GetId() == recurringExpense.GetId());
        return Task.CompletedTask;
    }
}
