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

// The EF Core InMemory provider (used by CustomWebApplicationFactory for
// every other Api.Tests suite) cannot shape a query that Includes a child
// entity with a ComplexProperty (Occurrence._referencePeriod) — the same
// provider limitation already documented and worked around in
// OccurrencesControllerTests.cs for GetByOccurrenceIdAsync (which also calls
// GetByIdAsync internally). GetRecurringExpenseByIdUseCase/
// UpdateRecurringExpenseUseCase call GetByIdAsync directly, so these tests
// use the same seeded-fake-repository technique instead, keeping full
// HTTP/controller/mapping/serialization coverage without touching that
// provider limitation.
public sealed class RecurringExpenseEditingTests
{
    private const string Endpoint = "/api/v1/recurring-expenses";

    private static HttpClient CreateClient(params RecurringExpense[] seed) =>
        new RecurringExpenseSeededWebApplicationFactory(seed).CreateClient();

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
        var resolvedStartDate = startDate ?? new DateOnly(2026, 8, 1);
        var resolvedCurrentReferencePeriod = currentReferencePeriod ?? new ReferencePeriod(2026, 8);
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

    private static DateOnly FirstDayOfMonthOffset(int monthsFromNow)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return new DateOnly(today.Year, today.Month, 1).AddMonths(monthsFromNow);
    }

    private static DateOnly LastDayOfMonthOffset(int monthsFromNow)
    {
        var target = FirstDayOfMonthOffset(monthsFromNow);
        return new DateOnly(target.Year, target.Month, DateTime.DaysInMonth(target.Year, target.Month));
    }

    private static object ValidUpdateBody(
        string startDate,
        string endDate = "2026-08-31",
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
    public async Task Put_EditFieldsWithExistingOccurrence_Returns200AndOccurrenceKeepsOldValues()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrence = expense.GetOccurrences().Single();
        var client = CreateClient(expense);

        var response = await client.PutAsJsonAsync(
            $"{Endpoint}/{expense.GetId()}",
            ValidUpdateBody("2026-08-01", name: "Aluguel do apartamento", category: "Services", monthlyAmount: 1900.00m, dueDay: 20));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var data = body!.RootElement.GetProperty("data");

        Assert.True(body.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Aluguel do apartamento", data.GetProperty("name").GetString());
        Assert.Equal("Services", data.GetProperty("category").GetString());

        Assert.Single(expense.GetOccurrences());
        Assert.Equal("Aluguel", occurrence.GetName().GetValue());
        Assert.Equal(ExpenseCategoryType.Housing, occurrence.GetCategory().GetValue());
        Assert.Equal(1500m, occurrence.GetExpectedAmount().GetValue());
    }

    [Fact]
    public async Task Put_NoFieldsChanged_Returns200AndDoesNotTouchOccurrences()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        var countBefore = expense.GetOccurrences().Count;
        var client = CreateClient(expense);

        var response = await client.PutAsJsonAsync($"{Endpoint}/{expense.GetId()}", ValidUpdateBody("2026-08-01"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(countBefore, expense.GetOccurrences().Count);
    }

    [Fact]
    public async Task Put_ExpenseWithPaidOccurrence_Returns200AndPaymentDataUnaffected()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        var occurrence = expense.GetOccurrences().Single();
        var paidAmount = new Money(1500m);
        var paymentDate = new CalendarDate(new DateOnly(2026, 8, 5));
        expense.MarkOccurrenceAsPaid(occurrence.GetId(), paidAmount, paymentDate);
        var client = CreateClient(expense);

        var response = await client.PutAsJsonAsync(
            $"{Endpoint}/{expense.GetId()}",
            ValidUpdateBody("2026-08-01", name: "Aluguel renomeado"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(paidAmount.GetValue(), occurrence.GetPaidAmount()!.GetValue());
        Assert.Equal(paymentDate.GetValue(), occurrence.GetPaymentDate()!.GetValue());
    }

    [Fact]
    public async Task Put_StatusActiveToPaused_Returns200AndDoesNotTouchOccurrences()
    {
        var expense = CreateExpense(status: RecurringExpenseStatusType.Active, currentReferencePeriod: new ReferencePeriod(2026, 8));
        var countBefore = expense.GetOccurrences().Count;
        var client = CreateClient(expense);

        var response = await client.PutAsJsonAsync($"{Endpoint}/{expense.GetId()}", ValidUpdateBody("2026-08-01", status: "Paused"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("Paused", body!.RootElement.GetProperty("data").GetProperty("status").GetString());
        Assert.Equal(countBefore, expense.GetOccurrences().Count);
    }

    [Fact]
    public async Task Put_StatusPausedToActiveStartDateBegunNoOccurrenceYet_GeneratesExactlyOnePendingOccurrence()
    {
        // currentReferencePeriod (2020-01) is well before startDate's own competência (2026-01), so
        // construction generates zero occurrences (EC06) regardless of the real "today" the running
        // API uses for Reactivate — startDate (2026-01) stays safely in the past relative to it.
        var expense = CreateExpense(
            status: RecurringExpenseStatusType.Paused,
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 1, 31),
            currentReferencePeriod: new ReferencePeriod(2020, 1));
        Assert.Empty(expense.GetOccurrences());
        var client = CreateClient(expense);

        var response = await client.PutAsJsonAsync(
            $"{Endpoint}/{expense.GetId()}",
            ValidUpdateBody("2026-01-01", endDate: "2026-01-31", status: "Active"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var occurrence = Assert.Single(expense.GetOccurrences());
        Assert.Equal(OccurrenceStatusType.Pending, occurrence.GetStatus().GetValue());

        var secondResponse = await client.PutAsJsonAsync(
            $"{Endpoint}/{expense.GetId()}",
            ValidUpdateBody("2026-01-01", endDate: "2026-01-31", status: "Active"));

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Single(expense.GetOccurrences());
    }

    [Fact]
    public async Task GetById_ExistingId_Returns200WithEditableFieldsAndNoOccurrences()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        var client = CreateClient(expense);

        var response = await client.GetAsync($"{Endpoint}/{expense.GetId()}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var data = body!.RootElement.GetProperty("data");

        Assert.Equal(expense.GetId(), data.GetProperty("id").GetGuid());
        Assert.Equal("Aluguel", data.GetProperty("name").GetString());
        Assert.Equal("Housing", data.GetProperty("category").GetString());
        Assert.Equal("Monthly", data.GetProperty("frequency").GetString());
        Assert.False(data.TryGetProperty("occurrences", out _));
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var client = CreateClient();

        var response = await client.GetAsync($"{Endpoint}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("data").ValueKind);
    }

    [Fact]
    public async Task Put_BusinessRuleViolation_Returns400WithOneFieldError()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        var client = CreateClient(expense);

        var response = await client.PutAsJsonAsync(
            $"{Endpoint}/{expense.GetId()}",
            ValidUpdateBody("2026-08-01", monthlyAmount: -10m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("data").ValueKind);

        var errors = root.GetProperty("errors");
        Assert.Equal(1, errors.GetArrayLength());
        Assert.Equal("monthlyAmount", errors[0].GetProperty("field").GetString());
    }

    [Fact]
    public async Task Put_MissingRequiredNameField_Returns400ShapePresenceNeverValidationProblemDetails()
    {
        var expense = CreateExpense(currentReferencePeriod: new ReferencePeriod(2026, 8));
        var client = CreateClient(expense);

        var payload = new
        {
            category = "Housing",
            monthlyAmount = 1500.00m,
            dueDay = 10,
            startDate = "2026-08-01",
            status = "Active"
        };

        var response = await client.PutAsJsonAsync($"{Endpoint}/{expense.GetId()}", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.TryGetProperty("title", out _));
        var errors = root.GetProperty("errors");
        Assert.Contains(errors.EnumerateArray(), error =>
            error.GetProperty("field").GetString() == "name" &&
            error.GetProperty("message").GetString() == "Nome é obrigatório.");
    }

    [Fact]
    public async Task Put_NonExistentId_Returns404AndCreatesNothing()
    {
        var client = CreateClient();

        var response = await client.PutAsJsonAsync($"{Endpoint}/{Guid.NewGuid()}", ValidUpdateBody("2026-08-01"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("data").ValueKind);
    }

    [Fact]
    public async Task Put_ExtendingEndDateToLaterCompetencia_Returns200WithUpdatedEndDateAndAddsNewOccurrences()
    {
        var startDate = FirstDayOfMonthOffset(-6);
        var expense = CreateExpense(
            startDate: startDate,
            endDate: LastDayOfMonthOffset(3),
            currentReferencePeriod: ReferencePeriod.FromDate(startDate)); // 10 occurrences: M-6..M+3
        var client = CreateClient(expense);

        var newEndDate = LastDayOfMonthOffset(5); // stays within the 1-year cap (startDate + 1yr == first day of M+6)
        var response = await client.PutAsJsonAsync(
            $"{Endpoint}/{expense.GetId()}",
            ValidUpdateBody(startDate.ToString("yyyy-MM-dd"), endDate: newEndDate.ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var data = body!.RootElement.GetProperty("data");
        Assert.Equal(newEndDate.ToString("yyyy-MM-dd"), data.GetProperty("endDate").GetString());

        Assert.Equal(12, expense.GetOccurrences().Count); // M-6..M+5
    }

    [Fact]
    public async Task Put_ExtendingEndDatePastCompetenciaAlreadyInThePast_Returns200AndFillsRetroactiveGapIncludingPastCompetencias()
    {
        var startDate = FirstDayOfMonthOffset(-6);
        var oldEndDate = LastDayOfMonthOffset(-4);
        var expense = CreateExpense(
            startDate: startDate,
            endDate: oldEndDate,
            currentReferencePeriod: ReferencePeriod.FromDate(startDate)); // 3 occurrences: M-6, M-5, M-4
        Assert.Equal(3, expense.GetOccurrences().Count);
        var client = CreateClient(expense);

        var newEndDate = LastDayOfMonthOffset(2);
        var response = await client.PutAsJsonAsync(
            $"{Endpoint}/{expense.GetId()}",
            ValidUpdateBody(startDate.ToString("yyyy-MM-dd"), endDate: newEndDate.ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Gap-filled: M-3, M-2, M-1 (already in the past relative to "now"), M0, M+1, M+2.
        Assert.Equal(9, expense.GetOccurrences().Count);
    }

    [Fact]
    public async Task Put_EndDateBeyondOneYearTetoFromCurrentStartDate_Returns400AndPersistsNothing()
    {
        var startDate = FirstDayOfMonthOffset(0);
        var originalEndDate = LastDayOfMonthOffset(0);
        var expense = CreateExpense(
            startDate: startDate,
            endDate: originalEndDate,
            currentReferencePeriod: ReferencePeriod.FromDate(startDate));
        var client = CreateClient(expense);

        var beyondTeto = startDate.AddYears(1).AddDays(1);
        var response = await client.PutAsJsonAsync(
            $"{Endpoint}/{expense.GetId()}",
            ValidUpdateBody(startDate.ToString("yyyy-MM-dd"), endDate: beyondTeto.ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var errors = body!.RootElement.GetProperty("errors");
        Assert.Equal(1, errors.GetArrayLength());
        Assert.Equal("endDate", errors[0].GetProperty("field").GetString());

        Assert.Equal(originalEndDate, expense.GetEndDate().GetValue());
        Assert.Single(expense.GetOccurrences());
    }

    [Fact]
    public async Task Put_NewStartDateNoLongerBeforeCurrentEndDate_Returns400AndPersistsNothing()
    {
        var startDate = FirstDayOfMonthOffset(0);
        var originalEndDate = LastDayOfMonthOffset(0);
        var expense = CreateExpense(
            startDate: startDate,
            endDate: originalEndDate,
            currentReferencePeriod: ReferencePeriod.FromDate(startDate));
        var client = CreateClient(expense);

        var newStartDate = FirstDayOfMonthOffset(1);
        var response = await client.PutAsJsonAsync(
            $"{Endpoint}/{expense.GetId()}",
            ValidUpdateBody(newStartDate.ToString("yyyy-MM-dd"), endDate: originalEndDate.ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var errors = body!.RootElement.GetProperty("errors");
        Assert.Equal(1, errors.GetArrayLength());
        Assert.Equal("startDate", errors[0].GetProperty("field").GetString());

        Assert.Equal(startDate, expense.GetStartDate().GetValue());
        Assert.Single(expense.GetOccurrences());
    }

    [Fact]
    public async Task Put_ReducingEndDateWithNoPaidOccurrenceInRemovedRange_Returns200AndExcludesTrailingOccurrences()
    {
        var startDate = FirstDayOfMonthOffset(-6);
        var expense = CreateExpense(
            startDate: startDate,
            endDate: LastDayOfMonthOffset(3),
            currentReferencePeriod: ReferencePeriod.FromDate(startDate)); // 10 occurrences: M-6..M+3
        var client = CreateClient(expense);

        var reducedEndDate = LastDayOfMonthOffset(1);
        var response = await client.PutAsJsonAsync(
            $"{Endpoint}/{expense.GetId()}",
            ValidUpdateBody(startDate.ToString("yyyy-MM-dd"), endDate: reducedEndDate.ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(8, expense.GetOccurrences().Count); // M+2, M+3 excluded
    }

    [Fact]
    public async Task Put_ReducingEndDateExcludingAPaidOccurrence_Returns400WithBlockMessageAndPersistsNothing()
    {
        var startDate = FirstDayOfMonthOffset(-6);
        var expense = CreateExpense(
            startDate: startDate,
            endDate: LastDayOfMonthOffset(3),
            currentReferencePeriod: ReferencePeriod.FromDate(startDate)); // 10 occurrences: M-6..M+3
        var occurrenceInRemovedRange = expense.GetOccurrencesForPeriod(new ReferencePeriod(
            FirstDayOfMonthOffset(2).Year, FirstDayOfMonthOffset(2).Month)).Single();
        expense.MarkOccurrenceAsPaid(occurrenceInRemovedRange.GetId(), new Money(1500m), new CalendarDate(FirstDayOfMonthOffset(2)));
        var client = CreateClient(expense);

        var reducedEndDate = LastDayOfMonthOffset(1);
        var response = await client.PutAsJsonAsync(
            $"{Endpoint}/{expense.GetId()}",
            ValidUpdateBody(startDate.ToString("yyyy-MM-dd"), endDate: reducedEndDate.ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var errors = body!.RootElement.GetProperty("errors");
        Assert.Equal(1, errors.GetArrayLength());
        Assert.Equal("endDate", errors[0].GetProperty("field").GetString());

        Assert.Equal(LastDayOfMonthOffset(3), expense.GetEndDate().GetValue());
        Assert.Equal(10, expense.GetOccurrences().Count);
    }

    [Fact]
    public async Task Put_ReducingEndDateToCompetenciaBeforeCurrentMonth_Returns400WithPastMessageEvenWithNoPaidOccurrence()
    {
        var startDate = FirstDayOfMonthOffset(-6);
        var expense = CreateExpense(
            startDate: startDate,
            endDate: LastDayOfMonthOffset(3),
            currentReferencePeriod: ReferencePeriod.FromDate(startDate)); // 10 occurrences: M-6..M+3
        var client = CreateClient(expense);

        var pastEndDate = LastDayOfMonthOffset(-1);
        var response = await client.PutAsJsonAsync(
            $"{Endpoint}/{expense.GetId()}",
            ValidUpdateBody(startDate.ToString("yyyy-MM-dd"), endDate: pastEndDate.ToString("yyyy-MM-dd")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var errors = body!.RootElement.GetProperty("errors");
        Assert.Equal(1, errors.GetArrayLength());
        Assert.Equal("endDate", errors[0].GetProperty("field").GetString());
        Assert.Equal("A data de fim não pode estar no passado.", errors[0].GetProperty("message").GetString());

        Assert.Equal(LastDayOfMonthOffset(3), expense.GetEndDate().GetValue());
        Assert.Equal(10, expense.GetOccurrences().Count);
    }
}

public sealed class RecurringExpenseSeededWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly IServiceProvider InMemoryProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

    private readonly RecurringExpense[] _seed;

    public RecurringExpenseSeededWebApplicationFactory(RecurringExpense[] seed)
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
            services.AddSingleton<IRepositoryManager>(new EditingSeededRepositoryManager(_seed));
        });
    }
}

internal sealed class EditingSeededRepositoryManager : IRepositoryManager
{
    public EditingSeededRepositoryManager(IEnumerable<RecurringExpense> seed)
    {
        RecurringExpenseRepository = new EditingSeededRecurringExpenseRepository(seed);
    }

    public IRecurringExpenseRepository RecurringExpenseRepository { get; }
}

internal sealed class EditingSeededRecurringExpenseRepository : IRecurringExpenseRepository
{
    private readonly List<RecurringExpense> _store;

    public EditingSeededRecurringExpenseRepository(IEnumerable<RecurringExpense> seed)
    {
        _store = seed.ToList();
    }

    public Task AddAsync(RecurringExpense recurringExpense) => throw new NotImplementedException();

    public Task<RecurringExpense?> GetByIdAsync(Guid id) =>
        Task.FromResult(_store.FirstOrDefault(expense => expense.GetId() == id));

    public Task<IReadOnlyCollection<RecurringExpense>> GetActiveAsync() => throw new NotImplementedException();

    public Task<IReadOnlyCollection<RecurringExpense>> GetByReferencePeriodAsync(ReferencePeriod referencePeriod) =>
        throw new NotImplementedException();

    public Task<RecurringExpense?> GetByOccurrenceIdAsync(Guid occurrenceId) => throw new NotImplementedException();

    public Task UpdateAsync(RecurringExpense recurringExpense) => Task.CompletedTask;
}
