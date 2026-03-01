namespace ControlPeso.Web.Services.Storage;

/// <summary>
/// Implementación de IStorageService para cookies HTTP.
/// 
/// ⚠️ ADVERTENCIA - LIMITACIONES EN BLAZOR SERVER:
/// Este servicio SOLO funciona durante la petición HTTP inicial (pre-render).
/// Después del render inicial, las interacciones del usuario se manejan vía SignalR/WebSockets,
/// por lo que NO se pueden modificar cookies (la respuesta HTTP ya comenzó).
/// 
/// USO RECOMENDADO:
/// - GetItemAsync: Funciona en cualquier momento (lee cookies del request inicial)
/// - SetItemAsync: SOLO funciona durante OnInitializedAsync / OnParametersSetAsync en pre-render
/// - Para storage durante interacciones de usuario, usar LocalStorageService o SessionStorageService
/// </summary>
public sealed class CookieStorageService : IStorageService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CookieStorageService> _logger;
    private const int DefaultExpirationDays = 365;

    public CookieStorageService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<CookieStorageService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Task<string?> GetItemAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                _logger.LogWarning(
                    "CookieStorageService: GetItem - HttpContext is null - Key: {Key}",
                    key);
                return Task.FromResult<string?>(null);
            }

            var value = httpContext.Request.Cookies[key];

            _logger.LogDebug(
                "CookieStorageService: GetItem - Key: {Key}, HasValue: {HasValue}",
                key, value != null);

            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "CookieStorageService: Error getting cookie - Key: {Key}",
                key);
            return Task.FromResult<string?>(null);
        }
    }

    public Task SetItemAsync(string key, string value, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                _logger.LogWarning(
                    "CookieStorageService: SetItem - HttpContext is null - Key: {Key}",
                    key);
                return Task.CompletedTask;
            }

            // ⚠️ CRITICAL: Check if response has started (Blazor Server limitation)
            if (httpContext.Response.HasStarted)
            {
                _logger.LogError(
                    "CookieStorageService: Cannot set cookie - Response has already started - Key: {Key}. " +
                    "In Blazor Server, cookies can only be set during pre-render (OnInitializedAsync). " +
                    "Use LocalStorageService or SessionStorageService for user interactions.",
                    key);
                throw new InvalidOperationException(
                    "Cannot set cookie after response has started. " +
                    "In Blazor Server, use LocalStorageService or SessionStorageService for user interactions.");
            }

            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(DefaultExpirationDays),
                HttpOnly = false, // Debe ser accesible desde JavaScript
                Secure = true,    // Solo HTTPS
                SameSite = SameSiteMode.Strict,
                IsEssential = true
            };

            httpContext.Response.Cookies.Append(key, value, cookieOptions);

            _logger.LogDebug(
                "CookieStorageService: SetItem successful - Key: {Key}",
                key);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "CookieStorageService: Error setting cookie - Key: {Key}",
                key);
            throw;
        }
    }

    public Task RemoveItemAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                _logger.LogWarning(
                    "CookieStorageService: RemoveItem - HttpContext is null - Key: {Key}",
                    key);
                return Task.CompletedTask;
            }

            // ⚠️ CRITICAL: Check if response has started
            if (httpContext.Response.HasStarted)
            {
                _logger.LogError(
                    "CookieStorageService: Cannot remove cookie - Response has already started - Key: {Key}",
                    key);
                throw new InvalidOperationException(
                    "Cannot remove cookie after response has started.");
            }

            httpContext.Response.Cookies.Delete(key);

            _logger.LogDebug(
                "CookieStorageService: RemoveItem successful - Key: {Key}",
                key);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "CookieStorageService: Error removing cookie - Key: {Key}",
                key);
            throw;
        }
    }

    public Task ClearAsync(CancellationToken ct = default)
    {
        _logger.LogWarning(
            "CookieStorageService: Clear operation not supported for cookies. " +
            "Cookies must be removed individually with RemoveItemAsync.");

        throw new NotSupportedException(
            "Clear operation not supported for cookies. " +
            "Use RemoveItemAsync for individual cookies.");
    }

    public Task<bool> ContainsKeyAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                _logger.LogWarning(
                    "CookieStorageService: ContainsKey - HttpContext is null - Key: {Key}",
                    key);
                return Task.FromResult(false);
            }

            var exists = httpContext.Request.Cookies.ContainsKey(key);

            _logger.LogDebug(
                "CookieStorageService: ContainsKey - Key: {Key}, Exists: {Exists}",
                key, exists);

            return Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "CookieStorageService: Error checking if cookie exists - Key: {Key}",
                key);
            return Task.FromResult(false);
        }
    }

    public Task<int> LengthAsync(CancellationToken ct = default)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                _logger.LogWarning("CookieStorageService: Length - HttpContext is null");
                return Task.FromResult(0);
            }

            var count = httpContext.Request.Cookies.Count;

            _logger.LogDebug(
                "CookieStorageService: Length - Count: {Count}",
                count);

            return Task.FromResult(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CookieStorageService: Error getting cookie count");
            return Task.FromResult(0);
        }
    }

    public Task<string?> KeyAsync(int index, CancellationToken ct = default)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Index cannot be negative");

        try
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                _logger.LogWarning(
                    "CookieStorageService: Key - HttpContext is null - Index: {Index}",
                    index);
                return Task.FromResult<string?>(null);
            }

            var keys = httpContext.Request.Cookies.Keys.ToList();

            if (index >= keys.Count)
            {
                _logger.LogWarning(
                    "CookieStorageService: Key - Index out of range - Index: {Index}, Count: {Count}",
                    index, keys.Count);
                return Task.FromResult<string?>(null);
            }

            var key = keys[index];

            _logger.LogDebug(
                "CookieStorageService: Key - Index: {Index}, Key: {Key}",
                index, key);

            return Task.FromResult<string?>(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "CookieStorageService: Error getting cookie key at index - Index: {Index}",
                index);
            return Task.FromResult<string?>(null);
        }
    }
}
