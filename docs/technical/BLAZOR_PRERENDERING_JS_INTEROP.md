# Blazor Server Prerendering y JavaScript Interop

## Problema

### Descripción
```
System.InvalidOperationException: JavaScript interop calls cannot be issued at this time. 
This is because the component is being statically rendered. When prerendering is enabled, 
JavaScript interop calls can only be performed during the OnAfterRenderAsync lifecycle method.
```

### Stack de Error
```
LocalStorageService.GetItemAsync
  → ThemeService.GetUserThemePreferenceAsync
    → MainLayout.OnInitializedAsync
```

## Causa Raíz Arquitectural

### Ciclo de Vida de Blazor Server con Prerendering

Cuando Blazor Server usa `InteractiveServerRenderMode` (configurado en `App.razor`), el ciclo de vida tiene **DOS fases**:

#### Fase 1: Prerendering (Servidor - SIN SignalR)
```
1. Blazor renderiza el componente ESTÁTICAMENTE en el servidor
2. NO hay conexión SignalR con el navegador
3. JavaScript interop NO está disponible
4. Se ejecuta: OnInitializedAsync() → OnParametersSetAsync()
5. Se genera HTML estático que se envía al navegador
```

#### Fase 2: Interactive (Cliente - CON SignalR)
```
1. El navegador recibe el HTML estático
2. Se establece conexión SignalR
3. Blazor "hidrata" el componente (lo hace interactivo)
4. JavaScript interop AHORA está disponible
5. Se ejecuta: OnAfterRenderAsync(firstRender: true)
```

### Por Qué Falló el Código Original

```csharp
// ❌ MAL - OnInitializedAsync ejecuta durante prerendering
protected override async Task OnInitializedAsync()
{
    // Durante prerendering, esto FALLA:
    _isDarkMode = await ThemeService.GetUserThemePreferenceAsync();
    //                     ↓
    //            LocalStorageService.GetItemAsync()
    //                     ↓
    //            _jsRuntime.InvokeAsync() ← BOOM! JS no disponible
}
```

## Solución Implementada

### Opción 1: Mover a OnAfterRenderAsync (RECOMENDADO) ✅

```csharp
// ✅ CORRECTO - OnAfterRenderAsync ejecuta DESPUÉS de prerendering
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        // AHORA sí tenemos JS interop disponible
        _isDarkMode = await ThemeService.GetUserThemePreferenceAsync();
        await InvokeAsync(StateHasChanged); // Re-render con tema correcto
    }
}
```

### Por Qué Esta Solución Es Correcta

1. **Respeta el ciclo de vida de Blazor Server**: No intenta usar JS interop durante prerendering
2. **Mantiene prerendering habilitado**: Mejor SEO y performance (HTML inicial se envía rápido)
3. **No requiere cambios en servicios**: `LocalStorageService` sigue siendo puro
4. **Patrón oficial de Microsoft**: Documentado en docs de Blazor

## Alternativas Consideradas

### Opción 2: Detectar Prerendering en LocalStorageService

```csharp
public async Task<string?> GetItemAsync(string key, CancellationToken ct = default)
{
    try
    {
        // Detectar si JS interop está disponible
        if (_jsRuntime is IJSInProcessRuntime)
        {
            return await _jsRuntime.InvokeAsync<string?>(
                "storageInterop.localStorage.getItem", ct, key);
        }
        return null; // Prerendering - retornar null
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop"))
    {
        return null; // Fallback silencioso
    }
}
```

**Desventajas:**
- Contamina servicio de storage con lógica de ciclo de vida
- Dificulta debugging (errores silenciosos)
- NO es responsabilidad del servicio saber sobre prerendering

### Opción 3: Deshabilitar Prerendering

```razor
<!-- App.razor -->
<Routes @rendermode="new InteractiveServerRenderMode(prerender: false)" />
```

**Desventajas:**
- ❌ Peor SEO (sin HTML inicial)
- ❌ Peor performance (delay hasta que SignalR conecta)
- ❌ Peor UX (pantalla blanca inicial)
- ❌ NO es la solución correcta arquitecturalmente

## Lecciones Aprendidas

### Reglas de JavaScript Interop en Blazor Server

1. **NUNCA** llamar `IJSRuntime.InvokeAsync()` en:
   - `OnInitializedAsync()`
   - `OnParametersSetAsync()`
   - Constructor del componente
   - Servicios inyectados que se llaman durante prerendering

2. **SIEMPRE** llamar `IJSRuntime.InvokeAsync()` en:
   - `OnAfterRenderAsync(firstRender: true)`
   - Event handlers (onclick, onchange, etc.)
   - Métodos invocados DESPUÉS de la hidratación

### Detección de Prerendering

Si necesitas detectar prerendering (aunque NO es necesario en esta solución):

```csharp
// Opción 1: Usar OperatingSystem.IsBrowser() (.NET 5+)
if (OperatingSystem.IsBrowser())
{
    // Código que solo corre en el navegador
}

// Opción 2: Proteger con try/catch específico
try
{
    await _jsRuntime.InvokeAsync(...);
}
catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop"))
{
    // Prerendering - manejar gracefully
}
```

## Referencias

- [Blazor Server Rendering Modes - Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes)
- [Blazor Lifecycle - Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle)
- [JavaScript Interop - Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/)

## Cambios Realizados

### `MainLayout.razor.cs`

**Antes:**
```csharp
protected override async Task OnInitializedAsync()
{
    // ❌ Llamaba a ThemeService que usa JS interop
    _isDarkMode = await ThemeService.GetUserThemePreferenceAsync();
}

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender && _themeProvider != null)
    {
        await InvokeAsync(StateHasChanged);
    }
}
```

**Después:**
```csharp
protected override async Task OnInitializedAsync()
{
    // ✅ NO carga tema aquí - se hará en OnAfterRenderAsync
    // Resto del código sin cambios
}

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        // ✅ Carga tema DESPUÉS de que JS interop esté disponible
        _isDarkMode = await ThemeService.GetUserThemePreferenceAsync();
        Logger.LogInformation("MainLayout: Theme preference loaded (after render) - IsDarkMode: {IsDarkMode}", _isDarkMode);
        await InvokeAsync(StateHasChanged);
    }
}
```

## Impacto

- ✅ **Zero cambios en servicios**: `LocalStorageService`, `ThemeService`, `UserPreferencesService` sin modificar
- ✅ **Mantiene prerendering**: SEO y performance intactos
- ✅ **Sigue arquitectura Blazor**: Usa el ciclo de vida correctamente
- ✅ **Fix permanente**: No volverá a ocurrir este error

## Testing

### Casos de Prueba

1. **Usuario NO autenticado:**
   - Primera carga → Lee de localStorage (si existe)
   - Default: dark mode si localStorage vacío
   - Toggle funciona y persiste en localStorage

2. **Usuario autenticado:**
   - Primera carga → Lee de base de datos
   - Toggle funciona y persiste en base de datos
   - Sincronización con Profile page

3. **Prerendering:**
   - HTML inicial se genera con tema default (dark)
   - Después de hidratación (OnAfterRenderAsync), se carga tema correcto
   - Re-render automático aplica el tema

### Verificación

```bash
# 1. Build sin errores
dotnet build

# 2. Run y verificar logs
dotnet run

# Esperado en logs:
# MainLayout: Initializing
# MainLayout: Theme preference loaded (after render) - IsDarkMode: true/false
```

---

**Fecha**: 2025-02-17  
**Autor**: Copilot (Claude Sonnet 4.5)  
**Issue**: JavaScript interop durante prerendering  
**Solución**: Mover carga de tema de OnInitializedAsync → OnAfterRenderAsync  
**Estado**: ✅ RESUELTO
