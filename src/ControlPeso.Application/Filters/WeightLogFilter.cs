namespace ControlPeso.Application.Filters;

/// <summary>
/// Filtros para consultas de registros de peso.
/// </summary>
public sealed record WeightLogFilter
{
    /// <summary>
    /// ID del usuario (obligatorio).
    /// </summary>
    public required Guid UserId { get; init; }
    
    /// <summary>
    /// Rango de fechas opcional.
    /// </summary>
    public DateRange? DateRange { get; init; }
    
    /// <summary>
    /// Número de página (1-based, default: 1).
    /// </summary>
    public int Page { get; init; } = 1;
    
    /// <summary>
    /// Tamaño de página (default: 10).
    /// </summary>
    public int PageSize { get; init; } = 10;
    
    /// <summary>
    /// Campo de ordenamiento (default: "Date").
    /// Valores válidos: "Date", "Weight", "CreatedAt".
    /// </summary>
    public string SortBy { get; init; } = "Date";
    
    /// <summary>
    /// Orden descendente (default: true - más reciente primero).
    /// </summary>
    public bool Descending { get; init; } = true;
}
