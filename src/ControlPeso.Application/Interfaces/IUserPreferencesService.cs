namespace ControlPeso.Application.Interfaces;

/// <summary>
/// Servicio para gestionar las preferencias de usuario
/// </summary>
public interface IUserPreferencesService
{
    /// <summary>
    /// Obtiene las preferencias de un usuario por su ID
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="ct">Token de cancelación</param>
    /// <returns>Preferencias del usuario o null si no existen</returns>
    Task<bool> GetDarkModePreferenceAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Actualiza la preferencia de tema oscuro del usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="isDarkMode">True para modo oscuro, False para modo claro</param>
    /// <param name="ct">Token de cancelación</param>
    Task UpdateDarkModeAsync(Guid userId, bool isDarkMode, CancellationToken ct = default);

    /// <summary>
    /// Crea las preferencias por defecto para un nuevo usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="ct">Token de cancelación</param>
    Task CreateDefaultPreferencesAsync(Guid userId, CancellationToken ct = default);
}
