using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ContasEmDia.Infrastructure.Contexts;

/// <summary>
/// Used exclusively by the `dotnet ef` design-time tooling (e.g. to scaffold migrations)
/// when no host project registers <see cref="ContasEmDiaDbContext"/> via dependency injection yet.
/// Never used at application runtime; the connection string below is a design-time placeholder,
/// not a real credential.
/// </summary>
public sealed class ContasEmDiaDbContextFactory : IDesignTimeDbContextFactory<ContasEmDiaDbContext>
{
    public ContasEmDiaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ContasEmDiaDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=ContasEmDia;Trusted_Connection=True;TrustServerCertificate=True;");

        return new ContasEmDiaDbContext(optionsBuilder.Options);
    }
}
