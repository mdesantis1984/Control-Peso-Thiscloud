namespace ControlPeso.Application.DTOs;

/// <summary>
/// DTO con información del usuario proveniente de Google OAuth.
/// Mapea los claims del token de Google.
/// </summary>
public sealed record GoogleUserInfo
{
    /// <summary>
    /// Subject claim (sub) - Identificador único de Google.
    /// </summary>
    public required string GoogleId { get; init; }
    
    /// <summary>
    /// Nombre completo del usuario.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Email verificado del usuario.
    /// </summary>
    public required string Email { get; init; }
    
    /// <summary>
    /// URL del avatar/foto de perfil (opcional).
    /// </summary>
    public string? AvatarUrl { get; init; }
}
