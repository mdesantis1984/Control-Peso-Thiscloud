# PLAN Fase 10 — Globalización (Multilanguage Support) | ControlPeso.Thiscloud

- Proyecto: `ControlPeso.Thiscloud.sln`
- Rama: `feature/fase-9-pixel-perfect` (working branch)
- Fase: **10 - Globalización (i18n)**
- Fecha inicio: **2026-02-19**
- Última actualización: **2026-02-23 01:05**
- Estado: 🟡 **EN PROGRESO** — 5/20 tareas (25% ejecutado)
- Duración estimada: **4-6 días** (40-60 horas de trabajo)

---

## Objetivo

Implementar soporte completo de **internacionalización (i18n)** para **Español (es-AR)** e **Inglés (en-US)** usando ASP.NET Core `IStringLocalizer` + archivos de recursos `.resx`. Convertir TODOS los textos hardcoded en español a recursos localizables, configurar RequestLocalization middleware, y sincronizar la selección del LanguageSelector component con `CultureInfo` para cambio de idioma en tiempo real.

---

## Contexto Actual

### Estado del Sistema

**✅ LanguageSelector Component Existente** (creado en P5.8):
- Dropdown con 5 idiomas: es-ARG, en-USA, zh-CN, fr-FR, it-IT
- Banderas cargadas desde Flagpedia CDN (https://flagcdn.com)
- Persiste selección en localStorage
- Funcionalidad visual completa (MudMenu + flags + check mark)

**❌ Limitaciones Actuales**:
- ❌ **Todos los textos están hardcoded en español**
- ❌ Cambiar idioma en LanguageSelector NO afecta el UI (solo guarda en localStorage)
- ❌ NO hay configuración de localization en Program.cs
- ❌ NO existen archivos `.resx` (carpeta `Resources/` NO existe)
- ❌ Componentes NO inyectan `IStringLocalizer`
- ❌ Meta tags SEO son estáticos en español

### Archivos con Textos Hardcoded

**Páginas** (8 archivos):
- `Dashboard.razor` — ~40 strings (Welcome back, Current Weight, Weekly Change, Goal Progress, BMI, etc.)
- `Profile.razor` — ~35 strings (Profile, Personal Information, Height, Date of Birth, Goal Weight, etc.)
- `History.razor` — ~25 strings (History, Search, Filters, Date Range, Weight Logs, etc.)
- `Trends.razor` — ~30 strings (Trends, Analysis, Last 7 Days, Last 30 Days, Projection, etc.)
- `Admin.razor` — ~45 strings (Admin Panel, Total Users, Active Users, Change Role, Export, etc.)
- `Login.razor` — ~15 strings (Login, Welcome to Control Peso, Continue with Google, etc.)
- `Error.razor` — ~10 strings (Error, Something went wrong, Go to Dashboard, etc.)
- `Home.razor` — ~8 strings (Home, Track your weight, Get started, etc.)

**Componentes** (7 archivos):
- `MainLayout.razor` — ~5 strings (Control Peso Thiscloud, Toggle menu, etc.)
- `NavMenu.razor` — ~8 strings (Dashboard, Profile, History, Trends, Admin, etc.)
- `AddWeightDialog.razor` — ~12 strings (Add Weight, Weight (kg), Date, Time, Notes, Save, Cancel, etc.)
- `StatsCard.razor` — ~4 strings (vs Last Week, Higher is Better, Lower is Better, etc.)
- `TrendCard.razor` — ~4 strings (Trend, Increase, Decrease, No Change, etc.)
- `WeightChart.razor` — ~3 strings (Weight Evolution, kg, lbs, etc.)
- `NotificationBell.razor` — ~5 strings (Notifications, No new notifications, Mark all as read, etc.)

**Validators** (3 archivos):
- `CreateWeightLogValidator.cs` — ~8 mensajes de error
- `UpdateWeightLogValidator.cs` — ~6 mensajes de error
- `UpdateUserProfileValidator.cs` — ~10 mensajes de error

**Total**: ~228 strings hardcoded que necesitan traducción

---

## Filosofía de Globalización

### Principios de Diseño

1. **Single Source of Truth**: Archivos `.resx` son la ÚNICA fuente de strings
2. **Granularidad**: 1 archivo `.resx` por página/componente (NO un Resources.resx gigante)
3. **Naming Convention**: `ComponentName.{culture}.resx` (ej: `Dashboard.es-AR.resx`)
4. **Fallback**: `es-AR` es la cultura default; si falta traducción en `en-US`, cae a default
5. **Format**: Placeholders tipo `MessageFormat` (ej: `"Welcome back, {0}!"`)
6. **Performance**: `IStringLocalizer<T>` con caching automático (sin impacto en rendering)
7. **Testable**: Validators con `IStringLocalizer` (mensajes traducidos en tests)

### Culturas Soportadas

| Código | Cultura | País | Default | Notas |
|--------|---------|------|---------|-------|
| `es-AR` | Español | Argentina | ✅ SÍ | Cultura por defecto del sistema |
| `en-US` | English | United States | ❌ NO | Segunda cultura soportada |

**Futuro**: La arquitectura permite agregar más culturas simplemente creando nuevos archivos `.resx` (ej: `Dashboard.fr-FR.resx` para francés).

### Alcance de Traducciones

| Categoría | Cantidad | Archivos .resx | Notas |
|-----------|----------|----------------|-------|
| **Páginas** | 8 | 16 archivos (8 × 2 culturas) | Dashboard, Profile, History, Trends, Admin, Login, Error, Home |
| **Componentes Layout** | 2 | 4 archivos (2 × 2 culturas) | MainLayout, NavMenu |
| **Componentes Shared** | 5 | 10 archivos (5 × 2 culturas) | AddWeightDialog, StatsCard, TrendCard, WeightChart, NotificationBell |
| **Validators** | 3 | 6 archivos (3 × 2 culturas) | CreateWeightLogValidator, UpdateWeightLogValidator, UpdateUserProfileValidator |
| **TOTAL** | 18 | **36 archivos .resx** | 18 componentes × 2 culturas |

---

## Arquitectura de Recursos

### Estructura de Carpetas

```
src/ControlPeso.Web/
├── Resources/
│   ├── Pages/                                    ← 16 archivos (8 páginas × 2 culturas)
│   │   ├── Dashboard.es-AR.resx
│   │   ├── Dashboard.en-US.resx
│   │   ├── Profile.es-AR.resx
│   │   ├── Profile.en-US.resx
│   │   ├── History.es-AR.resx
│   │   ├── History.en-US.resx
│   │   ├── Trends.es-AR.resx
│   │   ├── Trends.en-US.resx
│   │   ├── Admin.es-AR.resx
│   │   ├── Admin.en-US.resx
│   │   ├── Login.es-AR.resx
│   │   ├── Login.en-US.resx
│   │   ├── Error.es-AR.resx
│   │   ├── Error.en-US.resx
│   │   ├── Home.es-AR.resx
│   │   └── Home.en-US.resx
│   ├── Components/
│   │   ├── Layout/                               ← 4 archivos (2 componentes × 2 culturas)
│   │   │   ├── MainLayout.es-AR.resx
│   │   │   ├── MainLayout.en-US.resx
│   │   │   ├── NavMenu.es-AR.resx
│   │   │   └── NavMenu.en-US.resx
│   │   └── Shared/                               ← 10 archivos (5 componentes × 2 culturas)
│   │       ├── AddWeightDialog.es-AR.resx
│   │       ├── AddWeightDialog.en-US.resx
│   │       ├── StatsCard.es-AR.resx
│   │       ├── StatsCard.en-US.resx
│   │       ├── TrendCard.es-AR.resx
│   │       ├── TrendCard.en-US.resx
│   │       ├── WeightChart.es-AR.resx
│   │       ├── WeightChart.en-US.resx
│   │       ├── NotificationBell.es-AR.resx
│   │       └── NotificationBell.en-US.resx
│   └── Validators/                               ← 6 archivos (3 validators × 2 culturas)
│       ├── CreateWeightLogValidator.es-AR.resx
│       ├── CreateWeightLogValidator.en-US.resx
│       ├── UpdateWeightLogValidator.es-AR.resx
│       ├── UpdateWeightLogValidator.en-US.resx
│       ├── UpdateUserProfileValidator.es-AR.resx
│       └── UpdateUserProfileValidator.en-US.resx
```

### Ejemplo de Archivo .resx

**Dashboard.es-AR.resx** (Español Argentina):
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="PageTitle" xml:space="preserve">
    <value>Dashboard - Control Peso Thiscloud</value>
  </data>
  <data name="WelcomeBack" xml:space="preserve">
    <value>Bienvenido de nuevo, {0}!</value>
    <comment>Placeholder {0} = user name</comment>
  </data>
  <data name="CurrentWeight" xml:space="preserve">
    <value>Peso Actual</value>
  </data>
  <data name="WeeklyChange" xml:space="preserve">
    <value>Cambio Semanal</value>
  </data>
  <data name="GoalProgress" xml:space="preserve">
    <value>Progreso hacia Meta</value>
  </data>
  <data name="BMI" xml:space="preserve">
    <value>IMC</value>
  </data>
  <data name="WeightEvolution" xml:space="preserve">
    <value>Evolución del Peso (Últimos 30 Días)</value>
  </data>
  <data name="AddWeight" xml:space="preserve">
    <value>Agregar Peso</value>
  </data>
  <data name="Export" xml:space="preserve">
    <value>Exportar</value>
  </data>
  <data name="NoDataMessage" xml:space="preserve">
    <value>¡Bienvenido a Control Peso! Agrega tu primer registro de peso para comenzar.</value>
  </data>
  <data name="AddFirstWeight" xml:space="preserve">
    <value>Agregar Primer Peso</value>
  </data>
</root>
```

**Dashboard.en-US.resx** (English United States):
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="PageTitle" xml:space="preserve">
    <value>Dashboard - Control Peso Thiscloud</value>
  </data>
  <data name="WelcomeBack" xml:space="preserve">
    <value>Welcome back, {0}!</value>
    <comment>Placeholder {0} = user name</comment>
  </data>
  <data name="CurrentWeight" xml:space="preserve">
    <value>Current Weight</value>
  </data>
  <data name="WeeklyChange" xml:space="preserve">
    <value>Weekly Change</value>
  </data>
  <data name="GoalProgress" xml:space="preserve">
    <value>Goal Progress</value>
  </data>
  <data name="BMI" xml:space="preserve">
    <value>BMI</value>
  </data>
  <data name="WeightEvolution" xml:space="preserve">
    <value>Weight Evolution (Last 30 Days)</value>
  </data>
  <data name="AddWeight" xml:space="preserve">
    <value>Add Weight</value>
  </data>
  <data name="Export" xml:space="preserve">
    <value>Export</value>
  </data>
  <data name="NoDataMessage" xml:space="preserve">
    <value>Welcome to Control Peso! Add your first weight log to get started.</value>
  </data>
  <data name="AddFirstWeight" xml:space="preserve">
    <value>Add First Weight</value>
  </data>
</root>
```

---

## Configuración Técnica

### Program.cs — Localization Setup

```csharp
// ============================================================================
// LOCALIZATION CONFIGURATION (FASE 10)
// ============================================================================

// 1. Add Localization services
builder.Services.AddLocalization(options => 
{
    // ResourcesPath: carpeta donde se buscan los archivos .resx
    // Relativo a la raíz del proyecto Web
    options.ResourcesPath = "Resources";
});

// 2. Configure supported cultures
var supportedCultures = new[] 
{ 
    new CultureInfo("es-AR"),  // Español (Argentina) - DEFAULT
    new CultureInfo("en-US")   // English (United States)
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    // Cultura default si no se puede determinar la del usuario
    options.DefaultRequestCulture = new RequestCulture("es-AR");
    
    // Culturas soportadas para formateo de números, fechas, monedas
    options.SupportedCultures = supportedCultures;
    
    // Culturas soportadas para textos de UI (IStringLocalizer)
    options.SupportedUICultures = supportedCultures;
    
    // Request culture provider order (intenta en este orden):
    // 1. CookieRequestCultureProvider: Cookie persistida por LanguageSelector
    // 2. AcceptLanguageHeaderRequestCultureProvider: Browser default (Accept-Language header)
    // 3. DefaultRequestCulture: es-AR (fallback final)
    options.RequestCultureProviders = new IRequestCultureProvider[]
    {
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
    
    // Nombre de la cookie (puede personalizarse)
    // Default: ".AspNetCore.Culture"
    // options.CookieName = ".ControlPeso.Culture";
});

// ... resto de configuración (Authentication, Blazor, etc.)

var app = builder.Build();

// ============================================================================
// MIDDLEWARE PIPELINE — ORDEN CRÍTICO
// ============================================================================

app.UseHttpsRedirection();
app.UseStaticFiles();

// CRÍTICO: UseRequestLocalization DEBE ir:
// - DESPUÉS de UseRouting (si usas endpoint routing)
// - ANTES de UseAuthorization
// - ANTES de MapRazorComponents
app.UseRequestLocalization(
    app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value
);

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

### Uso en Componentes Blazor (Code-Behind Pattern)

**Dashboard.razor.cs** (ejemplo completo):
```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using ControlPeso.Application.DTOs;
using ControlPeso.Application.Interfaces;

namespace ControlPeso.Web.Pages;

public partial class Dashboard
{
    // ========================================================================
    // DEPENDENCY INJECTION
    // ========================================================================
    
    [Inject]
    private IStringLocalizer<Dashboard> Localizer { get; set; } = default!;
    
    [Inject]
    private IWeightLogService WeightLogService { get; set; } = default!;
    
    [Inject]
    private IUserService UserService { get; set; } = default!;
    
    // ========================================================================
    // PROPERTIES
    // ========================================================================
    
    private string _userName = string.Empty;
    private List<WeightLogDto> _weightLogs = [];
    private bool _isLoading = true;
    
    // ========================================================================
    // LOCALIZED STRINGS (Properties for clean markup)
    // ========================================================================
    
    // Simples (sin placeholders)
    private string PageTitle => Localizer["PageTitle"];
    private string CurrentWeightLabel => Localizer["CurrentWeight"];
    private string WeeklyChangeLabel => Localizer["WeeklyChange"];
    private string GoalProgressLabel => Localizer["GoalProgress"];
    private string BMILabel => Localizer["BMI"];
    private string AddWeightButton => Localizer["AddWeight"];
    private string ExportButton => Localizer["Export"];
    
    // Con placeholders (métodos)
    private string GetWelcomeMessage() => Localizer["WelcomeBack", _userName];
    private string GetWeightEvolutionTitle() => Localizer["WeightEvolution"];
    
    // ========================================================================
    // LIFECYCLE
    // ========================================================================
    
    protected override async Task OnInitializedAsync()
    {
        // Lógica de carga de datos...
        await LoadDataAsync();
    }
    
    private async Task LoadDataAsync()
    {
        try
        {
            // Cargar datos del usuario y weight logs
            // ...
        }
        catch (Exception ex)
        {
            // Error handling con mensajes localizados
            Snackbar.Add(Localizer["ErrorLoadingData"], Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }
}
```

**Dashboard.razor** (markup con Localizer):
```razor
@page "/dashboard"
@attribute [Authorize]
@namespace ControlPeso.Web.Pages

<PageTitle>@Localizer["PageTitle"]</PageTitle>

<HeadContent>
    <meta name="description" content="@Localizer["MetaDescription"]" />
</HeadContent>

@if (_isLoading)
{
    <div class="d-flex justify-center align-center" style="height: 80vh;">
        <MudProgressCircular Color="Color.Primary" Size="Size.Large" Indeterminate="true" />
    </div>
}
else if (_weightLogs.Count == 0)
{
    <MudContainer MaxWidth="MaxWidth.Medium" Class="mt-8">
        <MudPaper Elevation="0" Class="pa-8 text-center">
            <MudIcon Icon="@Icons.Material.Filled.MonitorWeight" 
                     Color="Color.Primary" 
                     Style="font-size: 96px;" 
                     Class="mb-4" />
            <MudText Typo="Typo.h4" Class="mb-2">
                @Localizer["NoDataMessage"]
            </MudText>
            <MudButton Variant="Variant.Filled" 
                       Color="Color.Primary" 
                       Size="Size.Large"
                       StartIcon="@Icons.Material.Filled.Add"
                       OnClick="@OpenAddWeightDialog">
                @Localizer["AddFirstWeight"]
            </MudButton>
        </MudPaper>
    </MudContainer>
}
else
{
    @* Header Section *@
    <div class="d-flex justify-space-between align-center mb-6">
        <div>
            <MudText Typo="Typo.h4" Class="mb-1">
                @GetWelcomeMessage()
            </MudText>
            <MudText Typo="Typo.body1" Color="Color.Secondary">
                @Localizer["SubtitleProgress"]
            </MudText>
        </div>
        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   StartIcon="@Icons.Material.Filled.FileDownload"
                   OnClick="@ExportData">
            @ExportButton
        </MudButton>
    </div>

    @* Stats Cards Grid *@
    <MudGrid Spacing="3" Class="mb-6">
        <MudItem xs="12" sm="3">
            <StatsCard Title="@CurrentWeightLabel" 
                       Value="@_currentWeight" 
                       Icon="@Icons.Material.Filled.MonitorWeight" />
        </MudItem>
        @* ... más cards *@
    </MudGrid>
}
```

### Validators con FluentValidation i18n

**CreateWeightLogValidator.cs**:
```csharp
using FluentValidation;
using Microsoft.Extensions.Localization;
using ControlPeso.Application.DTOs;

namespace ControlPeso.Application.Validators;

public class CreateWeightLogValidator : AbstractValidator<CreateWeightLogDto>
{
    private readonly IStringLocalizer<CreateWeightLogValidator> _localizer;

    public CreateWeightLogValidator(IStringLocalizer<CreateWeightLogValidator> localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        _localizer = localizer;

        // Regla: Weight entre 20-500 kg
        RuleFor(x => x.Weight)
            .InclusiveBetween(20, 500)
            .WithMessage(_localizer["WeightRange", 20, 500]);
            // es-AR: "'Peso' debe estar entre {0} y {1} kg."
            // en-US: "'Weight' must be between {0} and {1} kg."

        // Regla: Date no puede ser futuro
        RuleFor(x => x.Date)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Now))
            .WithMessage(_localizer["DateInPast"]);
            // es-AR: "'Fecha' no puede ser en el futuro."
            // en-US: "'Date' cannot be in the future."

        // Regla: Note máximo 500 caracteres
        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Note))
            .WithMessage(_localizer["NoteMaxLength", 500]);
            // es-AR: "'Nota' no debe exceder {0} caracteres."
            // en-US: "'Note' must not exceed {0} characters."
    }
}
```

**CreateWeightLogValidator.es-AR.resx**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="WeightRange" xml:space="preserve">
    <value>'Peso' debe estar entre {0} y {1} kg.</value>
    <comment>Validation message for weight range. {0} = min value, {1} = max value</comment>
  </data>
  <data name="DateInPast" xml:space="preserve">
    <value>'Fecha' no puede ser en el futuro.</value>
  </data>
  <data name="NoteMaxLength" xml:space="preserve">
    <value>'Nota' no debe exceder {0} caracteres.</value>
    <comment>{0} = max length</comment>
  </data>
</root>
```

### Integración LanguageSelector

**LanguageSelector.razor.cs** (modificaciones):
```csharp
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Localization;
using Microsoft.JSInterop;
using MudBlazor;
using ControlPeso.Web.Models;

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

    private readonly List<LanguageOption> _languages = new()
    {
        new() { Code = "es", Label = "Español (ARG)", CountryCode = "ar" },
        new() { Code = "en", Label = "English (USA)", CountryCode = "us" }
        // Removidos zh, fr, it — solo es/en en Fase 10
    };

    private LanguageOption _currentLanguage = new() { Code = "es", Label = "Español (ARG)", CountryCode = "ar" };
    private bool _menuOpen = false;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // 1. Intentar obtener idioma desde localStorage
            var savedLanguage = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "language");

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
            // Si falla localStorage, mantener default (es-AR)
        }
    }

    private async Task SelectLanguageAsync(LanguageOption language)
    {
        if (_currentLanguage.Code == language.Code)
            return;

        _currentLanguage = language;

        try
        {
            // 1. Guardar en localStorage (ya implementado)
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", "language", language.Code);

            // 2. Mapear código corto (es/en) a cultura completa (es-AR/en-US)
            var cultureName = language.Code == "es" ? "es-AR" : "en-US";
            var culture = new CultureInfo(cultureName);

            // 3. Cambiar CultureInfo del thread actual (solo afecta request actual en Blazor Server)
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            // 4. Persistir en cookie para RequestLocalization middleware
            // La cookie sobrevive a refresh y nuevas sesiones
            var cookieName = CookieRequestCultureProvider.DefaultCookieName;
            var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
            
            // Cookie: nombre=.AspNetCore.Culture, value=c=es-AR|uic=es-AR, path=/, max-age=1 año
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
```

---

## Tareas Detalladas (20 tareas)

### P10.1 — Configurar Localization en Program.cs

**Objetivo**: Configurar ASP.NET Core localization services + RequestLocalizationOptions + middleware.

**Pasos**:
1. Agregar `builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");`
2. Configurar `supportedCultures` array con `es-AR` y `en-US`
3. Configurar `RequestLocalizationOptions`:
   - `DefaultRequestCulture = "es-AR"`
   - `SupportedCultures` y `SupportedUICultures`
   - `RequestCultureProviders` (Cookie + AcceptLanguage)
4. Agregar `app.UseRequestLocalization(...)` en middleware pipeline (DESPUÉS de UseRouting, ANTES de UseAuthorization)
5. Compilar y verificar que no hay errores

**Criterios de Aceptación**:
- ✅ Localization services registrados en DI
- ✅ Middleware en orden correcto
- ✅ Build exitoso sin errores

**Duración Estimada**: 30 minutos

---

### P10.2 — Crear Estructura de Carpetas Resources/

**Objetivo**: Crear carpetas necesarias para archivos `.resx`.

**Pasos**:
1. Crear carpeta `src/ControlPeso.Web/Resources/`
2. Crear subcarpeta `Resources/Pages/`
3. Crear subcarpeta `Resources/Components/Layout/`
4. Crear subcarpeta `Resources/Components/Shared/`
5. Crear subcarpeta `Resources/Validators/`

**Criterios de Aceptación**:
- ✅ 5 carpetas creadas correctamente

**Duración Estimada**: 5 minutos

---

### P10.3 — Crear Archivos .resx para Páginas (16 archivos)

**Objetivo**: Crear 16 archivos `.resx` (8 páginas × 2 culturas) con traducciones completas.

**Páginas**: Dashboard, Profile, History, Trends, Admin, Login, Error, Home

**Pasos por Página** (ejemplo Dashboard):
1. Crear `Dashboard.es-AR.resx` en `Resources/Pages/`
2. Agregar TODOS los strings del archivo `Dashboard.razor` como entries `<data name="...">`:
   - PageTitle
   - WelcomeBack (con placeholder {0})
   - CurrentWeight
   - WeeklyChange
   - GoalProgress
   - BMI
   - WeightEvolution
   - AddWeight
   - Export
   - NoDataMessage
   - AddFirstWeight
   - ... etc (todos los textos del componente)
3. Crear `Dashboard.en-US.resx` con traducciones al inglés
4. Repetir para las 7 páginas restantes

**Criterios de Aceptación**:
- ✅ 16 archivos .resx creados (8 páginas × 2 culturas)
- ✅ TODOS los strings hardcoded de las páginas tienen entry en .resx
- ✅ Traducciones completas en inglés (NO usar Google Translate crudo, revisar contexto)
- ✅ Placeholders correctos (ej: {0}, {1})

**Duración Estimada**: 4-5 horas (30-40 min por página, revisar contexto de cada string)

---

### P10.4 — Crear Archivos .resx para Componentes (14 archivos)

**Objetivo**: Crear 14 archivos `.resx` (7 componentes × 2 culturas).

**Componentes**: MainLayout, NavMenu, AddWeightDialog, StatsCard, TrendCard, WeightChart, NotificationBell

**Pasos** (igual que P10.3 pero para componentes):
1. Crear `.es-AR.resx` y `.en-US.resx` para cada componente
2. Extraer TODOS los strings hardcoded del componente
3. Agregar entries con nombres descriptivos
4. Traducir al inglés con contexto correcto

**Criterios de Aceptación**:
- ✅ 14 archivos .resx creados
- ✅ Strings completos y contextualizados

**Duración Estimada**: 2-3 horas

---

### P10.5 — Crear Archivos .resx para Validators (6 archivos)

**Objetivo**: Crear 6 archivos `.resx` (3 validators × 2 culturas).

**Validators**: CreateWeightLogValidator, UpdateWeightLogValidator, UpdateUserProfileValidator

**Pasos**:
1. Extraer mensajes de error hardcoded en cada validator
2. Crear `.es-AR.resx` y `.en-US.resx` con entries para cada mensaje
3. Asegurar placeholders correctos (ej: "must be between {0} and {1}")

**Criterios de Aceptación**:
- ✅ 6 archivos .resx creados
- ✅ Mensajes de validación traducidos correctamente

**Duración Estimada**: 1 hora

---

### P10.6 — Refactorizar Dashboard.razor + Dashboard.razor.cs

**Objetivo**: Reemplazar TODOS los strings hardcoded por `IStringLocalizer`.

**Pasos**:
1. Inyectar `IStringLocalizer<Dashboard>` en `Dashboard.razor.cs`
2. Crear properties para strings simples (ej: `private string PageTitle => Localizer["PageTitle"];`)
3. Crear métodos para strings con placeholders (ej: `GetWelcomeMessage()`)
4. Reemplazar TODOS los strings hardcoded en `Dashboard.razor` por uso de Localizer
5. Compilar y verificar que no hay errores
6. Probar manualmente cambiando idioma

**Criterios de Aceptación**:
- ✅ CERO strings hardcoded en Dashboard.razor (excepto clases CSS, icons, etc.)
- ✅ IStringLocalizer inyectado correctamente
- ✅ Build exitoso
- ✅ UI renderiza correctamente en español (default)

**Duración Estimada**: 1.5 horas

---

### P10.7 — Refactorizar Profile.razor + Profile.razor.cs

**Objetivo**: Igual que P10.6 pero para Profile.

**Duración Estimada**: 1.5 horas

---

### P10.8 — Refactorizar History.razor + History.razor.cs

**Objetivo**: Igual que P10.6 pero para History.

**Duración Estimada**: 1 hora

---

### P10.9 — Refactorizar Trends.razor + Trends.razor.cs

**Objetivo**: Igual que P10.6 pero para Trends.

**Duración Estimada**: 1.5 horas

---

### P10.10 — Refactorizar Admin.razor + Admin.razor.cs

**Objetivo**: Igual que P10.6 pero para Admin.

**Duración Estimada**: 2 horas (más strings que otras páginas)

---

### P10.11 — Refactorizar Login.razor + Login.razor.cs

**Objetivo**: Igual que P10.6 pero para Login.

**Duración Estimada**: 45 minutos

---

### P10.12 — Refactorizar Error.razor

**Objetivo**: Igual que P10.6 pero para Error (página sin code-behind complejo).

**Duración Estimada**: 30 minutos

---

### P10.13 — Refactorizar MainLayout.razor.cs + NavMenu.razor.cs

**Objetivo**: Refactorizar componentes de layout con IStringLocalizer.

**Duración Estimada**: 1 hora

---

### P10.14 — Refactorizar Componentes Compartidos

**Objetivo**: Refactorizar AddWeightDialog, StatsCard, TrendCard, WeightChart, NotificationBell.

**Duración Estimada**: 2 horas (5 componentes)

---

### P10.15 — Refactorizar Validators FluentValidation

**Objetivo**: Inyectar `IStringLocalizer` en constructores de validators y usar en `.WithMessage()`.

**Pasos**:
1. Agregar parámetro `IStringLocalizer<ValidatorName>` en constructor
2. Reemplazar strings hardcoded en `.WithMessage()` por `_localizer["Key", params]`
3. Registrar validators en DI (ya hecho en ServiceCollectionExtensions.cs)
4. Probar validación con cultura es-AR y en-US

**Criterios de Aceptación**:
- ✅ 3 validators refactorizados
- ✅ Mensajes de error se traducen según cultura

**Duración Estimada**: 1 hora

---

### P10.16 — Modificar LanguageSelector.razor.cs

**Objetivo**: Integrar cambio de `CultureInfo` + cookie persistente + `forceLoad` en `SelectLanguageAsync()`.

**Pasos**:
1. Inyectar `NavigationManager`
2. En `SelectLanguageAsync()`:
   - Mapear `es` → `es-AR`, `en` → `en-US`
   - Crear `CultureInfo` object
   - Cambiar `CultureInfo.CurrentCulture` y `CultureInfo.CurrentUICulture`
   - Persistir cookie con `CookieRequestCultureProvider.MakeCookieValue()`
   - Llamar `NavigationManager.NavigateTo(uri, forceLoad: true)`
3. Remover idiomas zh, fr, it (solo es/en en Fase 10)
4. Probar cambio de idioma y verificar reload completo

**Criterios de Aceptación**:
- ✅ Cambiar idioma recarga la página con nuevas strings
- ✅ Cookie persistida sobrevive a refresh

**Duración Estimada**: 1 hora

---

### P10.17 — Actualizar Meta Tags SEO Dinámicos

**Objetivo**: Hacer `PageTitle` y `meta description` dinámicos según cultura.

**Pasos**:
1. En cada página, cambiar `<PageTitle>` hardcoded por `@Localizer["PageTitle"]`
2. En `<HeadContent>`, cambiar `meta description` por `@Localizer["MetaDescription"]`
3. Agregar entries `PageTitle` y `MetaDescription` en cada `.resx`
4. Verificar que SEO tags cambian con idioma

**Criterios de Aceptación**:
- ✅ PageTitle dinámico en 8 páginas
- ✅ Meta description dinámica en 8 páginas

**Duración Estimada**: 1 hora

---

### P10.18 — Testing Manual: Cambio de Idioma

**Objetivo**: Verificar que TODO el UI cambia al seleccionar idioma en LanguageSelector.

**Pasos**:
1. Arrancar app en Development
2. Login como usuario (Marco admin)
3. Dashboard debe mostrar textos en español (default)
4. Cambiar idioma a "English (USA)" en LanguageSelector
5. Verificar que TODA la página se recarga en inglés:
   - Navbar (Dashboard, Profile, etc.)
   - Stats cards
   - Botones
   - Mensajes
6. Navegar a Profile, History, Trends, Admin → verificar inglés en todas
7. Cambiar de vuelta a "Español (ARG)" → verificar español en todas
8. Probar validación: intentar agregar peso inválido → mensaje de error debe estar en el idioma seleccionado
9. Probar Snackbar notifications → deben estar en idioma actual

**Criterios de Aceptación**:
- ✅ TODO el UI cambia al seleccionar idioma
- ✅ Navbar traducido
- ✅ Páginas traducidas (8/8)
- ✅ Componentes traducidos (7/7)
- ✅ Validaciones traducidas (3/3)
- ✅ Snackbar traducidos

**Duración Estimada**: 1 hora

---

### P10.19 — Verificar Persistencia Cross-Session

**Objetivo**: Verificar que idioma seleccionado sobrevive a refresh y cerrar/reabrir navegador.

**Pasos**:
1. Seleccionar "English (USA)"
2. Refresh página (F5) → debe mantener inglés
3. Navegar a otra página (ej: Profile) → debe mantener inglés
4. Cerrar navegador completamente
5. Reabrir navegador
6. Login nuevamente
7. Verificar que Dashboard carga en inglés (idioma persistido)

**Criterios de Aceptación**:
- ✅ Idioma sobrevive a refresh
- ✅ Idioma sobrevive a cerrar/reabrir navegador
- ✅ Cookie persistente funciona correctamente

**Duración Estimada**: 30 minutos

---

### P10.20 — Build Final + Tests + Commit + Push

**Objetivo**: Verificar build final, ejecutar tests, documentar, commit y push.

**Pasos**:
1. Ejecutar `dotnet build` → debe compilar sin errores
2. Ejecutar `dotnet test` → 176/176 tests deben pasar (validar que refactorización NO rompió nada)
3. Actualizar `README.md` con sección i18n:
   ```markdown
   ## 🌍 Internacionalización (i18n)
   
   La aplicación soporta múltiples idiomas:
   - 🇦🇷 Español (Argentina) — Default
   - 🇺🇸 English (United States)
   
   Cambiar idioma: Click en el selector de idioma en el navbar.
   ```
4. Actualizar `ARCHITECTURE.md` con sección Localization Pattern
5. Git commit con mensaje descriptivo:
   ```
   feat(fase-10): complete multilanguage support (es-AR/en-US)
   
   - Add ASP.NET Core localization (RequestLocalization middleware)
   - Create 36 .resx files (16 pages + 14 components + 6 validators)
   - Refactor ALL pages/components to use IStringLocalizer
   - Integrate LanguageSelector with CultureInfo + cookie persistence
   - Dynamic SEO meta tags (PageTitle, description) per culture
   - FluentValidation messages translated
   - Cross-session persistence via cookie
   - forceLoad NavigationManager for instant language change
   
   BREAKING CHANGE: LanguageSelector now only supports es-AR/en-US
   (removed zh-CN, fr-FR, it-IT for Phase 10 scope)
   ```
6. Git push a `origin feature/fase-10-globalizacion`

**Criterios de Aceptación**:
- ✅ Build exitoso sin errores
- ✅ Tests 176/176 passing
- ✅ Documentación actualizada (README.md, ARCHITECTURE.md)
- ✅ Commit descriptivo pushed

**Duración Estimada**: 1 hora

---

## Tabla de Progreso

| ID | Tarea | Duración | % | Estado |
|---:|-------|----------|--:|:------:|
| P10.1 | Configurar localization en Program.cs | 30 min | 100% | ✅ |
| P10.2 | Crear estructura Resources/ | 5 min | 100% | ✅ |
| P10.3 | Crear .resx páginas (16 archivos) | 4-5 h | 100% | ✅ |
| P10.4 | Crear .resx componentes (14 archivos) | 2-3 h | 100% | ✅ |
| P10.5 | Crear .resx validators (6 archivos) | 1 h | 100% | ✅ |
| P10.6 | Refactorizar Dashboard con IStringLocalizer | 1.5 h | 100% | ✅ |
| P10.7 | Refactorizar Profile con IStringLocalizer | 1.5 h | 0% | 🔵 |
| P10.8 | Refactorizar History con IStringLocalizer | 1 h | 0% | 🔵 |
| P10.9 | Refactorizar Trends con IStringLocalizer | 1.5 h | 0% | 🔵 |
| P10.10 | Refactorizar Admin con IStringLocalizer | 2 h | 0% | 🔵 |
| P10.11 | Refactorizar Login con IStringLocalizer | 45 min | 0% | 🔵 |
| P10.12 | Refactorizar Error con IStringLocalizer | 30 min | 0% | 🔵 |
| P10.13 | Refactorizar MainLayout + NavMenu | 1 h | 0% | 🔵 |
| P10.14 | Refactorizar componentes compartidos | 2 h | 0% | 🔵 |
| P10.15 | Refactorizar validators FluentValidation | 1 h | 0% | 🔵 |
| P10.16 | Modificar LanguageSelector (CultureInfo + cookie) | 1 h | 0% | 🔵 |
| P10.17 | Actualizar meta tags SEO dinámicos | 1 h | 0% | 🔵 |
| P10.18 | Testing manual cambio idioma | 1 h | 0% | 🔵 |
| P10.19 | Verificar persistencia cross-session | 30 min | 0% | 🔵 |
| P10.20 | Build + tests + commit + push | 1 h | 0% | 🔵 |

**Total**: 20 tareas | **Progreso**: 6/20 completadas (30%) | **Duración**: ~26-28 horas (4-6 días de trabajo)

---

## Estrategia de Traducción

### Herramientas Recomendadas

1. **Google Translate** (baseline) — solo como punto de partida
2. **DeepL Translator** — mejor calidad que Google para es↔en
3. **Context.Reverso.net** — ejemplos de uso en contexto real
4. **Revisión Manual** — SIEMPRE revisar y ajustar según contexto de UI

### Guía de Estilo

**Español (es-AR)**:
- Tuteo (vos): "Bienvenido" (no vosotros)
- Formato fecha: DD/MM/YYYY
- Separador decimal: coma (1,5 kg)
- Términos específicos: "Peso" (no "masa"), "Registros" (no "logs")

**English (en-US)**:
- Informal tone: "Welcome back!" (not "Salutations")
- Date format: MM/DD/YYYY
- Decimal separator: dot (1.5 lbs)
- Specific terms: "Weight Log" (not "Weight Entry"), "Dashboard" (not "Control Panel")

### Placeholders

- Usar `{0}`, `{1}`, etc. para parámetros dinámicos
- **NO** traducir el placeholder number: siempre `{0}` (no `{1}` en traducción)
- Ejemplo correcto:
  - es-AR: `"Bienvenido de nuevo, {0}!"`
  - en-US: `"Welcome back, {0}!"`

---

## Checklist Pre-Merge

Antes de crear PR de Fase 10 a `develop`, verificar:

- [ ] Build exitoso sin errores: `dotnet build`
- [ ] Tests pasando 176/176: `dotnet test`
- [ ] CERO strings hardcoded en páginas (8/8 páginas refactorizadas)
- [ ] CERO strings hardcoded en componentes (7/7 componentes refactorizados)
- [ ] Validators con mensajes traducidos (3/3 validators refactorizados)
- [ ] LanguageSelector cambia idioma + forceLoad funciona
- [ ] Cookie persistente sobrevive a refresh + cerrar/reabrir navegador
- [ ] Meta tags SEO dinámicos (PageTitle + description)
- [ ] README.md actualizado con sección i18n
- [ ] ARCHITECTURE.md actualizado con Localization Pattern
- [ ] Commit descriptivo con BREAKING CHANGE note (removed zh/fr/it)
- [ ] Pushed a `origin feature/fase-10-globalizacion`

---

## Riesgos y Mitigaciones

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|--------------|---------|------------|
| Traducciones incorrectas por contexto | Alta | Medio | Revisión manual de TODAS las traducciones antes de commit final |
| Olvidar reemplazar algún string hardcoded | Media | Bajo | Búsqueda global de strings en español antes de P10.20 |
| Cookie no persiste correctamente | Baja | Alto | Testing exhaustivo en P10.19 (cross-browser: Chrome, Edge, Firefox) |
| forceLoad rompe estado de componentes | Media | Medio | Verificar que NO hay estado crítico que se pierda en reload |
| Build broken después de refactorización | Baja | Alto | Compilar después de CADA página refactorizada (P10.6-P10.14) |
| Tests rotos por cambio de mensajes | Baja | Alto | Ejecutar `dotnet test` después de P10.15 (validators) |

---

## Referencias

### Documentación Oficial

- [ASP.NET Core Globalization and localization](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization)
- [IStringLocalizer Interface](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.localization.istringlocalizer)
- [RequestLocalization Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-9.0#localization-middleware)
- [FluentValidation with Localization](https://docs.fluentvalidation.net/en/latest/localization.html)
- [CultureInfo Class](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo)

### Herramientas

- [DeepL Translator](https://www.deepl.com/translator)
- [Reverso Context](https://context.reverso.net/)
- [ResX Resource Manager (VS Extension)](https://marketplace.visualstudio.com/items?itemName=TomEnglert.ResXManager)

---

## Próximos Pasos Post-Fase 10

1. **Fase 11 (opcional)**: Agregar más idiomas (fr-FR, it-IT, zh-CN) si hay demanda
2. **Fase 12 (opcional)**: Migrar a SQLite → SQL Server (preparado en Fase 1)
3. **v2.0**: API REST pública + App móvil nativa

---

**Última actualización**: 2026-02-19 22:30

**Responsable**: Equipo ControlPeso.Thiscloud

**Estado**: 🟡 **EN PROGRESO** — 3/20 tareas completadas (15%)

---

## 📊 Resumen de Progreso

### ✅ Tareas Completadas (5/20)

**P10.1 — Configurar Localization en Program.cs** ✅
- ✅ Localization services registrados (`AddLocalization` con `ResourcesPath="Resources"`)
- ✅ `RequestLocalizationOptions` configuradas (es-AR default, en-US soporte)
- ✅ Middleware `UseRequestLocalization` agregado en orden correcto
- ✅ Build exitoso sin errores
- 📦 Commits: `2f0e58b`

**P10.2 — Crear Estructura de Carpetas Resources/** ✅
- ✅ 5 carpetas creadas: `Resources/`, `Pages/`, `Components/Layout/`, `Components/Shared/`, `Validators/`
- ✅ 4 archivos `.gitkeep` para trackeo en Git
- 📦 Commits: `d61dc7f`

**P10.3 — Crear Archivos .resx para Páginas (16 archivos)** ✅
- ✅ 16 archivos `.resx` creados (8 páginas × 2 culturas)
- ✅ Páginas cubiertas: Dashboard (38 strings), Login (15 strings), Error (7 strings), Home (9 strings), Profile (16 strings), History (8 strings), Trends (8 strings), Admin (10 strings)
- ✅ Traducciones inglés contextuales (NO Google Translate crudo)
- ✅ Placeholders correctos preservados (`{0}`, `{1}`)
- ✅ XML entities escapados (`&amp;` para `&`)
- ✅ Build exitoso sin errores
- 📦 Commits: `f14e861`, `a5d6b10`

**P10.4 — Crear Archivos .resx para Componentes (14 archivos)** ✅
- ✅ 14 archivos `.resx` creados (7 componentes × 2 culturas)
- ✅ Componentes cubiertas: MainLayout (9 strings), NavMenu (11 strings), AddWeightDialog (9 strings), StatsCard (placeholder), TrendCard (placeholder), WeightChart (1 string), NotificationBell (1 string)
- ✅ Traducciones inglés contextuales
- ✅ Aria labels para accesibilidad incluidos
- ✅ Build exitoso sin errores
- 📦 Commits: `ebf55f8`

**P10.5 — Crear Archivos .resx para Validators (6 archivos)** ✅
- ✅ 6 archivos `.resx` creados (3 validators × 2 culturas)
- ✅ Validators cubiertos: CreateWeightLogValidator (8 messages), UpdateWeightLogValidator (7 messages), UpdateUserProfileValidator (12 messages)
- ✅ 27 mensajes de validación traducidos con placeholders
- ✅ Range validations (Weight 20-500kg, Height 50-300cm)
- ✅ Date validations (future, 150 years limit)
- ✅ Enum validations (DisplayUnit, UnitSystem, Language)
- ✅ Build exitoso sin errores
- 📦 Commits: `1be0bc2`

**Total Strings en .resx**: ~169 entries (páginas + componentes + validators) | **Total Archivos Creados**: 40 archivos (4 .gitkeep + 36 .resx)

### 🔵 Tareas Pendientes (15/20)

**Próxima Tarea**: P10.6 — Refactorizar Dashboard con IStringLocalizer
- Inyectar `IStringLocalizer<Dashboard>` en code-behind
- Reemplazar TODOS los strings hardcoded por localizer keys
- Crear properties/métodos para acceso limpio desde markup
- Duración estimada: 1.5 horas

### 📈 Métricas de Progreso

| Métrica | Valor | Progreso |
|---------|-------|----------|
| **Tareas Completadas** | 5/20 | 25% |
| **Archivos .resx Creados** | 36/36 | 100% ✅ |
| **Strings Traducidos** | ~169/280 | ~60% |
| **Commits** | 7 | - |
| **Build Status** | ✅ Passing | 100% |
| **Tiempo Invertido** | ~3 horas | - |

### 🎯 Siguiente Hito

**Completar P10.6-P10.14** → Todos los componentes refactorizados con IStringLocalizer (9 tareas)
