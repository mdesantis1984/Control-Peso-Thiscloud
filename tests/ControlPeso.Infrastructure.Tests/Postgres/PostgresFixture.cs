using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace ControlPeso.Infrastructure.Tests.Postgres;

/// <summary>
/// Starts one <c>postgres:15</c> container per test class and hands out a freshly migrated
/// database per test. When Docker is not available nothing is started and the tests marked
/// with <see cref="PostgresFactAttribute"/> are reported as skipped.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public async Task InitializeAsync()
    {
        if (!DockerAvailability.IsAvailable)
        {
            return;
        }

        _container = new PostgreSqlBuilder("postgres:15").Build();
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates an empty database, applies every EF Core migration to it and returns its connection string.
    /// </summary>
    public async Task<string> CreateMigratedDatabaseAsync()
    {
        if (_container is null)
        {
            throw new InvalidOperationException("PostgreSQL container is not running (Docker unavailable).");
        }

        var databaseName = $"controlpeso_{Guid.NewGuid():N}";

        await using (var admin = new NpgsqlConnection(_container.GetConnectionString()))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        var connectionString = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = databaseName,
            Pooling = false
        }.ConnectionString;

        await using var context = CreateContext(connectionString);
        await context.Database.MigrateAsync();

        return connectionString;
    }

    public static ControlPesoDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<ControlPesoDbContext>().UseNpgsql(connectionString).Options);
}

/// <summary>
/// <see cref="IDbContextFactory{TContext}"/> over a real PostgreSQL connection string.
/// </summary>
public sealed class PostgresDbContextFactory(string connectionString) : IDbContextFactory<DbContext>
{
    public DbContext CreateDbContext() => PostgresFixture.CreateContext(connectionString);
}

/// <summary>
/// Detects whether a Docker daemon endpoint is configured on this machine.
/// Set CONTROLPESO_SKIP_DOCKER_TESTS=1 to force the PostgreSQL tests to be skipped.
/// </summary>
internal static class DockerAvailability
{
    public static bool IsAvailable { get; } = Detect();

    private static bool Detect()
    {
        if (Environment.GetEnvironmentVariable("CONTROLPESO_SKIP_DOCKER_TESTS") == "1")
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST")))
        {
            return true;
        }

        return OperatingSystem.IsWindows()
            ? File.Exists(@"\\.\pipe\docker_engine") || Directory.Exists(@"\\.\pipe\")
            : File.Exists("/var/run/docker.sock");
    }
}

/// <summary>
/// A <see cref="FactAttribute"/> that is skipped (not failed) when Docker is unavailable.
/// </summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (!DockerAvailability.IsAvailable)
        {
            Skip = "Docker is not available: PostgreSQL integration tests skipped.";
        }
    }
}
