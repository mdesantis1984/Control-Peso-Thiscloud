using ControlPeso.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ControlPeso.Infrastructure.Tests.Helpers;

/// <summary>
/// Factory para crear instancias de DbContext en memoria para tests de integración.
/// Usa SQLite in-memory en vez de EF Core InMemory provider para evitar conflictos con OnConfiguring.
/// </summary>
internal static class InMemoryDbContextFactory
{
    /// <summary>
    /// Crea un DbContext configurado con SQLite in-memory.
    /// Cada test obtiene una BD SQLite in-memory aislada y única.
    /// IMPORTANTE: La conexión DEBE mantenerse abierta durante toda la vida del test.
    /// </summary>
    /// <param name="databaseName">Nombre de la BD (para identificación, pero se usa :memory: único por test).</param>
    /// <returns>Tupla (Context, Connection) - ambos deben disponerse en el test.</returns>
    public static (ControlPesoDbContext Context, SqliteConnection Connection) Create(string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        // Usar SQLite :memory: puro (sin nombre ni cache compartido) para aislamiento TOTAL entre tests
        // Cada conexión obtiene su propia base de datos en memoria completamente aislada
        var connectionString = "Data Source=:memory:";
        var connection = new SqliteConnection(connectionString);
        connection.Open(); // Mantener abierta - al cerrar, la BD in-memory se destruye

        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseSqlite(connection) // Usar la misma conexión abierta
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .Options;

        var context = new ControlPesoDbContext(options);

        // Asegurar que la BD in-memory está creada con el schema
        context.Database.EnsureCreated();

        return (context, connection);
    }

    /// <summary>
    /// Crea un DbContext y lo puebla con datos de seed para testing.
    /// Usa DbSeederFactory para crear el seeder sin acceder a la clase internal.
    /// IMPORTANTE: La conexión DEBE mantenerse abierta durante toda la vida del test.
    /// </summary>
    /// <param name="databaseName">Nombre único de la BD.</param>
    /// <returns>Tupla (Context, Connection) con datos de seed cargados - ambos deben disponerse en el test.</returns>
    public static async Task<(ControlPesoDbContext Context, SqliteConnection Connection)> CreateWithSeedDataAsync(string databaseName)
    {
        var (context, connection) = Create(databaseName);

        // Usar DbSeederFactory público para crear el seeder
        var seeder = DbSeederFactory.Create(context, logger: null);

        await seeder.SeedAsync();

        return (context, connection);
    }
}
