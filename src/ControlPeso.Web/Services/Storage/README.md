# Storage Services - Arquitectura Unificada

## Descripción

Interfaz unificada para almacenamiento persistente en navegador con múltiples implementaciones:

- **LocalStorageService**: Persistencia permanente (incluso después de cerrar navegador)
- **SessionStorageService**: Persistencia temporal (se pierde al cerrar pestaña)
- **CookieStorageService**: Cookies HTTP (con limitaciones en Blazor Server)

## Limitaciones de Blazor Server

### Problema con Cookies
En **Blazor Server**, después del render inicial de la página, las interacciones del usuario se manejan a través de **SignalR/WebSockets**, NO a través de nuevas peticiones HTTP.

Esto significa que:
- ✅ **GetItemAsync**: Funciona siempre (lee cookies del request inicial)
- ❌ **SetItemAsync**: SOLO funciona durante `OnInitializedAsync` en **pre-render**
- ❌ **SetItemAsync en eventos de usuario**: FALLA con `InvalidOperationException: Headers are read-only, response has already started`

### Solución
Para interacciones de usuario (clicks, cambios de estado), usar:
- **LocalStorageService** (recomendado para persistencia permanente)
- **SessionStorageService** (para persistencia temporal)

## Uso en ThemeService

```csharp
// ✅ CORRECTO - Usa LocalStorageService por defecto
public sealed class ThemeService
{
    private readonly IStorageService _storageService; // Inyecta LocalStorageService

    public async Task SetUserThemePreferenceAsync(bool isDarkMode)
    {
        // Usuarios autenticados: guardar en DB
        if (userAuthenticated)
        {
            await _userPreferencesService.UpdateDarkModeAsync(userId, isDarkMode);
            return;
        }

        // Usuarios NO autenticados: guardar en localStorage
        await _storageService.SetItemAsync("IsDarkMode", isDarkMode.ToString());
    }
}
```

## Registro en DI (Program.cs)

```csharp
// LocalStorageService es el predeterminado para IStorageService
builder.Services.AddScoped<IStorageService, LocalStorageService>();

// Registrar implementaciones específicas para inyección explícita
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<SessionStorageService>();
builder.Services.AddScoped<CookieStorageService>();
```

## Uso Avanzado (Múltiples Storages)

Si necesitas usar múltiples storages en un mismo servicio:

```csharp
public class MultiStorageService
{
    private readonly LocalStorageService _localStorage;
    private readonly SessionStorageService _sessionStorage;
    private readonly CookieStorageService _cookieStorage;

    public MultiStorageService(
        LocalStorageService localStorage,
        SessionStorageService sessionStorage,
        CookieStorageService cookieStorage)
    {
        _localStorage = localStorage;
        _sessionStorage = sessionStorage;
        _cookieStorage = cookieStorage;
    }

    public async Task SavePermanentAsync(string key, string value)
    {
        await _localStorage.SetItemAsync(key, value);
    }

    public async Task SaveTemporaryAsync(string key, string value)
    {
        await _sessionStorage.SetItemAsync(key, value);
    }

    public async Task<string?> ReadCookieAsync(string key)
    {
        // Cookies solo para LECTURA en Blazor Server (post-render)
        return await _cookieStorage.GetItemAsync(key);
    }
}
```

## Migración desde Cookies

Si estás migrando desde `CookieStorageService` a `LocalStorageService`:

1. **Leer cookies existentes en `OnInitializedAsync`** (pre-render):
   ```csharp
   protected override async Task OnInitializedAsync()
   {
       // Migrar cookie existente a localStorage
       var cookieValue = await _cookieStorage.GetItemAsync("IsDarkMode");
       if (cookieValue != null)
       {
           await _localStorage.SetItemAsync("IsDarkMode", cookieValue);
           // Opcional: eliminar cookie (solo en pre-render)
           await _cookieStorage.RemoveItemAsync("IsDarkMode");
       }
   }
   ```

2. **Usar localStorage para todas las operaciones de escritura**

## Testing

```csharp
// Mock para tests
public class MockStorageService : IStorageService
{
    private readonly Dictionary<string, string> _storage = new();

    public Task<string?> GetItemAsync(string key, CancellationToken ct = default)
    {
        _storage.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task SetItemAsync(string key, string value, CancellationToken ct = default)
    {
        _storage[key] = value;
        return Task.CompletedTask;
    }

    // ... implementar resto de métodos
}
```

## Referencias

- [Web Storage API (MDN)](https://developer.mozilla.org/en-US/docs/Web/API/Web_Storage_API)
- [Blazor JavaScript Interop](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/)
- [Blazor Server Architecture](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models?view=aspnetcore-8.0#blazor-server)
