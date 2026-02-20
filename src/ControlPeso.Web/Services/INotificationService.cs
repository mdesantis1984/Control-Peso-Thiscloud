namespace ControlPeso.Web.Services;

/// <summary>
/// Service for sending notifications to configured channels (Telegram, Email, etc.)
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a critical error notification to all configured channels
    /// </summary>
    /// <param name="errorMessage">Error message to send</param>
    /// <param name="traceId">Trace ID for debugging</param>
    /// <param name="exception">Exception details (optional)</param>
    /// <param name="ct">Cancellation token</param>
    Task SendCriticalErrorAsync(
        string errorMessage,
        string traceId,
        Exception? exception = null,
        CancellationToken ct = default);
}
