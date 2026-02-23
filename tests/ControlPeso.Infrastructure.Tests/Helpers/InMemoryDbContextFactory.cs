using ControlPeso.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ControlPeso.Infrastructure.Tests.Helpers;

/// <summary>
/// Factory para crear instancias de DbContext en memoria para tests de integración.
/// Cada test obtiene una BD in-memory aislada con nombre único.
/// </summary>
internal static class InMemoryDbContextFactory
{
    /// <summary>
    /// Crea un DbContext configurado con InMemory provider.
    /// </summary>
    /// <param name="databaseName">Nombre único de la BD (usar nombre del test para aislamiento).</param>
    /// <returns>DbContext configurado para in-memory testing.</returns>
    public static ControlPesoDbContext Create(string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var options = new DbContextOptionsBuilder<ControlPesoDbContext>()
            .UseInMemoryDatabase(databaseName)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .Options;

        var context = new ControlPesoDbContext(options);

        // Asegurar que la BD in-memory está creada
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
