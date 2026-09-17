using ContasEmDia.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace ContasEmDia.Infrastructure.Tests.Migrations;

// Dedicated (non-shared) PostgreSQL container: unlike PostgreSqlContainerFixture (used by every
// other Infrastructure.Tests suite), this test needs to control migration application order —
// migrate up to the migration immediately before AddRecurringExpenseEndDate, insert a
// pre-existing RecurringExpenses row via raw SQL (simulating data that predates this feature),
// then apply AddRecurringExpenseEndDate and verify its backfill (User Story 4, FR-011).
public sealed class AddRecurringExpenseEndDateMigrationTests : IAsyncLifetime
{
    private const string MigrationBeforeThisFeature = "20260907211702_AddOccurrencePaymentTracking";
    private const string ThisFeaturesMigration = "20260917173700_AddRecurringExpenseEndDate";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public async Task InitializeAsync() => await _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private ContasEmDiaDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ContasEmDiaDbContext>();
        optionsBuilder.UseNpgsql(_container.GetConnectionString());

        return new ContasEmDiaDbContext(optionsBuilder.Options);
    }

    [Fact]
    public async Task Migrate_PreExistingRecurringExpenseRow_BackfillsEndDateAsStartDatePlusOneYearWithoutInsertingOccurrences()
    {
        var recurringExpenseId = Guid.NewGuid();
        var startDate = new DateOnly(2026, 3, 15);

        await using (var context = CreateContext())
        {
            await context.GetInfrastructure().GetRequiredService<IMigrator>().MigrateAsync(MigrationBeforeThisFeature);
        }

        await using (var connection = new NpgsqlConnection(_container.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO "RecurringExpenses"
                    ("Id", "Name", "Category", "MonthlyAmount", "DueDay", "StartDate", "Frequency", "Status", "Note")
                VALUES
                    (@id, 'Aluguel', 0, 1500.00, 10, @startDate, 0, 0, NULL);
                """;
            command.Parameters.AddWithValue("id", recurringExpenseId);
            command.Parameters.AddWithValue("startDate", startDate);
            await command.ExecuteNonQueryAsync();
        }

        await using (var context = CreateContext())
        {
            await context.GetInfrastructure().GetRequiredService<IMigrator>().MigrateAsync(ThisFeaturesMigration);
        }

        await using var verifyContext = CreateContext();
        var endDate = await verifyContext.Database
            .SqlQuery<DateOnly>($"SELECT \"EndDate\" AS \"Value\" FROM \"RecurringExpenses\" WHERE \"Id\" = {recurringExpenseId}")
            .SingleAsync();
        var occurrenceCount = await verifyContext.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM \"Occurrences\" WHERE \"RecurringExpenseId\" = {recurringExpenseId}")
            .SingleAsync();

        Assert.Equal(startDate.AddYears(1), endDate);
        Assert.Equal(0, occurrenceCount);
    }
}
