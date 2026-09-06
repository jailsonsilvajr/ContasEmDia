using ContasEmDia.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ContasEmDia.Infrastructure.Tests;

public sealed class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public ContasEmDiaDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ContasEmDiaDbContext>();
        optionsBuilder.UseNpgsql(_container.GetConnectionString());

        return new ContasEmDiaDbContext(optionsBuilder.Options);
    }
}

[CollectionDefinition(nameof(PostgreSqlCollection))]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlContainerFixture>;
