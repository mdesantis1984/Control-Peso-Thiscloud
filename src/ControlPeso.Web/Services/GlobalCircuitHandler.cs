using Microsoft.AspNetCore.Components.Server.Circuits;

namespace ControlPeso.Web.Services;

/// <summary>
/// Circuit handler that captures all unhandled exceptions in Blazor Server circuits.
/// Logs circuit lifecycle events.
/// </summary>
public sealed class GlobalCircuitHandler : CircuitHandler
{
    private readonly ILogger<GlobalCircuitHandler> _logger;

    public GlobalCircuitHandler(ILogger<GlobalCircuitHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

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
}
