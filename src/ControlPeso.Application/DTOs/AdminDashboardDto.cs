namespace ControlPeso.Application.DTOs;

/// <summary>
/// DTO con métricas del panel de administración.
/// </summary>
public sealed record AdminDashboardDto
{
    /// <summary>
    /// Total de usuarios registrados.
    /// </summary>
    public int TotalUsers { get; init; }
    
    /// <summary>
    /// Usuarios activos (Status = Active).
    /// </summary>
    public int ActiveUsers { get; init; }
    
    /// <summary>
    /// Usuarios pendientes (Status = Pending).
    /// </summary>
    public int PendingUsers { get; init; }
    
    /// <summary>
    /// Usuarios inactivos (Status = Inactive).
    /// </summary>
    public int InactiveUsers { get; init; }
    
    /// <summary>
    /// Total de registros de peso en el sistema.
    /// </summary>
    public int TotalWeightLogs { get; init; }
    
    /// <summary>
    /// Registros creados en los últimos 7 días.
    /// </summary>
    public int WeightLogsLastWeek { get; init; }
    
    /// <summary>
    /// Registros creados en los últimos 30 días.
    /// </summary>
    public int WeightLogsLastMonth { get; init; }
    
    /// <summary>
    /// Fecha del usuario más reciente.
    /// </summary>
    public DateTime? LatestUserRegistration { get; init; }
}
