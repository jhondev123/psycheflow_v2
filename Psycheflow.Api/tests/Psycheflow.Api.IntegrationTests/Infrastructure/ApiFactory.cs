using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.IntegrationTests.Infrastructure;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(ApiFactory))]

// Todos os testes compartilham um único Postgres e o banco é limpo antes de cada teste.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Psycheflow.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Sobe a API real em memória contra um PostgreSQL descartável (Testcontainers).
/// As migrations rodam na inicialização (inclusive as roles via HasData); cada teste começa com o banco limpo (Respawn).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Segunda-feira, 05/10/2026, 09:00 em São Paulo (12:00 UTC).</summary>
    public static readonly DateTimeOffset DefaultNow = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("psycheflow_tests")
        .WithUsername("psycheflow")
        .WithPassword("psycheflow")
        .Build();

    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"psycheflow-it-storage-{Guid.NewGuid():N}");

    private Respawner? _respawner;

    public TestClock Clock { get; } = new(DefaultNow);

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        // Força a inicialização do host (e, com ela, as migrations).
        _ = Server;

        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Table(PersistenceSetup.MigrationsHistoryTable), new Table("roles")],
        });
    }

    public async Task ResetAsync()
    {
        Clock.UtcNow = DefaultNow;

        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await _respawner!.ResetAsync(connection);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        if (Directory.Exists(_storagePath))
        {
            Directory.Delete(_storagePath, recursive: true);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:Key", "integration-tests-signing-key-0123456789abcdef");
        builder.UseSetting(PersistenceSetup.MigrateOnStartupKey, "true");
        builder.UseSetting("Storage:Path", _storagePath);
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Clock));
    }
}
