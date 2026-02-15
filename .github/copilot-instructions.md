# Copilot Instructions — Control Peso Thiscloud

> **Proyecto**: ControlPeso.Thiscloud
> **Archivo**: `.github/copilot-instructions.md`
> **Modelo Copilot**: Claude Sonnet 4.5 (modo agente)
> **Última actualización**: 2026-02-15

---

## Contexto del Proyecto

Este es un proyecto **Blazor Server (.NET 9)** con **MudBlazor** como framework de UI exclusivo. La aplicación "Control Peso Thiscloud" permite a los usuarios registrar, monitorear y analizar su peso corporal con tendencias, historial y panel de administración.

### Stack Tecnológico (DECISIÓN CERRADA)

- **Runtime**: .NET 9 (`net9.0`)
- **UI Framework**: Blazor Server + MudBlazor 8.0.0
- **ORM**: Entity Framework Core 9.0.1, modo **Database First**
- **Base de datos**: SQLite (desarrollo/MVP) — preparado para migrar a SQL Server
- **Autenticación**: Google OAuth 2.0 (ASP.NET Core Identity + Google provider)
- **Analytics**: Google Analytics 4 (gtag.js) + Cloudflare Analytics (capa gratuita)
- **IDE**: Visual Studio 2022+ con GitHub Copilot (Claude Sonnet 4.5 agente)

### Arquitectura (Onion / Cebolla) — OBLIGATORIA

```
ControlPeso.Thiscloud.sln
│
├── src/
│   ├── ControlPeso.Domain/              ← Entidades (scaffolded), enums, interfaces de dominio
│   ├── ControlPeso.Application/         ← Servicios, DTOs, interfaces de aplicación
│   ├── ControlPeso.Infrastructure/      ← EF Core DbContext (scaffolded), repos, servicios externos
│   └── ControlPeso.Web/                 ← Blazor Server, Pages, Components, Layout
│
├── tests/
│   ├── ControlPeso.Domain.Tests/
│   ├── ControlPeso.Application.Tests/
│   └── ControlPeso.Infrastructure.Tests/
│
└── docs/
    └── schema/
        └── schema_v1.sql               ← CONTRATO MAESTRO de datos
```

### Reglas de dependencia (INQUEBRANTABLE)

```
Domain ← NO depende de NADA externo (zero dependencies)
Application ← Depende SOLO de Domain
Infrastructure ← Depende de Domain + Application
Web ← Depende de Application + Infrastructure (solo para DI registration)
```

- **Domain** NO referencia EF Core, MudBlazor, ASP.NET Core ni ningún paquete externo.
- **Application** define interfaces (`IWeightLogService`, `IUserService`, etc.) que Infrastructure implementa.
- **Infrastructure** implementa las interfaces con EF Core, Google Auth, etc.
- **Web** solo consume interfaces de Application vía DI. NUNCA accede a Infrastructure directamente desde componentes

---

## 🚨 REGLAS NO NEGOCIABLES

### Arquitectura

1. ✅ Respetar arquitectura Onion estrictamente:
   - **Domain**: ZERO dependencias, solo entidades scaffolded + enums/exceptions manuales
   - **Application**: depende SOLO de Domain
   - **Infrastructure**: depende de Domain + Application
   - **Web**: depende de Application + Infrastructure
2. ✅ SOLID en todas las capas
3. ✅ Programación por interfaces (Application define interfaces, Infrastructure/Web implementan)
4. ❌ PROHIBIDO acceder a `DbContext` desde Web
5. ❌ PROHIBIDO lógica de negocio en componentes `.razor` o `.razor.cs`
6. ❌ PROHIBIDO exponer entidades scaffolded a UI (usar DTOs siempre)

### Blazor Components (MANDATORIO)

7. ❌ **PROHIBIDO** bloques `@code { }` en archivos `.razor`
8. ✅ **OBLIGATORIO** patrón code-behind:
   - `ComponentName.razor` → Solo markup (HTML/Razor)
   - `ComponentName.razor.cs` → `partial class ComponentName` con toda la lógica C#
9. ✅ Ejemplo correcto:
   ```razor
   @* AddWeightDialog.razor *@
   <MudDialog>
       <DialogContent>
           <MudTextField @bind-Value="Weight" Label="Peso" />
       </DialogContent>
   </MudDialog>
   ```
   ```csharp
   // AddWeightDialog.razor.cs
   namespace ControlPeso.Web.Components.Shared;

   public partial class AddWeightDialog
   {
       [Parameter] public double Weight { get; set; }

       private void OnSave()
       {
           // Lógica aquí
       }
   }
   ```

### Database First (SQL como Contrato Maestro)

10. ❌ PROHIBIDO modificar entidades scaffolded manualmente
11. ❌ PROHIBIDO agregar Data Annotations a entidades scaffolded
12. ❌ PROHIBIDO migrations code-first
13. ✅ TODO cambio de esquema EMPIEZA en `docs/schema/schema_v1.sql`
14. ✅ Flujo obligatorio: SQL → Aplicar → Scaffold → (Ajustar value converters si necesario)
15. ✅ Enums se crean manualmente en `Domain/Enums/` (mapean los INTEGER del SQL)
16. ✅ SQL define: tipos, restricciones, defaults, CHECKs, FKs, índices (TODO)

**Filosofía Database First**: El **esquema SQL es la fuente de verdad** absoluta. Todo el gobierno de datos vive en SQL:
- Tipos de datos, precisión, longitudes → DDL
- Restricciones NOT NULL, UNIQUE, CHECK → DDL
- Defaults → DDL
- Foreign Keys, ON DELETE → DDL
- Índices → DDL

**Flujo de trabajo**:
```
1. Modificar docs/schema/schema_v1.sql
     ↓
2. Aplicar SQL: sqlite3 controlpeso.db < schema_v1.sql
     ↓
3. Scaffold EF Core:
   dotnet ef dbcontext scaffold "Data Source=../../controlpeso.db" \
     Microsoft.EntityFrameworkCore.Sqlite \
     --context ControlPesoDbContext \
     --output-dir ../ControlPeso.Domain/Entities \
     --context-dir . \
     --force
     ↓
4. DbContext queda en Infrastructure, entidades en Domain/Entities
5. Ajustes post-scaffold solo para value converters (Guid, DateTime, enums)
6. NO tocar entidades manualmente — el SQL ya tiene todo
```

### MudBlazor (UI Framework Exclusivo)

17. ❌ PROHIBIDO HTML crudo (`<input>`, `<button>`, `<table>`, etc.) cuando existe `Mud*` equivalente
18. ✅ Usar SIEMPRE componentes MudBlazor:
    - `MudTextField` en vez de `<input>`
    - `MudButton` en vez de `<button>`
    - `MudDataGrid` en vez de `<table>`
    - `MudCard` para contenedores
    - `MudDialog` para modales
    - `MudSnackbar` para notificaciones
19. ✅ Tema oscuro configurado en `ControlPeso.Web.Theme.ControlPesoTheme.DarkTheme`
20. ✅ Providers obligatorios en `Routes.razor`: `MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider`

### Seguridad

21. ❌ PROHIBIDO hardcodear secretos o connection strings
22. ❌ PROHIBIDO `try/catch` vacíos
23. ❌ PROHIBIDO loguear tokens, passwords, PII
24. ❌ PROHIBIDO almacenar contraseñas (auth solo Google)
25. ✅ Secretos en User Secrets (dev) o env vars (prod)
26. ✅ TODO catch DEBE loguear la excepción
27. ✅ Cookie: `HttpOnly + Secure + SameSite=Strict`
28. ✅ CSP headers restrictivos

### Código C#

29. ❌ NO usar `public` por defecto → Regla de mínima exposición: `private` > `internal` > `protected` > `public`
30. ❌ NO modificar código auto-generado (`*.g.cs`, `// <auto-generated>`)
31. ❌ NO agregar métodos/parámetros sin usar
32. ✅ Null checks: `ArgumentNullException.ThrowIfNull(x)` para objetos, `string.IsNullOrWhiteSpace(x)` para strings
33. ✅ Excepciones precisas (`ArgumentException`, `InvalidOperationException`, NO `Exception` genérica)
34. ✅ Async end-to-end: métodos async terminan en `Async`, aceptan `CancellationToken`
35. ✅ `ConfigureAwait(false)` en helpers/libraries
36. ✅ Comentarios explican **por qué**, NO qué hace el código
37. ✅ **Nullable enabled** en todos los proyectos: `<Nullable>enable</Nullable>`
38. ✅ **Implicit usings** habilitado: `<ImplicitUsings>enable</ImplicitUsings>`
39. ✅ Usar **records** para DTOs y value objects cuando aporte inmutabilidad
40. ✅ Usar **primary constructors** donde sea apropiado
41. ✅ Prefijo `I` para interfaces: `IWeightLogService`, `IUserRepository`
42. ✅ Sufijo `Dto` para DTOs: `WeightLogDto`, `UserProfileDto`
43. ✅ Sufijo `Service` para servicios de aplicación: `WeightLogService`

### Blazor + MudBlazor (Detalles Adicionales)

44. ✅ Componentes `.razor` pequeños y cohesivos (máximo ~150 líneas)
45. ✅ Lógica compleja en **code-behind** (`.razor.cs`) o servicios inyectados
46. ✅ Estados UI explícitos: Loading → Empty → Error → Success
47. ✅ **Virtualización** (`MudVirtualize`) para listas largas
48. ✅ **`@key`** en loops de componentes para optimizar re-renderizado
49. ❌ Evitar `StateHasChanged()` manual salvo necesidad justificada
50. ✅ Formularios con `MudForm` + `MudTextField` + validación FluentValidation
51. ✅ Diálogos con `IDialogService` de MudBlazor
52. ✅ Snackbars con `ISnackbar` para notificaciones
53. ✅ Navegación con `MudNavMenu` + `MudNavLink`

### Entity Framework Core (Database First)

54. ✅ Modo **Database First**: el esquema SQL es el contrato maestro
55. ✅ `DbContext` en **Infrastructure** únicamente
56. ✅ Entidades scaffolded en **Domain/Entities** — no se modifican manualmente
57. ✅ **AsNoTracking()** para consultas de solo lectura
58. ✅ **Proyecciones selectivas** (`.Select()`) para evitar over-fetching
59. ✅ Conexión SQLite en Development: `Data Source=controlpeso.db`
60. ✅ Preparado para swap a SQL Server: solo cambiar connection string + provider
61. ❌ **No** exponer `DbContext` fuera de Infrastructure
62. ❌ **No** usar migrations code-first — los cambios se hacen en SQL y se re-scaffold

### Manejo de Errores

63. ✅ Middleware global de excepciones en Web
64. ❌ **No** `try/catch` vacíos (PROHIBIDO)
65. ✅ Logging estructurado con `ILogger<T>`
66. ✅ Excepciones de dominio tipadas: `DomainException`, `NotFoundException`, `ValidationException`
67. ✅ En Blazor: `ErrorBoundary` para errores de componentes

### Validación y Lógica de Negocio

68. ✅ FluentValidation para DTOs de entrada
69. ✅ Validación en Application layer, NO en Web
70. ✅ Mappers manuales en `Application/Mapping/` (conversiones string→Guid, string→DateTime, int→enum)
71. ✅ TODO peso almacenado SIEMPRE en **kg** (conversión a lb solo en display)

### Testing

72. ✅ xUnit + Moq/NSubstitute
73. ✅ Proyecto de test separado por capa (`*.Tests`)
74. ✅ Nombre de tests: `WhenConditionThenExpectedBehavior` o `Metodo_Escenario_ResultadoEsperado`
75. ✅ Patrón AAA (Arrange-Act-Assert)
76. ✅ NO branching/conditionals en tests
77. ✅ Tests deben poder correr en cualquier orden o en paralelo

### NuGet y Versiones

78. ✅ Central Package Management (`Directory.Packages.props`)
79. ✅ NO especificar `Version` en `PackageReference` de .csproj
80. ✅ Versiones exactas en `Directory.Packages.props`

### Git y Documentación

81. ❌ PROHIBIDO commits directos a `main` o `develop`
82. ✅ Git Flow: `main` → `develop` → `feature/*`
83. ✅ PR obligatorio con CI verde
84. ✅ Plan `ControlPeso/docs/Plan_ControlPeso_Thiscloud_v1_0.md` es contractual → actualizar ANTES de marcar tareas "Done"

---

## Modelo de Datos — SQL como Contrato Maestro

> Las entidades C# se generan por scaffold. Abajo se documenta la estructura esperada
> después del scaffold para referencia de Copilot.

### Entidades esperadas post-scaffold (Domain/Entities)

#### User (scaffolded desde tabla `Users`)

```csharp
// GENERADO POR SCAFFOLD — NO MODIFICAR MANUALMENTE
// Cambios de estructura → modificar SQL → re-scaffold
public class User
{
    public string Id { get; set; } = null!;          // TEXT NOT NULL PK (GUID como texto en SQLite)
    public string GoogleId { get; set; } = null!;    // TEXT NOT NULL UNIQUE
    public string Name { get; set; } = null!;        // TEXT NOT NULL, max 200
    public string Email { get; set; } = null!;       // TEXT NOT NULL UNIQUE, max 320
    public int Role { get; set; }                     // INTEGER NOT NULL DEFAULT 0 CHECK(0..1)
    public string? AvatarUrl { get; set; }           // TEXT NULL, max 2048
    public string MemberSince { get; set; } = null!; // TEXT NOT NULL (ISO 8601 datetime)
    public double Height { get; set; }                // REAL NOT NULL DEFAULT 170.0 CHECK(50..300)
    public int UnitSystem { get; set; }               // INTEGER NOT NULL DEFAULT 0 CHECK(0..1)
    public string? DateOfBirth { get; set; }         // TEXT NULL (ISO 8601 date)
    public string Language { get; set; } = null!;    // TEXT NOT NULL DEFAULT 'es', max 10
    public int Status { get; set; }                   // INTEGER NOT NULL DEFAULT 0 CHECK(0..2)
    public double? GoalWeight { get; set; }          // REAL NULL CHECK(20..500)
    public double? StartingWeight { get; set; }      // REAL NULL CHECK(20..500)
    public string CreatedAt { get; set; } = null!;   // TEXT NOT NULL (ISO 8601 datetime)
    public string UpdatedAt { get; set; } = null!;   // TEXT NOT NULL (ISO 8601 datetime)

    // Navigation properties (generadas por scaffold)
    public virtual ICollection<WeightLog> WeightLogs { get; set; } = [];
    public virtual UserPreference? UserPreference { get; set; }
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = [];
}
```

#### WeightLog (scaffolded desde tabla `WeightLogs`)

```csharp
// GENERADO POR SCAFFOLD — NO MODIFICAR MANUALMENTE
public class WeightLog
{
    public string Id { get; set; } = null!;           // TEXT NOT NULL PK
    public string UserId { get; set; } = null!;       // TEXT NOT NULL FK → Users(Id)
    public string Date { get; set; } = null!;         // TEXT NOT NULL (YYYY-MM-DD)
    public string Time { get; set; } = null!;         // TEXT NOT NULL (HH:MM)
    public double Weight { get; set; }                 // REAL NOT NULL CHECK(20..500) — kg siempre
    public int DisplayUnit { get; set; }               // INTEGER NOT NULL DEFAULT 0 CHECK(0..1)
    public string? Note { get; set; }                 // TEXT NULL, max 500
    public int Trend { get; set; }                     // INTEGER NOT NULL DEFAULT 2 CHECK(0..2)
    public string CreatedAt { get; set; } = null!;    // TEXT NOT NULL (ISO 8601 datetime)

    // Navigation
    public virtual User User { get; set; } = null!;
}
```

#### UserPreference (scaffolded desde tabla `UserPreferences`)

```csharp
// GENERADO POR SCAFFOLD — NO MODIFICAR MANUALMENTE
public class UserPreference
{
    public string Id { get; set; } = null!;              // TEXT NOT NULL PK
    public string UserId { get; set; } = null!;          // TEXT NOT NULL UNIQUE FK → Users(Id)
    public long DarkMode { get; set; }                    // INTEGER NOT NULL DEFAULT 1 (bool)
    public long NotificationsEnabled { get; set; }        // INTEGER NOT NULL DEFAULT 1 (bool)
    public string TimeZone { get; set; } = null!;        // TEXT NOT NULL DEFAULT 'America/Argentina/Buenos_Aires', max 100
    public string UpdatedAt { get; set; } = null!;       // TEXT NOT NULL (ISO 8601 datetime)

    // Navigation
    public virtual User User { get; set; } = null!;
}
```

#### AuditLog (scaffolded desde tabla `AuditLog`)

```csharp
// GENERADO POR SCAFFOLD — NO MODIFICAR MANUALMENTE
public class AuditLog
{
    public string Id { get; set; } = null!;             // TEXT NOT NULL PK
    public string UserId { get; set; } = null!;         // TEXT NOT NULL FK → Users(Id)
    public string Action { get; set; } = null!;         // TEXT NOT NULL, max 100
    public string EntityType { get; set; } = null!;     // TEXT NOT NULL, max 100
    public string EntityId { get; set; } = null!;       // TEXT NOT NULL
    public string? OldValue { get; set; }              // TEXT NULL (JSON snapshot)
    public string? NewValue { get; set; }              // TEXT NULL (JSON snapshot)
    public string CreatedAt { get; set; } = null!;     // TEXT NOT NULL (ISO 8601 datetime)

    // Navigation
    public virtual User User { get; set; } = null!;
}
```

### Enums (Domain/Enums/) — estos NO son scaffolded

```csharp
// Mapean los INTEGER con CHECK del SQL
public enum UserRole       { User = 0, Administrator = 1 }
public enum UserStatus     { Active = 0, Inactive = 1, Pending = 2 }
public enum UnitSystem     { Metric = 0, Imperial = 1 }
public enum WeightUnit     { Kg = 0, Lb = 1 }
public enum WeightTrend    { Up = 0, Down = 1, Neutral = 2 }
```

### Conversiones en Application Layer

Dado que SQLite + scaffold genera `int`/`long` para enums y `string` para GUIDs/fechas, la **capa Application** se encarga de las conversiones en los servicios:

```csharp
// Ejemplo: mapeo de entidad scaffolded a DTO tipado
public WeightLogDto MapToDto(WeightLog entity) => new()
{
    Id = Guid.Parse(entity.Id),
    UserId = Guid.Parse(entity.UserId),
    Date = DateOnly.Parse(entity.Date),
    Time = TimeOnly.Parse(entity.Time),
    Weight = (decimal)entity.Weight,
    DisplayUnit = (WeightUnit)entity.DisplayUnit,
    Note = entity.Note,
    Trend = (WeightTrend)entity.Trend,
    CreatedAt = DateTime.Parse(entity.CreatedAt)
};
```

> **Alternativa aceptable**: configurar value converters en Fluent API (`OnModelCreating`) post-scaffold para mapeos automáticos de Guid, DateTime, DateOnly, TimeOnly y enums.

---

## Interfaces de Servicio (Application)

```csharp
public interface IWeightLogService
{
    Task<WeightLogDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<WeightLogDto>> GetByUserAsync(Guid userId, WeightLogFilter filter, CancellationToken ct = default);
    Task<WeightLogDto> CreateAsync(CreateWeightLogDto dto, CancellationToken ct = default);
    Task<WeightLogDto> UpdateAsync(Guid id, UpdateWeightLogDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<WeightStatsDto> GetStatsAsync(Guid userId, DateRange range, CancellationToken ct = default);
}

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserDto?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default);
    Task<UserDto> CreateOrUpdateFromGoogleAsync(GoogleUserInfo info, CancellationToken ct = default);
    Task<UserDto> UpdateProfileAsync(Guid id, UpdateUserProfileDto dto, CancellationToken ct = default);
    Task<PagedResult<UserDto>> GetAllAsync(UserFilter filter, CancellationToken ct = default);
}

public interface ITrendService
{
    Task<TrendAnalysisDto> GetTrendAnalysisAsync(Guid userId, DateRange range, CancellationToken ct = default);
    Task<WeightProjectionDto> GetProjectionAsync(Guid userId, CancellationToken ct = default);
}

public interface IAdminService
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken ct = default);
    Task<PagedResult<UserDto>> GetUsersAsync(UserFilter filter, CancellationToken ct = default);
    Task UpdateUserRoleAsync(Guid userId, UserRole role, CancellationToken ct = default);
    Task UpdateUserStatusAsync(Guid userId, UserStatus status, CancellationToken ct = default);
}
```

---

## Estructura de Páginas Blazor (Web)

```
Pages/
├── Login.razor                    ← Autenticación Google
├── Dashboard.razor                ← Vista principal con métricas y gráfico
├── Profile.razor                  ← Perfil de usuario + configuración
├── Trends.razor                   ← Análisis de tendencias + proyecciones
├── History.razor                  ← Historial de registros con búsqueda/filtros
├── Admin.razor                    ← Panel de administración (solo Administrator)
└── Error.razor                    ← Página de error

Components/
├── Layout/
│   ├── MainLayout.razor
│   ├── MainLayout.razor.cs        ← partial class MainLayout
│   ├── NavMenu.razor
│   └── NavMenu.razor.cs           ← partial class NavMenu
├── Shared/
│   ├── AddWeightDialog.razor
│   ├── AddWeightDialog.razor.cs   ← partial class AddWeightDialog
│   ├── WeightChart.razor
│   ├── WeightChart.razor.cs       ← partial class WeightChart
│   ├── StatsCard.razor
│   ├── StatsCard.razor.cs         ← partial class StatsCard
│   ├── WeightTable.razor
│   ├── WeightTable.razor.cs       ← partial class WeightTable
│   ├── TrendCard.razor
│   ├── TrendCard.razor.cs         ← partial class TrendCard
│   ├── UserCard.razor
│   ├── UserCard.razor.cs          ← partial class UserCard
│   ├── LanguageSelector.razor
│   ├── LanguageSelector.razor.cs  ← partial class LanguageSelector
│   ├── NotificationBell.razor
│   └── NotificationBell.razor.cs  ← partial class NotificationBell
└── _Imports.razor
```

---

## Patrones de Respuesta para Copilot

### Cuando generes código nuevo:

1. Verifica en qué capa pertenece (Domain/Application/Infrastructure/Web)
2. Respeta las dependencias entre capas
3. Usa las interfaces existentes, no crees accesos directos
4. Incluye `CancellationToken` en métodos async
5. Usa componentes MudBlazor — NUNCA HTML crudo para lo que MudBlazor cubre
6. Incluye manejo de estados (Loading/Error/Empty/Success) en componentes Blazor
7. Agrega validación de entrada en DTOs (FluentValidation)
8. Incluye logging con `ILogger<T>`
9. **NUNCA modifiques entidades scaffolded** — si necesitás cambiar estructura, indicá el cambio SQL
10. **SIEMPRE usa code-behind** (.razor.cs) — NO bloques `@code { }`

### Cuando modifiques código existente:

1. No rompas la arquitectura de capas
2. Mantén las interfaces estables
3. No introduzcas dependencias prohibidas (ej: EF Core en Domain)
4. Mantén los tests existentes pasando
5. Si necesitás cambiar el modelo de datos → propone el cambio SQL primero
6. Respeta el patrón code-behind existente

### Cuando propongas cambios al modelo de datos:

1. **Primero** escribí el ALTER TABLE / CREATE TABLE en SQL
2. **Segundo** indicá que hay que re-scaffold
3. **Tercero** actualizá los DTOs y servicios en Application
4. **NUNCA** modifiques las entidades scaffolded directamente

### Cuando generes tests:

1. Usa **xUnit** como framework
2. Patrón Arrange-Act-Assert
3. Nombres descriptivos: `Metodo_Escenario_ResultadoEsperado`
4. Mock con **Moq** o **NSubstitute** para dependencias
5. No testear implementaciones internas, testear comportamiento

---

## SEO y Accesibilidad

- Todas las páginas deben tener `<PageTitle>` con título descriptivo
- Usar `<HeadContent>` para meta tags (description, og:title, og:image, etc.)
- Componentes MudBlazor con `aria-label` donde sea necesario
- Contraste WCAG AA mínimo en tema personalizado
- Navegación por teclado funcional
- `alt` text en imágenes

---

## Internacionalización (i18n)

- Soporte bilingüe: **Español (es)** y **Inglés (en)**
- Usar `IStringLocalizer<T>` de ASP.NET Core
- Archivos de recursos: `Resources/Pages/NombrePagina.es.resx` y `.en.resx`
- El idioma se selecciona en Login y persiste en perfil de usuario
- Formato de números y fechas según cultura seleccionada

---

## Prohibiciones (NUNCA hacer esto)

1. ❌ **NO** usar `@code { }` bloques en archivos `.razor` (usar `.razor.cs` code-behind)
2. ❌ **NO** usar HTML crudo cuando MudBlazor tiene componente equivalente
3. ❌ **NO** acceder a `DbContext` desde Web layer
4. ❌ **NO** poner lógica de negocio en componentes `.razor` o `.razor.cs`
5. ❌ **NO** hardcodear strings de conexión o secretos
6. ❌ **NO** usar `try/catch` vacíos
7. ❌ **NO** ignorar `CancellationToken` en operaciones async
8. ❌ **NO** crear dependencias circulares entre capas
9. ❌ **NO** usar JavaScript interop cuando MudBlazor lo resuelve nativamente
10. ❌ **NO** almacenar contraseñas (auth solo Google)
11. ❌ **NO** exponer entidades scaffolded directamente a la UI (usar DTOs)
12. ❌ **NO** hacer queries N+1 con EF Core
13. ❌ **NO** usar `StateHasChanged()` sin justificación documentada
14. ❌ **NO** agregar Data Annotations a entidades scaffolded
15. ❌ **NO** usar migrations code-first — cambios van en SQL y re-scaffold
16. ❌ **NO** modificar entidades generadas por scaffold manualmente

---

## Configuración de Referencia

### appsettings.json (base)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=controlpeso.db"
  },
  "Authentication": {
    "Google": {
      "ClientId": "",
      "ClientSecret": ""
    }
  },
  "GoogleAnalytics": {
    "MeasurementId": ""
  },
  "App": {
    "Name": "Control Peso Thiscloud",
    "Version": "1.0.0",
    "DefaultLanguage": "es",
    "SupportedLanguages": ["es", "en"]
  }
}
```

### Program.cs (estructura esperada)

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Application services
builder.Services.AddApplicationServices();

// 2. Infrastructure services (EF Core scaffolded DbContext, repos, etc.)
builder.Services.AddInfrastructureServices(builder.Configuration);

// 3. Authentication (Google OAuth)
builder.Services.AddGoogleAuthentication(builder.Configuration);

// 4. Blazor Server
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();

// 5. Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var app = builder.Build();

// Middleware pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
```

### Comando de scaffold (referencia)

```sh
dotnet ef dbcontext scaffold \
  "Data Source=controlpeso.db" \
  Microsoft.EntityFrameworkCore.Sqlite \
  --context ControlPesoDbContext \
  --output-dir ../ControlPeso.Domain/Entities \
  --context-dir . \
  --project src/ControlPeso.Infrastructure \
  --force \
  --no-pluralize
```

---

## Notas para el Agente Copilot

- Este proyecto es **minimalista**: no sobrecargar con features no pedidas.
- Priorizar **funcionalidad** sobre perfección visual.
- El prototipo de referencia (Google AI Studio) es una guía de UX, no una spec exacta.
- Tema oscuro como default (similar al prototipo).
- Todo peso se almacena internamente en **kg** y se convierte a lb solo para display.
- La base de datos SQLite es temporal para MVP; la arquitectura debe facilitar el swap a SQL Server sin tocar Domain ni Application.
- **El SQL manda**: cualquier cambio de modelo de datos empieza en SQL, nunca en C#.
- **Code-behind obligatorio**: NUNCA generes bloques `@code { }` en archivos `.razor`

---

## Patrones y Convenciones Específicas

### Estructura de Servicios (Application)

```csharp
// Application/Interfaces/IWeightLogService.cs
public interface IWeightLogService
{
    Task<WeightLogDto> CreateAsync(CreateWeightLogDto dto, CancellationToken ct = default);
    Task<PagedResult<WeightLogDto>> GetByUserAsync(Guid userId, WeightLogFilter filter, CancellationToken ct = default);
}

// Application/Services/WeightLogService.cs
internal sealed class WeightLogService : IWeightLogService
{
    private readonly ControlPesoDbContext _context;
    
    public WeightLogService(ControlPesoDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }
    
    public async Task<WeightLogDto> CreateAsync(CreateWeightLogDto dto, CancellationToken ct = default)
    {
        // Validación, mapeo, lógica de negocio
    }
}
```

### Estructura de Componentes Blazor

```
Components/
├── Layout/
│   ├── MainLayout.razor
│   ├── MainLayout.razor.cs          ← partial class MainLayout
│   ├── NavMenu.razor
│   └── NavMenu.razor.cs             ← partial class NavMenu
├── Shared/
│   ├── AddWeightDialog.razor
│   ├── AddWeightDialog.razor.cs     ← partial class AddWeightDialog
│   ├── WeightChart.razor
│   └── WeightChart.razor.cs         ← partial class WeightChart
└── Pages/
    ├── Dashboard.razor
    ├── Dashboard.razor.cs           ← partial class Dashboard
    ├── Profile.razor
    └── Profile.razor.cs             ← partial class Profile
```

### Ejemplo Component Code-Behind

```razor
@* WeightChart.razor *@
@namespace ControlPeso.Web.Components.Shared

<MudChart ChartType="ChartType.Line" 
          ChartSeries="@Series" 
          XAxisLabels="@Labels" 
          Width="100%" 
          Height="350px" />
```

```csharp
// WeightChart.razor.cs
using MudBlazor;

namespace ControlPeso.Web.Components.Shared;

public partial class WeightChart
{
    [Parameter, EditorRequired]
    public List<WeightLogDto> Data { get; set; } = [];
    
    private List<ChartSeries> Series { get; set; } = [];
    private string[] Labels { get; set; } = Array.Empty<string>();
    
    protected override void OnParametersSet()
    {
        if (Data.Count == 0) return;
        
        Labels = Data.Select(d => d.Date.ToString("dd/MM")).ToArray();
        
        Series = new List<ChartSeries>
        {
            new ChartSeries
            {
                Name = "Peso (kg)",
                Data = Data.Select(d => (double)d.Weight).ToArray()
            }
        };
    }
}
```

### Mappers (Application/Mapping)

```csharp
// WeightLogMapper.cs
internal static class WeightLogMapper
{
    public static WeightLogDto ToDto(WeightLog entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        return new WeightLogDto
        {
            Id = Guid.Parse(entity.Id),                    // string → Guid
            UserId = Guid.Parse(entity.UserId),            // string → Guid
            Date = DateOnly.Parse(entity.Date),            // string → DateOnly
            Time = TimeOnly.Parse(entity.Time),            // string → TimeOnly
            Weight = entity.Weight,                         // double (kg siempre)
            DisplayUnit = (WeightUnit)entity.DisplayUnit,  // int → enum
            Note = entity.Note,
            Trend = (WeightTrend)entity.Trend,             // int → enum
            CreatedAt = DateTime.Parse(entity.CreatedAt)   // string → DateTime
        };
    }
    
    public static WeightLog ToEntity(CreateWeightLogDto dto, Guid userId)
    {
        ArgumentNullException.ThrowIfNull(dto);
        
        return new WeightLog
        {
            Id = Guid.NewGuid().ToString(),                // Guid → string
            UserId = userId.ToString(),                    // Guid → string
            Date = dto.Date.ToString("yyyy-MM-dd"),        // DateOnly → string
            Time = dto.Time.ToString("HH:mm"),             // TimeOnly → string
            Weight = dto.Weight,                            // double (ya en kg)
            DisplayUnit = (int)dto.DisplayUnit,            // enum → int
            Note = dto.Note,
            Trend = (int)WeightTrend.Neutral,              // default
            CreatedAt = DateTime.UtcNow.ToString("O")      // DateTime → string ISO
        };
    }
}
```

---

## Referencias de Package Versions (Directory.Packages.props)

```xml
<PackageVersion Include="MudBlazor" Version="8.0.0" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.1" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.1" />
<PackageVersion Include="FluentValidation" Version="11.11.0" />
<PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="11.11.0" />
<PackageVersion Include="Serilog.AspNetCore" Version="8.0.3" />
<PackageVersion Include="xunit" Version="2.9.2" />
<PackageVersion Include="Moq" Version="4.20.72" />
<PackageVersion Include="bunit" Version="1.34.7" />
```

---

## Workflow de Desarrollo

### 1. Cambio de Schema (Database First)

```bash
# 1. Editar docs/schema/schema_v1.sql
# 2. Aplicar SQL
sqlite3 controlpeso.db < docs/schema/schema_v1.sql

# 3. Re-scaffold (desde src/ControlPeso.Infrastructure)
dotnet ef dbcontext scaffold "Data Source=../../controlpeso.db" \
  Microsoft.EntityFrameworkCore.Sqlite \
  --context ControlPesoDbContext \
  --output-dir ../ControlPeso.Domain/Entities \
  --context-dir . \
  --force

# 4. Ajustar value converters en DbContext si necesario (Guid, DateTime, enums)
```

### 2. Crear Nuevo Componente Blazor

```bash
# 1. Crear ComponentName.razor (solo markup)
# 2. Crear ComponentName.razor.cs (partial class con lógica)
# 3. NO usar @code { } en .razor
```

### 3. Crear Nuevo Servicio

```bash
# 1. Definir interface en Application/Interfaces/IServiceName.cs
# 2. Implementar en Application/Services/ServiceName.cs (internal sealed)
# 3. Registrar en Application/Extensions/ServiceCollectionExtensions.cs
# 4. Inyectar en Web via DI
```

### 4. Crear Nueva Página

```bash
# 1. Pages/PageName.razor (@page "/route")
# 2. Pages/PageName.razor.cs (partial class con inject de servicios)
# 3. Actualizar NavMenu.razor con MudNavLink
# 4. Crear DTOs necesarios en Application/DTOs/
# 5. Crear servicios necesarios en Application/Services/
```

---

## Checklist Antes de Commit

- [ ] Código compila sin warnings (`dotnet build`)
- [ ] Tests pasan (`dotnet test`)
- [ ] NO hay bloques `@code { }` en archivos `.razor`
- [ ] Todos los componentes tienen `.razor.cs` separado
- [ ] NO hay entidades scaffolded expuestas a UI (solo DTOs)
- [ ] NO hay lógica de negocio en Web (solo en Application)
- [ ] NO hay acceso directo a DbContext desde Web
- [ ] TODO peso almacenado en kg (conversión a lb solo en display)
- [ ] Validación con FluentValidation en Application
- [ ] Excepciones específicas, NO genéricas
- [ ] TODO async acepta `CancellationToken`
- [ ] NO hay secretos hardcodeados
- [ ] Plan actualizado si se completó alguna tarea

---

## Referencias Rápidas

- **Plan del Proyecto**: `ControlPeso/docs/Plan_ControlPeso_Thiscloud_v1_0.md`
- **Schema SQL**: `docs/schema/schema_v1.sql`
- **Tema MudBlazor**: `src/ControlPeso.Web/Theme/ControlPesoTheme.cs`
- **CPM**: `Directory.Packages.props`
- **CI Workflow**: `.github/workflows/ci.yml`

---

## Preguntas Frecuentes

**Q: ¿Puedo usar Bootstrap o HTML crudo?**  
A: ❌ NO. MudBlazor es el framework exclusivo. Solo HTML semántico cuando no existe componente MudBlazor equivalente.

**Q: ¿Puedo modificar las entidades scaffolded?**  
A: ❌ NO. Todo cambio va en SQL → Aplicar → Re-scaffold.

**Q: ¿Puedo poner lógica en el componente .razor?**  
A: ❌ NO. Solo markup en `.razor`, toda lógica en `.razor.cs` (code-behind).

**Q: ¿Puedo inyectar DbContext en un componente Blazor?**  
A: ❌ NO. Web solo inyecta servicios de Application. Application accede a Infrastructure.

**Q: ¿Puedo almacenar peso en libras?**  
A: ❌ NO. Siempre kg internamente. Conversión a lb solo en display layer.

**Q: ¿Puedo hacer PR directo a main?**  
A: ❌ NO. Git Flow: `feature/*` → `develop` → `main` (con PR y CI verde).

**Q: ¿Dónde va la validación de DTOs?**  
A: ✅ FluentValidation en Application layer (`Application/Validators/`).

**Q: ¿Cómo registro un nuevo servicio?**  
A: ✅ Interface en `Application/Interfaces/`, implementación en `Application/Services/`, registro en `Application/Extensions/ServiceCollectionExtensions.cs`.

---

**Última actualización**: 2026-02-15
