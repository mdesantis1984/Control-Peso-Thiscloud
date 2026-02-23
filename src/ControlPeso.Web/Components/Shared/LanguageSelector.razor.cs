using ControlPeso.Web.Models;
using Microsoft.AspNetCore.Components;
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

    private readonly List<LanguageOption> _languages = new()
    {
        new() { Code = "es", Label = "Español (ARG)", CountryCode = "ar" },
        new() { Code = "en", Label = "English (USA)", CountryCode = "us" },
        new() { Code = "zh", Label = "中文 (China)", CountryCode = "cn" },
        new() { Code = "fr", Label = "Français (France)", CountryCode = "fr" },
        new() { Code = "it", Label = "Italiano (Italia)", CountryCode = "it" }
    };

    // Inicializar con valor por defecto INMEDIATO para evitar NullReferenceException
    // Blazor puede intentar renderizar ANTES de OnInitializedAsync
    private LanguageOption _currentLanguage = new() { Code = "es", Label = "Español (ARG)", CountryCode = "ar" };

    // Control de apertura del menú (mismo patrón que avatar menu)
    private bool _menuOpen = false;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Intentar obtener idioma desde localStorage
            var savedLanguage = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "language");

            // Solo actualizar si se encontró un idioma guardado válido
            if (!string.IsNullOrWhiteSpace(savedLanguage))
            {
                var matchedLanguage = _languages.FirstOrDefault(x => x.Code == savedLanguage);
                if (matchedLanguage != null)
                {
                    _currentLanguage = matchedLanguage;
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
            // Guardar en localStorage
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", "language", language.Code);

            // Mostrar confirmación
            Snackbar.Add($"Idioma cambiado a {language.Label}", Severity.Success);

            // TODO: Implementar cambio real de idioma con IStringLocalizer en Fase 10
            // Por ahora solo guardamos la preferencia
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error al cambiar idioma: {ex.Message}", Severity.Error);
        }
    }
}
