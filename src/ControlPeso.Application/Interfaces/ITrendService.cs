namespace ControlPeso.Application.Interfaces;

/// <summary>
/// Service interface for trend analysis and projections
/// </summary>
public interface ITrendService
{
    /// <summary>
    /// Get trend analysis for a user within a date range
    /// </summary>
    Task<TrendAnalysisDto> GetTrendAnalysisAsync(Guid userId, DateRange range, CancellationToken ct = default);

    /// <summary>
    /// Get weight projection based on current trend
    /// </summary>
    Task<WeightProjectionDto> GetProjectionAsync(Guid userId, CancellationToken ct = default);
}
