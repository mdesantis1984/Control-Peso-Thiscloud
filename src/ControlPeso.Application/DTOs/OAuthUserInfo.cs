namespace ControlPeso.Application.DTOs;

/// <summary>
/// DTO genérico con información del usuario proveniente de OAuth providers (Google, LinkedIn, etc.).
/// </summary>
public sealed record OAuthUserInfo
{
    /// <summary>
    /// Provider que autenticó al usuario (ej: "Google", "LinkedIn").
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// Identificador único del usuario en el provider externo (sub claim).
    /// </summary>
    public required string ExternalId { get; init; }

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
