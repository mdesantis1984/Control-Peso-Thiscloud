namespace ControlPeso.Domain.Enums;

/// <summary>
/// Rol del usuario en el sistema.
/// Mapea el campo INTEGER Role en la tabla Users.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Usuario estándar con permisos básicos.
    /// </summary>
    User = 0,

    /// <summary>
    /// Administrador con permisos completos.
    /// </summary>
    Administrator = 1
}
