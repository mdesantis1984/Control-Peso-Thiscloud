using ControlPeso.Domain.Enums;

namespace ControlPeso.Application.Filters;

/// <summary>
/// Filtros para consultas de usuarios (admin panel).
/// </summary>
public sealed record UserFilter
{
    /// <summary>
    /// Búsqueda por nombre o email (case-insensitive).
    /// </summary>
    public string? SearchTerm { get; init; }
    
    /// <summary>
    /// Filtro por rol específico.
    /// </summary>
    public UserRole? Role { get; init; }
    
    /// <summary>
    /// Filtro por estado específico.
    /// </summary>
    public UserStatus? Status { get; init; }
    
    /// <summary>
    /// Filtro por idioma.
    /// </summary>
    public string? Language { get; init; }
    
    /// <summary>
    /// Número de página (1-based, default: 1).
    /// </summary>
    public int Page { get; init; } = 1;
    
    /// <summary>
    /// Tamaño de página (default: 20).
    /// </summary>
    public int PageSize { get; init; } = 20;
    
    /// <summary>
    /// Campo de ordenamiento (default: "MemberSince").
    /// Valores válidos: "Name", "Email", "MemberSince", "Status".
    /// </summary>
    public string SortBy { get; init; } = "MemberSince";
    
    /// <summary>
    /// Orden descendente (default: true - más reciente primero).
    /// </summary>
    public bool Descending { get; init; } = true;
}
