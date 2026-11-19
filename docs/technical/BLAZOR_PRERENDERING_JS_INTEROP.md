# Blazor Server: prerendering y JavaScript interop

> **Verificado contra el código**: 2026-11-19 (versión 9.0.0)

## Problema

La aplicación usa `@rendermode="InteractiveServer"` (con prerendering activo por defecto) en `HeadOutlet` y `Routes` de `src/ControlPeso.Web/Components/App.razor`. Eso implica dos fases:

1. **Prerendering** (servidor, sin SignalR): se ejecutan `OnInitializedAsync` y `OnParametersSetAsync` y se envía HTML estático. **No hay JS interop.**
2. **Interactiva** (con circuito SignalR): `OnAfterRenderAsync(firstRender: true)` ya puede usar JS interop.

Llamar a `IJSRuntime` durante la primera fase produce:

```
System.InvalidOperationException: JavaScript interop calls cannot be issued at this time.
This is because the component is being statically rendered.
```

Los servicios `LocalStorageService`, `SessionStorageService` (`src/ControlPeso.Web/Services/Storage/`) y `ThemeService` dependen de JS interop, así que cualquier componente que los use durante el prerendering falla.

## Regla

- **Nunca** usar `IJSRuntime` (ni servicios que lo usen, como `ThemeService` o los storage services) en el constructor, `OnInitialized(Async)` ni `OnParametersSet(Async)`.
- **Siempre** hacerlo en `OnAfterRenderAsync(firstRender: true)` o en manejadores de eventos, y volver a renderizar con `StateHasChanged` si cambia el estado visible.

## Implementación actual: carga del tema

`MainLayout.razor.cs` mantiene un valor por defecto (modo oscuro) durante el prerendering y carga la preferencia real después del primer render:

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        _jsReady = true; // marcar JS disponible antes de cualquier interop
        try
        {
            _isDarkMode = await ThemeService.GetUserThemePreferenceAsync();
            await InvokeAsync(StateHasChanged); // re-render con el tema real
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "MainLayout: Error loading theme in OnAfterRenderAsync");
            // se mantiene el valor por defecto
        }
    }
    await base.OnAfterRenderAsync(firstRender);
}
```

Comportamiento resultante:

- HTML inicial con el tema por defecto (oscuro); tras la hidratación se aplica la preferencia real (usuario autenticado: base de datos; en otro caso: `localStorage`).
- Cualquier otro código que necesite JS interop debe respetar la bandera `_jsReady` o el mismo patrón `firstRender`.

## Alternativas descartadas

| Alternativa | Motivo |
|-------------|--------|
| Capturar la excepción dentro de `LocalStorageService` | Mezcla ciclo de vida de componentes con el servicio de storage y oculta errores. |
| `InteractiveServerRenderMode(prerender: false)` | Pierde el HTML inicial (peor SEO y primera pintura) para resolver un problema que se evita con el ciclo de vida correcto. |

## Referencias

- [Render modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes)
- [Component lifecycle](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle)
- [JavaScript interop](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/)
- [ARCHITECTURE.md](../ARCHITECTURE.md)
