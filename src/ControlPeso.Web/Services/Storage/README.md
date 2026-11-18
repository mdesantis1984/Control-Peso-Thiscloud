# Storage Services - Arquitectura Unificada

## Descripción

Interfaz unificada para almacenamiento persistente en navegador con dos implementaciones:

- **LocalStorageService**: Persistencia permanente (incluso después de cerrar navegador)
- **SessionStorageService**: Persistencia temporal (se pierde al cerrar pestaña)

## Cookies

No hay servicio de almacenamiento en cookies: `CookieStorageService` se eliminó (SEC-5561) porque
escribía cookies sin `HttpOnly`/`Secure`/`SameSite` y no funciona tras el render inicial en Blazor Server.
Para persistencia en el navegador usar `LocalStorageService` o `SessionStorageService`.

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
```

## Uso Avanzado (Múltiples Storages)

Si necesitas usar ambos storages en un mismo servicio, inyecta las implementaciones concretas:

```csharp
public class MultiStorageService(LocalStorageService localStorage, SessionStorageService sessionStorage)
{
    public Task SavePermanentAsync(string key, string value) => localStorage.SetItemAsync(key, value);

    public Task SaveTemporaryAsync(string key, string value) => sessionStorage.SetItemAsync(key, value);
}
```

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
