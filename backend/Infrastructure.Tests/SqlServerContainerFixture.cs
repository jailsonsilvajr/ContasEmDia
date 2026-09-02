using ContasEmDia.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace ContasEmDia.Infrastructure.Tests;

public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

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
        optionsBuilder.UseSqlServer(_container.GetConnectionString());

        return new ContasEmDiaDbContext(optionsBuilder.Options);
    }
}

[CollectionDefinition(nameof(SqlServerCollection))]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerContainerFixture>;
