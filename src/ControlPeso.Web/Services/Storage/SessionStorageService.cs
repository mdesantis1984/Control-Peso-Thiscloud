using Microsoft.JSInterop;

namespace ControlPeso.Web.Services.Storage;

/// <summary>
/// Implementación de IStorageService para sessionStorage del navegador.
/// Usa JavaScript Interop para acceder al API nativo de sessionStorage.
/// Los datos persisten solo durante la sesión del navegador (se pierden al cerrar la pestaña).
/// </summary>
public sealed class SessionStorageService : IStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<SessionStorageService> _logger;

    public SessionStorageService(
        IJSRuntime jsRuntime,
        ILogger<SessionStorageService> logger)
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
                "storageInterop.sessionStorage.getItem",
                ct,
                key);

            _logger.LogDebug(
                "SessionStorageService: GetItem - Key: {Key}, HasValue: {HasValue}",
                key, value != null);

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SessionStorageService: Error getting item - Key: {Key}",
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
                "storageInterop.sessionStorage.setItem",
                ct,
                key,
                value);

            if (success)
            {
                _logger.LogDebug(
                    "SessionStorageService: SetItem successful - Key: {Key}",
                    key);
            }
            else
            {
                _logger.LogWarning(
                    "SessionStorageService: SetItem failed - Key: {Key}",
                    key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SessionStorageService: Error setting item - Key: {Key}",
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
                "storageInterop.sessionStorage.removeItem",
                ct,
                key);

            if (success)
            {
                _logger.LogDebug(
                    "SessionStorageService: RemoveItem successful - Key: {Key}",
                    key);
            }
            else
            {
                _logger.LogWarning(
                    "SessionStorageService: RemoveItem failed - Key: {Key}",
                    key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SessionStorageService: Error removing item - Key: {Key}",
                key);
            throw;
        }
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        try
        {
            var success = await _jsRuntime.InvokeAsync<bool>(
                "storageInterop.sessionStorage.clear",
                ct);

            if (success)
            {
                _logger.LogInformation("SessionStorageService: Clear successful");
            }
            else
            {
                _logger.LogWarning("SessionStorageService: Clear failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SessionStorageService: Error clearing storage");
            throw;
        }
    }

    public async Task<bool> ContainsKeyAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var exists = await _jsRuntime.InvokeAsync<bool>(
                "storageInterop.sessionStorage.containsKey",
                ct,
                key);

            _logger.LogDebug(
                "SessionStorageService: ContainsKey - Key: {Key}, Exists: {Exists}",
                key, exists);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SessionStorageService: Error checking if key exists - Key: {Key}",
                key);
            return false;
        }
    }

    public async Task<int> LengthAsync(CancellationToken ct = default)
    {
        try
        {
            var length = await _jsRuntime.InvokeAsync<int>(
                "storageInterop.sessionStorage.length",
                ct);

            _logger.LogDebug(
                "SessionStorageService: Length - Count: {Count}",
                length);

            return length;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SessionStorageService: Error getting length");
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
                "storageInterop.sessionStorage.key",
                ct,
                index);

            _logger.LogDebug(
                "SessionStorageService: Key - Index: {Index}, Key: {Key}",
                index, key ?? "(null)");

            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SessionStorageService: Error getting key at index - Index: {Index}",
                index);
            return null;
        }
    }
}
