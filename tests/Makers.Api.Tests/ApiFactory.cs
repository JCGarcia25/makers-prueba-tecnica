using Makers.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Makers.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"api-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Seed:Password", "Demo123456!");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<LoanDbContext>>();
            services.RemoveAll<LoanDbContext>();
            services.RemoveAll<IDatabaseProvider>();
            services.AddDbContext<LoanDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        DemoSeed.RunAsync(scope.ServiceProvider.GetRequiredService<LoanDbContext>(),
            scope.ServiceProvider.GetRequiredService<Makers.Application.IPasswordService>(), "Demo123456!")
            .GetAwaiter().GetResult();
        return host;
    }
}
