using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Options;

namespace ControlPeso.Web.Services;

/// <summary>
/// Telegram notification service implementation
/// Sends critical errors to configured Telegram chat
/// Implements throttling and deduplication to prevent notification floods
/// </summary>
internal sealed class TelegramNotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramNotificationService> _logger;

    // Throttling state - static para compartir entre todas las instancias
    private static readonly ConcurrentDictionary<string, DateTime> _recentExceptions = new();
    private static int _messageCount = 0;
    private static DateTime _lastReset = DateTime.UtcNow;
    private const int MaxMessagesPerMinute = 5; // Máximo 5 notificaciones por minuto

    public TelegramNotificationService(
        HttpClient httpClient,
        IOptions<TelegramOptions> options,
        ILogger<TelegramNotificationService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendCriticalErrorAsync(
        string errorMessage,
        string traceId,
        Exception? exception = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);

        if (!_options.Enabled)
        {
            _logger.LogWarning("Telegram notifications are disabled - skipping notification");
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            _logger.LogError("Telegram BotToken is not configured - cannot send notification");
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.ChatId))
        {
            _logger.LogError("Telegram ChatId is not configured - cannot send notification");
            return;
        }

        // THROTTLING: Verificar si debemos enviar esta notificación
        if (!ShouldSendNotification(errorMessage, exception))
        {
            return; // Skip - throttled o duplicada
        }

        _logger.LogInformation(
            "Sending critical error notification to Telegram - TraceId: {TraceId}",
            traceId);

        try
        {
            var message = BuildErrorMessage(errorMessage, traceId, exception);
            await SendTelegramMessageAsync(message, ct);

            _logger.LogInformation(
                "Critical error notification sent successfully to Telegram - TraceId: {TraceId}",
                traceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send Telegram notification - TraceId: {TraceId}",
                traceId);
            // No throw - notification failure should not crash the app
        }
    }

    /// <summary>
    /// Determina si se debe enviar una notificación basado en throttling y deduplicación.
    /// Implementa:
    /// 1. Circuit breaker: máximo 5 mensajes por minuto
    /// 2. Deduplicación: no enviar la misma excepción dentro de 60 segundos
    /// </summary>
    private bool ShouldSendNotification(string errorMessage, Exception? exception)
    {
        var now = DateTime.UtcNow;

        // Reset counter cada minuto
        if ((now - _lastReset).TotalMinutes >= 1)
        {
            _messageCount = 0;
            _lastReset = now;
            _recentExceptions.Clear();
            _logger.LogInformation("Telegram notification throttling: Counter reset");
        }

        // Circuit breaker: detener después de MaxMessagesPerMinute
        if (_messageCount >= MaxMessagesPerMinute)
        {
            _logger.LogWarning(
                "Telegram notifications throttled - circuit breaker active (max {Max}/min reached)",
                MaxMessagesPerMinute);
            return false;
        }

        // Deduplicación: crear clave única para esta excepción
        var exceptionType = exception?.GetType().Name ?? "Unknown";
        var messagePart = errorMessage.Length > 50 
            ? errorMessage.Substring(0, 50) 
            : errorMessage;
        var key = $"{exceptionType}:{messagePart}";

        // Verificar si ya enviamos esta excepción recientemente
        if (_recentExceptions.TryGetValue(key, out var lastSent))
        {
            if ((now - lastSent).TotalSeconds < 60)
            {
                _logger.LogInformation(
                    "Telegram notification skipped - duplicate exception within 60 seconds: {ExceptionType}",
                    exceptionType);
                return false;
            }
        }

        // Permitir notificación - actualizar tracking
        _recentExceptions[key] = now;
        _messageCount++;

        _logger.LogInformation(
            "Telegram notification allowed - Count: {Count}/{Max}, ExceptionType: {ExceptionType}",
            _messageCount, MaxMessagesPerMinute, exceptionType);

        return true;
    }

    private string BuildErrorMessage(string errorMessage, string traceId, Exception? exception)
    {
        var sb = new StringBuilder();

        sb.AppendLine("🚨 <b>ERROR CRÍTICO - Control Peso Thiscloud</b>");
        sb.AppendLine();
        sb.AppendLine($"<b>🔍 Trace ID:</b> <code>{traceId}</code>");
        sb.AppendLine($"<b>⏰ Timestamp:</b> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"<b>🌍 Ambiente:</b> {_options.Environment}");
        sb.AppendLine();
        sb.AppendLine($"<b>📝 Mensaje:</b>");
        sb.AppendLine($"<pre>{EscapeHtml(errorMessage)}</pre>");

        if (exception is not null)
        {
            sb.AppendLine();
            sb.AppendLine($"<b>❌ Excepción:</b> <code>{EscapeHtml(exception.GetType().Name)}</code>");
            
            var exMessage = exception.Message;
            if (exMessage.Length > 500)
            {
                exMessage = exMessage.Substring(0, 497) + "...";
            }
            sb.AppendLine($"<pre>{EscapeHtml(exMessage)}</pre>");

            if (exception.InnerException is not null)
            {
                sb.AppendLine();
                sb.AppendLine($"<b>⚠️ Inner Exception:</b>");
                var innerMessage = exception.InnerException.Message;
                if (innerMessage.Length > 300)
                {
                    innerMessage = innerMessage.Substring(0, 297) + "...";
                }
                sb.AppendLine($"<pre>{EscapeHtml(innerMessage)}</pre>");
            }
        }

        sb.AppendLine();
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine("🔗 <a href=\"https://controlpeso.thiscloud.com.ar/logs\">Ver Logs Completos</a>");

        return sb.ToString();
    }

    private async Task SendTelegramMessageAsync(string message, CancellationToken ct)
    {
        var url = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";

        var payload = new
        {
            chat_id = _options.ChatId,
            text = message,
            parse_mode = "HTML",
            disable_web_page_preview = true
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Telegram API returned error - StatusCode: {StatusCode}, Response: {Response}",
                response.StatusCode, errorContent);

            response.EnsureSuccessStatusCode(); // Throw to trigger catch block
        }
    }

    private static string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}

/// <summary>
/// Configuration options for Telegram notifications
/// </summary>
public sealed class TelegramOptions
{
    public const string ConfigSection = "Telegram";

    /// <summary>
    /// Whether Telegram notifications are enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Telegram bot token (from BotFather)
    /// </summary>
    public string? BotToken { get; set; }

    /// <summary>
    /// Telegram chat ID to send notifications to
    /// </summary>
    public string? ChatId { get; set; }

    /// <summary>
    /// Current environment name (Development, Production, etc.)
    /// </summary>
    public string Environment { get; set; } = "Unknown";
}
