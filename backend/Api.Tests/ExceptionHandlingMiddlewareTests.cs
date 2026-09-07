using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.Repositories;
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

    public Task<RecurringExpense?> GetByIdAsync(Guid id) => throw new NotImplementedException();

    public Task<IReadOnlyCollection<RecurringExpense>> GetActiveAsync() => throw new NotImplementedException();
}
