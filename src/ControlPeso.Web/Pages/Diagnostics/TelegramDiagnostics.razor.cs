using ControlPeso.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;

namespace ControlPeso.Web.Pages.Diagnostics;

public partial class TelegramDiagnostics
{
    [Inject] private IOptions<TelegramOptions> TelegramConfig { get; set; } = null!;
    [Inject] private INotificationService NotificationService { get; set; } = null!;
    [Inject] private ILogger<TelegramDiagnostics> Logger { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private TelegramOptions? _config;
    private bool _isSending;
    private string _testResult = string.Empty;
    private Severity _testResultSeverity = Severity.Normal;

    protected override void OnInitialized()
    {
        _config = TelegramConfig.Value;
        Logger.LogInformation("TelegramDiagnostics: Initialized - Enabled: {Enabled}", _config?.Enabled);
    }

    private bool IsBotTokenConfigured()
    {
        return _config != null &&
               !string.IsNullOrWhiteSpace(_config.BotToken) &&
               !_config.BotToken.Contains("YOUR_TELEGRAM_BOT_TOKEN_HERE", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsChatIdConfigured()
    {
        return _config != null &&
               !string.IsNullOrWhiteSpace(_config.ChatId) &&
               !_config.ChatId.Contains("YOUR_TELEGRAM_CHAT_ID_HERE", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsFullyConfigured()
    {
        return _config != null &&
               _config.Enabled &&
               IsBotTokenConfigured() &&
               IsChatIdConfigured();
    }

    private string GetMaskedToken()
    {
        if (_config?.BotToken == null || _config.BotToken.Length < 15)
        {
            return "N/A";
        }

        // Mostrar primeros 10 caracteres + "..."
        return $"{_config.BotToken.Substring(0, 10)}...";
    }

    private async Task SendTestNotification()
    {
        _isSending = true;
        _testResult = string.Empty;
        StateHasChanged();

        try
        {
            Logger.LogInformation("TelegramDiagnostics: Sending test notification");

            var testMessage = "🧪 MENSAJE DE PRUEBA desde Panel de Diagnóstico";
            var traceId = $"TEST-{Guid.NewGuid():N}";

            await NotificationService.SendCriticalErrorAsync(
                testMessage,
                traceId,
                exception: null,
                ct: CancellationToken.None);

            _testResult = $"✅ Mensaje enviado correctamente!\n\n" +
                         $"🔍 Trace ID: {traceId}\n" +
                         $"📱 Revisa tu Telegram para confirmar que llegó el mensaje.";
            _testResultSeverity = Severity.Success;

            Logger.LogInformation("TelegramDiagnostics: Test notification sent successfully - TraceId: {TraceId}", traceId);
            Snackbar.Add("Mensaje de prueba enviado a Telegram", Severity.Success);
        }
        catch (Exception ex)
        {
            _testResult = $"❌ Error al enviar mensaje:\n\n{ex.Message}\n\n" +
                         $"Verifica:\n" +
                         $"• BotToken es correcto (de @BotFather)\n" +
                         $"• ChatId es correcto (de /getUpdates)\n" +
                         $"• Enviaste /start al bot antes de obtener Chat ID";
            _testResultSeverity = Severity.Error;

            Logger.LogError(ex, "TelegramDiagnostics: Failed to send test notification");
            Snackbar.Add("Error al enviar mensaje de prueba", Severity.Error);
        }
        finally
        {
            _isSending = false;
            StateHasChanged();
        }
    }
}
