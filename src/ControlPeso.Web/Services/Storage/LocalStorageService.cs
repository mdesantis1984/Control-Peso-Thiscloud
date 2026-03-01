using Microsoft.JSInterop;

namespace ControlPeso.Web.Services.Storage;

/// <summary>
/// Implementación de IStorageService para localStorage del navegador.
/// Usa JavaScript Interop para acceder al API nativo de localStorage.
/// Los datos persisten incluso después de cerrar el navegador.
/// </summary>
public sealed class LocalStorageService : IStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(
        IJSRuntime jsRuntime,
        ILogger<LocalStorageService> logger)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        ArgumentNullException.ThrowIfNull(logger);

        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<string?> GetItemAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var value = await _jsRuntime.InvokeAsync<string?>(
                "storageInterop.localStorage.getItem",
                ct,
                key);

            _logger.LogDebug(
                "LocalStorageService: GetItem - Key: {Key}, HasValue: {HasValue}",
                key, value != null);

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "LocalStorageService: Error getting item - Key: {Key}",
                key);
            return null;
        }
    }

    public async Task SetItemAsync(string key, string value, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var success = await _jsRuntime.InvokeAsync<bool>(
                "storageInterop.localStorage.setItem",
                ct,
                key,
                value);

            if (success)
            {
                _logger.LogDebug(
                    "LocalStorageService: SetItem successful - Key: {Key}",
                    key);
            }
            else
            {
                _logger.LogWarning(
                    "LocalStorageService: SetItem failed - Key: {Key}",
                    key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "LocalStorageService: Error setting item - Key: {Key}",
                key);
            throw;
        }
    }

    public async Task RemoveItemAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var success = await _jsRuntime.InvokeAsync<bool>(
                "storageInterop.localStorage.removeItem",
                ct,
                key);

            if (success)
            {
                _logger.LogDebug(
                    "LocalStorageService: RemoveItem successful - Key: {Key}",
                    key);
            }
            else
            {
                _logger.LogWarning(
                    "LocalStorageService: RemoveItem failed - Key: {Key}",
                    key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "LocalStorageService: Error removing item - Key: {Key}",
                key);
            throw;
        }
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        try
        {
            var success = await _jsRuntime.InvokeAsync<bool>(
                "storageInterop.localStorage.clear",
                ct);

            if (success)
            {
                _logger.LogInformation("LocalStorageService: Clear successful");
            }
            else
            {
                _logger.LogWarning("LocalStorageService: Clear failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LocalStorageService: Error clearing storage");
            throw;
        }
    }

    public async Task<bool> ContainsKeyAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var exists = await _jsRuntime.InvokeAsync<bool>(
                "storageInterop.localStorage.containsKey",
                ct,
                key);

            _logger.LogDebug(
                "LocalStorageService: ContainsKey - Key: {Key}, Exists: {Exists}",
                key, exists);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "LocalStorageService: Error checking if key exists - Key: {Key}",
                key);
            return false;
        }
    }

    public async Task<int> LengthAsync(CancellationToken ct = default)
    {
        try
        {
            var length = await _jsRuntime.InvokeAsync<int>(
                "storageInterop.localStorage.length",
                ct);

            _logger.LogDebug(
                "LocalStorageService: Length - Count: {Count}",
                length);

            return length;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LocalStorageService: Error getting length");
            return 0;
        }
    }

    public async Task<string?> KeyAsync(int index, CancellationToken ct = default)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Index cannot be negative");

        try
        {
            var key = await _jsRuntime.InvokeAsync<string?>(
                "storageInterop.localStorage.key",
                ct,
                index);

            _logger.LogDebug(
                "LocalStorageService: Key - Index: {Index}, Key: {Key}",
                index, key ?? "(null)");

            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "LocalStorageService: Error getting key at index - Index: {Index}",
                index);
            return null;
        }
    }
}
