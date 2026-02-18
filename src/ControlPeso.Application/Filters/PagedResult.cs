namespace ControlPeso.Application.Filters;

/// <summary>
/// Resultado paginado genérico para listas.
/// </summary>
/// <typeparam name="T">Tipo de elemento en la lista.</typeparam>
public sealed record PagedResult<T>
{
    /// <summary>
    /// Items de la página actual.
    /// </summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>
    /// Número de página actual (1-based).
    /// </summary>
    public required int Page { get; init; }

    /// <summary>
    /// Tamaño de página (cantidad de items por página).
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// Total de items en todas las páginas.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Total de páginas calculado (TotalCount / PageSize, redondeado arriba).
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Indica si hay página anterior.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Indica si hay página siguiente.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;
}
