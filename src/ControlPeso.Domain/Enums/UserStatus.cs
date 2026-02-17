namespace ControlPeso.Domain.Enums;

/// <summary>
/// Estado del usuario en el sistema.
/// Mapea el campo INTEGER Status en la tabla Users.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// Usuario activo, puede usar la aplicación normalmente.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Usuario inactivo, cuenta deshabilitada temporalmente.
    /// </summary>
    Inactive = 1,

    /// <summary>
    /// Usuario pendiente de confirmación/verificación.
    /// </summary>
    Pending = 2
}
