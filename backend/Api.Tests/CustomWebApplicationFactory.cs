using ContasEmDia.Infrastructure.Contexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ContasEmDia.Api.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
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
        });
    }
}
