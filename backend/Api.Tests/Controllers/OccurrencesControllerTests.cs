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

public sealed class OccurrencesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Endpoint = "/api/v1/occurrences";

    private readonly HttpClient _client;

    public OccurrencesControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_MonthOutOfRange_Returns400WithPeriodFieldError()
    {
        var response = await _client.GetAsync($"{Endpoint}?year=2026&month=13");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        var errors = root.GetProperty("errors");
        Assert.Equal(1, errors.GetArrayLength());
        Assert.Equal("period", errors[0].GetProperty("field").GetString());
    }

    [Fact]
    public async Task Get_NonNumericYearOrMonth_Returns400WithPeriodFieldError()
    {
        var response = await _client.GetAsync($"{Endpoint}?year=abc&month=8");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("period", root.GetProperty("errors")[0].GetProperty("field").GetString());
    }

    [Fact]
    public async Task Get_OnlyOneOfYearOrMonthProvided_Returns400WithPeriodFieldError()
    {
        var response = await _client.GetAsync($"{Endpoint}?month=8");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("period", root.GetProperty("errors")[0].GetProperty("field").GetString());
    }
}

public sealed class OccurrencesControllerSuccessTests
{
    // The EF Core InMemory provider (used by CustomWebApplicationFactory for
    // every other Api.Tests suite) cannot shape a query that Includes a child
    // entity with a ComplexProperty (Occurrence._referencePeriod) — a provider
    // limitation confirmed independent of this feature's code: it also
    // reproduces on the untouched, already-existing GetActiveAsync. The real
    // Npgsql provider (Infrastructure.Tests, via Testcontainers) handles the
    // same query correctly. These tests swap in a seeded in-memory repository
    // fake instead, keeping full HTTP/controller/mapping/serialization
    // coverage without touching that provider limitation.
    private static HttpClient CreateClient(params RecurringExpense[] seed) =>
        new OccurrencesSeededWebApplicationFactory(seed).CreateClient();

    private static RecurringExpense CreateActiveExpenseWithOccurrenceIn(ReferencePeriod period) => new(
        new ExpenseName("Aluguel"),
        new ExpenseCategory(ExpenseCategoryType.Housing),
        new Money(1500.00m),
        new DueDay(10),
        new CalendarDate(new DateOnly(period.Year, period.Month, 1)),
        new Frequency(FrequencyType.Monthly),
        new RecurringExpenseStatus(RecurringExpenseStatusType.Active),
        new Note(null),
        period);

    [Fact]
    public async Task Get_NoQueryParamsWithExistingOccurrence_Returns200WithOccurrenceData()
    {
        var currentPeriod = ReferencePeriod.FromDate(DateOnly.FromDateTime(DateTime.Now));
        var client = CreateClient(CreateActiveExpenseWithOccurrenceIn(currentPeriod));

        var response = await client.GetAsync("/api/v1/occurrences");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.True(root.GetProperty("data").GetProperty("occurrences").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Get_PeriodWithNoOccurrences_Returns200WithEmptyList()
    {
        var client = CreateClient(CreateActiveExpenseWithOccurrenceIn(new ReferencePeriod(2026, 8)));

        var response = await client.GetAsync("/api/v1/occurrences?year=2020&month=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(0, root.GetProperty("data").GetProperty("occurrences").GetArrayLength());
    }

    [Fact]
    public async Task Patch_ValidAmountAndDate_Returns200WithPaidOccurrence()
    {
        var expense = CreateActiveExpenseWithOccurrenceIn(ReferencePeriod.FromDate(DateOnly.FromDateTime(DateTime.Now)));
        var occurrenceId = expense.GetOccurrences().Single().GetId();
        var client = CreateClient(expense);

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/occurrences/{occurrenceId}/payment",
            new { paidAmount = "1500,00", paymentDate = "18/08/2026" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        var occurrence = root.GetProperty("data").GetProperty("occurrence");
        Assert.Equal("Paid", occurrence.GetProperty("status").GetString());
        Assert.Equal(1500.00m, occurrence.GetProperty("paidAmount").GetDecimal());
        Assert.Equal("2026-08-18", occurrence.GetProperty("paymentDate").GetString());
    }

    [Fact]
    public async Task Patch_OccurrenceNotFound_Returns404()
    {
        var client = CreateClient();

        var response = await client.PatchAsJsonAsync($"/api/v1/occurrences/{Guid.NewGuid()}/payment", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("Ocorrência não encontrada.", root.GetProperty("errors")[0].GetProperty("message").GetString());
    }

    [Fact]
    public async Task Patch_OccurrenceAlreadyPaid_Returns400()
    {
        var expense = CreateActiveExpenseWithOccurrenceIn(ReferencePeriod.FromDate(DateOnly.FromDateTime(DateTime.Now)));
        var occurrence = expense.GetOccurrences().Single();
        occurrence.MarkAsPaid(new Money(1500m), new CalendarDate(DateOnly.FromDateTime(DateTime.Now)));
        var client = CreateClient(expense);

        var response = await client.PatchAsJsonAsync($"/api/v1/occurrences/{occurrence.GetId()}/payment", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("Esta ocorrência já está paga.", root.GetProperty("errors")[0].GetProperty("message").GetString());
    }

    [Fact]
    public async Task Delete_PaidOccurrenceDueInPast_Returns200WithRecalculatedOverdueStatus()
    {
        // A fixed period far in the past guarantees the recalculated status is
        // Overdue regardless of when this test actually runs (GetByOccurrenceIdAsync
        // looks up the occurrence directly, independent of any "current period" filter).
        var pastPeriod = new ReferencePeriod(2020, 1);
        var expense = new RecurringExpense(
            new ExpenseName("Aluguel"),
            new ExpenseCategory(ExpenseCategoryType.Housing),
            new Money(1500.00m),
            new DueDay(1),
            new CalendarDate(new DateOnly(pastPeriod.Year, pastPeriod.Month, 1)),
            new Frequency(FrequencyType.Monthly),
            new RecurringExpenseStatus(RecurringExpenseStatusType.Active),
            new Note(null),
            pastPeriod);
        var occurrence = expense.GetOccurrences().Single();
        occurrence.MarkAsPaid(new Money(1500m), new CalendarDate(DateOnly.FromDateTime(DateTime.Now)));
        var client = CreateClient(expense);

        var response = await client.DeleteAsync($"/api/v1/occurrences/{occurrence.GetId()}/payment");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        var responseOccurrence = root.GetProperty("data").GetProperty("occurrence");
        Assert.Equal("Overdue", responseOccurrence.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, responseOccurrence.GetProperty("paidAmount").ValueKind);
        Assert.Equal(JsonValueKind.Null, responseOccurrence.GetProperty("paymentDate").ValueKind);
    }

    [Fact]
    public async Task Delete_OccurrenceNotFound_Returns404()
    {
        var client = CreateClient();

        var response = await client.DeleteAsync($"/api/v1/occurrences/{Guid.NewGuid()}/payment");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("Ocorrência não encontrada.", root.GetProperty("errors")[0].GetProperty("message").GetString());
    }

    [Fact]
    public async Task Delete_OccurrenceNotYetPaid_Returns400()
    {
        var expense = CreateActiveExpenseWithOccurrenceIn(ReferencePeriod.FromDate(DateOnly.FromDateTime(DateTime.Now)));
        var occurrence = expense.GetOccurrences().Single();
        var client = CreateClient(expense);

        var response = await client.DeleteAsync($"/api/v1/occurrences/{occurrence.GetId()}/payment");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("Esta ocorrência ainda não foi paga.", root.GetProperty("errors")[0].GetProperty("message").GetString());
    }
}

public sealed class OccurrencesSeededWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly IServiceProvider InMemoryProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

    private readonly RecurringExpense[] _seed;

    public OccurrencesSeededWebApplicationFactory(RecurringExpense[] seed)
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
            services.AddSingleton<IRepositoryManager>(new SeededRepositoryManager(_seed));
        });
    }
}

internal sealed class SeededRepositoryManager : IRepositoryManager
{
    public SeededRepositoryManager(IEnumerable<RecurringExpense> seed)
    {
        RecurringExpenseRepository = new SeededRecurringExpenseRepository(seed);
    }

    public IRecurringExpenseRepository RecurringExpenseRepository { get; }
}

internal sealed class SeededRecurringExpenseRepository : IRecurringExpenseRepository
{
    private readonly List<RecurringExpense> _store;

    public SeededRecurringExpenseRepository(IEnumerable<RecurringExpense> seed)
    {
        _store = seed.ToList();
    }

    public Task AddAsync(RecurringExpense recurringExpense) => throw new NotImplementedException();

    public Task<RecurringExpense?> GetByIdAsync(Guid id) => throw new NotImplementedException();

    public Task<IReadOnlyCollection<RecurringExpense>> GetActiveAsync() => throw new NotImplementedException();

    public Task<IReadOnlyCollection<RecurringExpense>> GetByReferencePeriodAsync(ReferencePeriod referencePeriod)
    {
        IReadOnlyCollection<RecurringExpense> matching = _store
            .Where(expense => expense.GetOccurrencesForPeriod(referencePeriod).Count > 0)
            .ToList();

        return Task.FromResult(matching);
    }

    public Task<RecurringExpense?> GetByOccurrenceIdAsync(Guid occurrenceId)
    {
        var recurringExpense = _store.FirstOrDefault(expense => expense.FindOccurrence(occurrenceId) is not null);
        return Task.FromResult(recurringExpense);
    }

    public Task UpdateAsync(RecurringExpense recurringExpense) => Task.CompletedTask;
}

public sealed class OccurrencesThrowingWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly IServiceProvider InMemoryProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

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
            services.AddScoped<IRepositoryManager, OccurrencesThrowingRepositoryManager>();
        });
    }
}

public sealed class OccurrencesControllerFailureTests : IClassFixture<OccurrencesThrowingWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OccurrencesControllerFailureTests(OccurrencesThrowingWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_WhenRepositoryThrows_Returns500WithGenericEnvelope()
    {
        var response = await _client.GetAsync("/api/v1/occurrences");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Single(root.GetProperty("errors").EnumerateArray());
    }

    [Fact]
    public async Task Patch_WhenRepositoryThrows_Returns500WithGenericEnvelope()
    {
        var response = await _client.PatchAsJsonAsync($"/api/v1/occurrences/{Guid.NewGuid()}/payment", new { });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Single(root.GetProperty("errors").EnumerateArray());
    }

    [Fact]
    public async Task Delete_WhenRepositoryThrows_Returns500WithGenericEnvelope()
    {
        var response = await _client.DeleteAsync($"/api/v1/occurrences/{Guid.NewGuid()}/payment");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Single(root.GetProperty("errors").EnumerateArray());
    }
}

internal sealed class OccurrencesThrowingRepositoryManager : IRepositoryManager
{
    public IRecurringExpenseRepository RecurringExpenseRepository => new OccurrencesThrowingRecurringExpenseRepository();
}

internal sealed class OccurrencesThrowingRecurringExpenseRepository : IRecurringExpenseRepository
{
    public Task AddAsync(RecurringExpense recurringExpense) => throw new NotImplementedException();

    public Task<RecurringExpense?> GetByIdAsync(Guid id) => throw new NotImplementedException();

    public Task<IReadOnlyCollection<RecurringExpense>> GetActiveAsync() => throw new NotImplementedException();

    public Task<IReadOnlyCollection<RecurringExpense>> GetByReferencePeriodAsync(ReferencePeriod referencePeriod) =>
        throw new InvalidOperationException("Simulated persistence failure for testing the 500 safety net.");

    public Task<RecurringExpense?> GetByOccurrenceIdAsync(Guid occurrenceId) =>
        throw new InvalidOperationException("Simulated persistence failure for testing the 500 safety net.");

    public Task UpdateAsync(RecurringExpense recurringExpense) =>
        throw new InvalidOperationException("Simulated persistence failure for testing the 500 safety net.");
}
