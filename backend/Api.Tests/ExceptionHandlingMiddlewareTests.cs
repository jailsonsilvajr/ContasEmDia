using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.Entities;
using ContasEmDia.Domain.Repositories;
using ContasEmDia.Domain.ValueObjects;
using ContasEmDia.Infrastructure.Contexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ContasEmDia.Api.Tests;

public sealed class ExceptionHandlingMiddlewareTests : IClassFixture<ThrowingRepositoryWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ExceptionHandlingMiddlewareTests(ThrowingRepositoryWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WhenPersistenceThrows_Returns500WithGenericEnvelopeAndNoExceptionDetails()
    {
        var payload = new
        {
            name = "Aluguel",
            category = "Housing",
            monthlyAmount = 1850.00m,
            dueDay = 10,
            startDate = "2026-09-01",
            frequency = "Monthly",
            status = "Active",
            note = (string?)null
        };

        var response = await _client.PostAsJsonAsync("/api/v1/recurring-expenses", payload);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var root = JsonDocument.Parse(body).RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("data").ValueKind);
        Assert.Single(root.GetProperty("errors").EnumerateArray());
        Assert.DoesNotContain("RecurringExpenseRepository", body);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetById_WhenPersistenceThrows_Returns500WithGenericEnvelopeAndNoExceptionDetails()
    {
        var response = await _client.GetAsync($"/api/v1/recurring-expenses/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var root = JsonDocument.Parse(body).RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("data").ValueKind);
        Assert.Single(root.GetProperty("errors").EnumerateArray());
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Put_WhenPersistenceThrows_Returns500WithGenericEnvelopeAndNoExceptionDetails()
    {
        var payload = new
        {
            name = "Aluguel",
            category = "Housing",
            monthlyAmount = 1850.00m,
            dueDay = 10,
            startDate = "2026-09-01",
            status = "Active",
            note = (string?)null
        };

        var response = await _client.PutAsJsonAsync($"/api/v1/recurring-expenses/{Guid.NewGuid()}", payload);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var root = JsonDocument.Parse(body).RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("data").ValueKind);
        Assert.Single(root.GetProperty("errors").EnumerateArray());
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class DomainRuleViolationMiddlewareTests : IClassFixture<AlreadyPaidOccurrenceWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DomainRuleViolationMiddlewareTests(AlreadyPaidOccurrenceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Patch_OccurrenceAlreadyPaid_Returns400WithBusinessRuleMessage()
    {
        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/occurrences/{AlreadyPaidOccurrenceWebApplicationFactory.OccurrenceId}/payment",
            new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("Esta ocorrência já está paga.", root.GetProperty("errors")[0].GetProperty("message").GetString());
    }
}

public sealed class NotFoundOccurrenceMiddlewareTests : IClassFixture<NotFoundOccurrenceWebApplicationFactory>
{
    private readonly HttpClient _client;

    public NotFoundOccurrenceMiddlewareTests(NotFoundOccurrenceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Patch_OccurrenceNotFound_Returns404WithNotFoundMessage()
    {
        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/occurrences/{Guid.NewGuid()}/payment",
            new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("Ocorrência não encontrada.", root.GetProperty("errors")[0].GetProperty("message").GetString());
    }
}

public sealed class AlreadyPaidOccurrenceWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly IServiceProvider InMemoryProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

    private static readonly RecurringExpense Expense = CreateAlreadyPaidExpense();

    public static Guid OccurrenceId => Expense.GetOccurrences().Single().GetId();

    private static RecurringExpense CreateAlreadyPaidExpense()
    {
        var period = ReferencePeriod.FromDate(DateOnly.FromDateTime(DateTime.Now));
        var expense = new RecurringExpense(
            new ExpenseName("Aluguel"),
            new ExpenseCategory(ExpenseCategoryType.Housing),
            new Money(1500.00m),
            new DueDay(10),
            new CalendarDate(new DateOnly(period.Year, period.Month, 1)),
            new Frequency(FrequencyType.Monthly),
            new RecurringExpenseStatus(RecurringExpenseStatusType.Active),
            new Note(null),
            period);

        var occurrence = expense.GetOccurrences().Single();
        occurrence.MarkAsPaid(new Money(1500.00m), new CalendarDate(DateOnly.FromDateTime(DateTime.Now)));

        return expense;
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
            services.AddSingleton<IRepositoryManager>(new SingleExpenseRepositoryManager(Expense));
        });
    }
}

public sealed class NotFoundOccurrenceWebApplicationFactory : WebApplicationFactory<Program>
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
            services.AddSingleton<IRepositoryManager>(new SingleExpenseRepositoryManager(recurringExpense: null));
        });
    }
}

internal sealed class SingleExpenseRepositoryManager : IRepositoryManager
{
    public SingleExpenseRepositoryManager(RecurringExpense? recurringExpense)
    {
        RecurringExpenseRepository = new SingleExpenseRecurringExpenseRepository(recurringExpense);
    }

    public IRecurringExpenseRepository RecurringExpenseRepository { get; }
}

internal sealed class SingleExpenseRecurringExpenseRepository : IRecurringExpenseRepository
{
    private readonly RecurringExpense? _recurringExpense;

    public SingleExpenseRecurringExpenseRepository(RecurringExpense? recurringExpense)
    {
        _recurringExpense = recurringExpense;
    }

    public Task AddAsync(RecurringExpense recurringExpense) => throw new NotImplementedException();

    public Task<RecurringExpense?> GetByIdAsync(Guid id) => throw new NotImplementedException();

    public Task<IReadOnlyCollection<RecurringExpense>> GetActiveAsync() => throw new NotImplementedException();

    public Task<IReadOnlyCollection<RecurringExpense>> GetByReferencePeriodAsync(ReferencePeriod referencePeriod) =>
        throw new NotImplementedException();

    public Task<RecurringExpense?> GetByOccurrenceIdAsync(Guid occurrenceId) => Task.FromResult(_recurringExpense);

    public Task UpdateAsync(RecurringExpense recurringExpense) => Task.CompletedTask;
}

public sealed class ThrowingRepositoryWebApplicationFactory : WebApplicationFactory<Program>
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
            services.AddScoped<IRepositoryManager, ThrowingRepositoryManager>();
        });
    }
}

internal sealed class ThrowingRepositoryManager : IRepositoryManager
{
    public IRecurringExpenseRepository RecurringExpenseRepository => new ThrowingRecurringExpenseRepository();
}

internal sealed class ThrowingRecurringExpenseRepository : IRecurringExpenseRepository
{
    public Task AddAsync(RecurringExpense recurringExpense) =>
        throw new InvalidOperationException("Simulated persistence failure for testing the 500 safety net.");

    public Task<RecurringExpense?> GetByIdAsync(Guid id) =>
        throw new InvalidOperationException("Simulated persistence failure for testing the 500 safety net.");

    public Task<IReadOnlyCollection<RecurringExpense>> GetActiveAsync() => throw new NotImplementedException();

    public Task<IReadOnlyCollection<RecurringExpense>> GetByReferencePeriodAsync(ReferencePeriod referencePeriod) =>
        throw new NotImplementedException();

    public Task<RecurringExpense?> GetByOccurrenceIdAsync(Guid occurrenceId) =>
        throw new NotImplementedException();

    public Task UpdateAsync(RecurringExpense recurringExpense) =>
        throw new NotImplementedException();
}
