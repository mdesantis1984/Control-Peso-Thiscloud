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
    /// Cada test obtiene una BD SQLite in-memory aislada con conexión única.
    /// </summary>
    /// <param name="databaseName">Nombre único de la BD (usar nombre del test para aislamiento).</param>
    /// <returns>DbContext configurado para in-memory testing.</returns>
    public static ControlPesoDbContext Create(string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        // Crear conexión SQLite in-memory (Mode=Memory;Cache=Shared permite múltiples accesos)
        var connectionString = $"Data Source={databaseName};Mode=Memory;Cache=Shared";
        var connection = new SqliteConnection(connectionString);
        connection.Open(); // Mantener abierta para que la BD in-memory persista

        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseSqlite(connection) // Usar la misma conexión abierta
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .Options;

        var context = new ControlPesoDbContext(options);

        // Asegurar que la BD in-memory está creada con el schema
        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>
    /// Crea un DbContext y lo puebla con datos de seed para testing.
    /// Usa DbSeederFactory para crear el seeder sin acceder a la clase internal.
    /// </summary>
    /// <param name="databaseName">Nombre único de la BD.</param>
    /// <returns>DbContext con datos de seed cargados.</returns>
    public static async Task<ControlPesoDbContext> CreateWithSeedDataAsync(string databaseName)
    {
        var context = Create(databaseName);

        // Usar DbSeederFactory público para crear el seeder
        var seeder = DbSeederFactory.Create(context, logger: null);

        await seeder.SeedAsync();

        return context;
    }
}
