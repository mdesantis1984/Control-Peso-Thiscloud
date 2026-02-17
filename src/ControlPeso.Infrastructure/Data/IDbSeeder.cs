namespace ControlPeso.Infrastructure.Data;

/// <summary>
/// Interfaz para seeding de datos en la base de datos.
/// </summary>
public interface IDbSeeder
{
    /// <summary>
    /// Ejecuta el seeding de datos en la base de datos.
    /// Solo debe ejecutarse en Development.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SeedAsync(CancellationToken ct = default);
}
