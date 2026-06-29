using GastroFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace GastroFlow.IntegrationTests.Infrastructure;

// Starts one PostgreSQL container for the entire test class.
// Applies all EF Core migrations once so every test works against a real schema.
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithDatabase("gastroflow_test")
        .WithUsername("test")
        .WithPassword("test123!")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var factory = new GastroFlowFactory(ConnectionString);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.StopAsync();
}
