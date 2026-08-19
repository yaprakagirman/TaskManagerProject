using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace TaskManager.API.IntegrationTests;

public sealed class TaskManagerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string JwtKey = "integration-test-key-at-least-32-characters-long";
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.4")
        .WithDatabase("taskmanager_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string TokenKey => JwtKey;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = "TaskManagerApi",
                ["Jwt:Audience"] = "TaskManagerClient"
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
