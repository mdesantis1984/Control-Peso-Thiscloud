using System.Globalization;
using ControlPeso.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Localization;
using Microsoft.JSInterop;
using MudBlazor;

namespace ControlPeso.Web.Components.Shared;

public partial class LanguageSelector
{
    [Parameter]
    public string? Class { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    // Fase 10: Solo es-AR y en-US (zh, fr, it removidos por scope)
    private readonly List<LanguageOption> _languages = new()
    {
        new() { Code = "es", Label = "Español (ARG)", CountryCode = "ar" },
        new() { Code = "en", Label = "English (USA)", CountryCode = "us" }
    };

    // Inicializar con valor por defecto INMEDIATO para evitar NullReferenceException
    // Blazor puede intentar renderizar ANTES de OnInitializedAsync
    private LanguageOption _currentLanguage = new() { Code = "es", Label = "Español (ARG)", CountryCode = "ar" };

    // Control de apertura del menú (mismo patrón que avatar menu)
    private bool _menuOpen = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {
            // Intentar obtener idioma desde localStorage
            // CRÍTICO: JSInterop SOLO funciona después del primer render (no en prerendering)
            var savedLanguage = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "language");

            // Solo actualizar si se encontró un idioma guardado válido
            if (!string.IsNullOrWhiteSpace(savedLanguage))
            {
                var matchedLanguage = _languages.FirstOrDefault(x => x.Code == savedLanguage);
                if (matchedLanguage != null)
                {
                    _currentLanguage = matchedLanguage;
                    StateHasChanged(); // Forzar re-render con idioma actualizado
                }
            }
        }
        catch
        {
            // Si falla, mantener el default (ya inicializado arriba)
        }
    }

    private async Task SelectLanguageAsync(LanguageOption language)
    {
        if (_currentLanguage.Code == language.Code)
            return;

        _currentLanguage = language;

        try
        {
            // 1. Guardar en localStorage (persistencia client-side)
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", "language", language.Code);

            // 2. Mapear código corto (es/en) a cultura completa (es-AR/en-US)
            var cultureName = language.Code switch
            {
                "es" => "es-AR",
                "en" => "en-US",
                _ => "es-AR" // fallback default
            };

            var culture = new CultureInfo(cultureName);

            // 3. Cambiar CultureInfo del thread actual (Blazor Server)
            // IMPORTANTE: Esto solo afecta al request actual, NO persiste entre requests
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            // 4. Persistir en cookie para RequestLocalization middleware
            // La cookie sobrevive a refresh y nuevas sesiones (1 año max-age)
            var cookieName = CookieRequestCultureProvider.DefaultCookieName; // ".AspNetCore.Culture"
            var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));

            // Cookie: nombre=.AspNetCore.Culture, value=c=es-AR|uic=es-AR, path=/, max-age=1 año, SameSite=Strict
            await JSRuntime.InvokeVoidAsync(
                "eval",
                $"document.cookie = '{cookieName}={cookieValue}; path=/; max-age=31536000; SameSite=Strict'");

            // 5. Forzar recarga COMPLETA de la página para aplicar nuevas strings
            // forceLoad=true: recarga desde servidor, NO usa cache
            // Esto hace que RequestLocalization middleware lea la cookie y establezca cultura correcta
            NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);

            // Nota: El Snackbar NO se verá porque hacemos forceLoad inmediato
            // Pero lo dejamos por si en el futuro se hace reload sin forceLoad
            // Snackbar.Add($"Idioma cambiado a {language.Label}", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error al cambiar idioma: {ex.Message}", Severity.Error);
        }
    }
}
