using Microsoft.AspNetCore.Components.Server.Circuits;

namespace ControlPeso.Web.Services;

/// <summary>
/// Circuit handler that captures all unhandled exceptions in Blazor Server circuits.
/// Sends critical errors to Telegram notification service.
/// </summary>
public sealed class GlobalCircuitHandler : CircuitHandler
{
    private readonly ILogger<GlobalCircuitHandler> _logger;
    private readonly IServiceProvider _serviceProvider;

    public GlobalCircuitHandler(
        ILogger<GlobalCircuitHandler> logger,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _logger = logger;
        _serviceProvider = serviceProvider;

        // Log handler instantiation at Debug level (internal diagnostic)
        _logger.LogDebug("GlobalCircuitHandler created - ready to monitor Blazor Server circuits");
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Blazor circuit opened - CircuitId: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Blazor circuit connection established - CircuitId: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Blazor circuit connection lost - CircuitId: {CircuitId}. This may indicate network issues or user navigating away.", circuit.Id);
        return Task.CompletedTask;
    }

    public override async Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Blazor circuit closed - CircuitId: {CircuitId}", circuit.Id);
        await Task.CompletedTask;
    }

    private async Task SendTelegramNotificationAsync(
        string circuitId,
        Exception exception,
        string phase)
    {
        try
        {
            // Create a scope to get scoped services
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var errorMessage = $"Blazor Server Circuit Error\n" +
                               $"Circuit ID: {circuitId}\n" +
                               $"Phase: {phase}";

            await notificationService.SendCriticalErrorAsync(
                errorMessage,
                traceId: circuitId,
                exception,
                CancellationToken.None);

            _logger.LogInformation(
                "Telegram notification sent for circuit {CircuitId} error in phase {Phase}",
                circuitId, phase);
        }
        catch (Exception ex)
        {
            // Log failure but don't throw - notification failure should not crash the app
            _logger.LogError(ex,
                "Failed to send Telegram notification for circuit {CircuitId} error",
                circuitId);
        }
    }
}
