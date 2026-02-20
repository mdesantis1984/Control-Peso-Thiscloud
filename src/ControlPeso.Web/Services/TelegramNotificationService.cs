using System.Text;
using Microsoft.Extensions.Options;

namespace ControlPeso.Web.Services;

/// <summary>
/// Telegram notification service implementation
/// Sends critical errors to configured Telegram chat
/// </summary>
internal sealed class TelegramNotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramNotificationService> _logger;

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
