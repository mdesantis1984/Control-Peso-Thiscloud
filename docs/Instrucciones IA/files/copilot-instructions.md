# Instrucciones de Repositorio para GitHub Copilot

> **Proyecto**: ControlPeso.Thiscloud
> **Archivo**: `.github/copilot-instructions.md`
> **Modelo Copilot**: Claude Sonnet 4.5 (modo agente)
> **Última actualización**: 2026-02-15

---

## Contexto del Proyecto

Este es un proyecto **Blazor Server (.NET 9)** con **MudBlazor** como framework de UI exclusivo. La aplicación "Control Peso Thiscloud" permite a los usuarios registrar, monitorear y analizar su peso corporal con tendencias, historial y panel de administración.

### Stack Tecnológico (DECISIÓN CERRADA)

- **Runtime**: .NET 9 (`net9.0`)
- **UI Framework**: Blazor Server + MudBlazor (última versión compatible con .NET 9)
- **ORM**: Entity Framework Core (última versión compatible con .NET 9), modo **Database First**
- **Base de datos**: SQLite (desarrollo/MVP) — preparado para migrar a SQL Server/PostgreSQL
- **Autenticación**: Google OAuth 2.0 (ASP.NET Core Identity + Google provider)
- **Analytics**: Google Analytics 4 (gtag.js) + Cloudflare Analytics (capa gratuita)
- **IDE**: Visual Studio 2022+ con GitHub Copilot (Claude Sonnet 4.5 agente)

---

## Arquitectura (Onion / Cebolla) — OBLIGATORIA

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
- **Web** solo consume interfaces de Application vía DI. NUNCA accede a Infrastructure directamente desde componentes.

---

## Database First — SQL como Contrato Maestro (DECISIÓN CERRADA)

### Filosofía

El **esquema SQL es la fuente de verdad** (contrato maestro). Todo el gobierno de datos vive en SQL:

- Tipos de datos, precisión, longitudes → definidos en DDL
- Restricciones NOT NULL, UNIQUE, CHECK → definidas en DDL
- Defaults → definidos en DDL
- Foreign Keys, ON DELETE → definidas en DDL
- Índices → definidos en DDL

### Flujo de trabajo Database First

```
1. Modificar schema SQL (docs/schema/schema_v1.sql)
     ↓
2. Aplicar SQL contra SQLite (sqlite3 controlpeso.db < schema_v1.sql)
     ↓
3. Scaffold con EF Core:
   dotnet ef dbcontext scaffold "Data Source=controlpeso.db" \
     Microsoft.EntityFrameworkCore.Sqlite \
     --context ControlPesoDbContext \
     --output-dir ../ControlPeso.Domain/Entities \
     --context-dir . \
     --project src/ControlPeso.Infrastructure \
     --force
     ↓
4. Mover DbContext a Infrastructure (si scaffold no lo puso ahí)
5. Entidades generadas van a Domain/Entities (sin modificar salvo ajustes menores)
6. NO agregar Data Annotations manuales — el SQL ya las define
```

### Reglas Database First

1. **PROHIBIDO** agregar `[Required]`, `[MaxLength]`, `[Column]` u otros Data Annotations manualmente a entidades scaffolded.
2. **PROHIBIDO** crear migrations code-first. Las migraciones son scripts SQL manuales.
3. Si se necesita un cambio en la estructura de datos → se modifica el SQL primero y se re-scaffold.
4. Fluent API en `OnModelCreating` solo se usa para:
   - Ajustes que el scaffold no mapeó correctamente.
   - Conversiones de tipo (ej: enum → int si SQLite no lo resuelve bien).
   - Shadow properties si se necesitan.
5. Las entidades scaffolded son **POCO planas** — no tienen lógica de negocio.

---

## Convenciones de Código (OBLIGATORIAS)

### C# General

- **Nullable enabled** en todos los proyectos: `<Nullable>enable</Nullable>`
- **Implicit usings** habilitado: `<ImplicitUsings>enable</ImplicitUsings>`
- **Async/await end-to-end** en todas las operaciones I/O
- **CancellationToken** obligatorio en métodos async de servicios e infraestructura
- Usar **records** para DTOs y value objects cuando aporte inmutabilidad
- Usar **primary constructors** donde sea apropiado
- Prefijo `I` para interfaces: `IWeightLogService`, `IUserRepository`
- Sufijo `Dto` para DTOs: `WeightLogDto`, `UserProfileDto`
- Sufijo `Service` para servicios de aplicación: `WeightLogService`

### Blazor + MudBlazor

- **SOLO componentes MudBlazor** para UI. Prohibido HTML crudo para elementos que MudBlazor cubre (botones, tablas, formularios, dialogs, etc.)
- Componentes `.razor` pequeños y cohesivos (máximo ~150 líneas)
- Lógica compleja en **code-behind** (`.razor.cs`) o servicios inyectados
- Estados UI explícitos: Loading → Empty → Error → Success
- Usar `MudThemeProvider` con tema personalizado (dark theme como el prototipo)
- **Virtualización** (`MudVirtualize`) para listas largas
- **`@key`** en loops de componentes para optimizar re-renderizado
- Evitar `StateHasChanged()` manual salvo necesidad justificada
- Formularios con `MudForm` + `MudTextField` + validación FluentValidation o DataAnnotations
- Diálogos con `IDialogService` de MudBlazor
- Snackbars con `ISnackbar` para notificaciones
- Navegación con `MudNavMenu` + `MudNavLink`

### Entity Framework Core (Database First)

- Modo **Database First**: el esquema SQL es el contrato maestro
- `DbContext` en **Infrastructure** únicamente
- Entidades scaffolded en **Domain/Entities** — no se modifican manualmente
- **AsNoTracking()** para consultas de solo lectura
- **Proyecciones selectivas** (`.Select()`) para evitar over-fetching
- Conexión SQLite en Development: `Data Source=controlpeso.db`
- Preparado para swap a SQL Server: solo cambiar connection string + provider
- **No** exponer `DbContext` fuera de Infrastructure
- **No** usar migrations code-first — los cambios se hacen en SQL y se re-scaffold

### Manejo de Errores

- Middleware global de excepciones en Web
- **No** `try/catch` vacíos (PROHIBIDO)
- Logging estructurado con `ILogger<T>`
- Excepciones de dominio tipadas: `DomainException`, `NotFoundException`, `ValidationException`
- En Blazor: `ErrorBoundary` para errores de componentes

### Seguridad (MÍNIMO EXIGIBLE)

- Autenticación Google OAuth obligatoria para acceso
- Autorización por roles: `User`, `Administrator`
- Validación y saneamiento de toda entrada de usuario
- **No** hardcodear secretos (usar `appsettings.json` + User Secrets + env vars)
- HTTPS obligatorio
- Antiforgery tokens en formularios
- Content Security Policy (CSP) headers
- Rate limiting en endpoints sensibles
- Protección CSRF habilitada
- Cookie segura (HttpOnly, Secure, SameSite=Strict)

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

### Enums (Domain — estos SÍ son manuales, no scaffolded)

```csharp
// Estos enums se crean manualmente en Domain/Enums/
// Se usan en Application para castear los INTEGER de las entidades scaffolded
public enum UserRole { User = 0, Administrator = 1 }
public enum UserStatus { Active = 0, Inactive = 1, Pending = 2 }
public enum UnitSystem { Metric = 0, Imperial = 1 }
public enum WeightUnit { Kg = 0, Lb = 1 }
public enum WeightTrend { Up = 0, Down = 1, Neutral = 2 }
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

Shared/
├── MainLayout.razor               ← Layout principal (MudLayout + NavMenu)
├── Components/
│   ├── AddWeightDialog.razor      ← Diálogo para agregar peso (MudDialog)
│   ├── WeightChart.razor          ← Gráfico de evolución (MudChart)
│   ├── StatsCard.razor            ← Tarjeta de estadísticas (MudCard)
│   ├── WeightTable.razor          ← Tabla de historial (MudDataGrid)
│   ├── TrendCard.razor            ← Tarjeta de tendencia
│   ├── UserCard.razor             ← Tarjeta de usuario (Admin)
│   ├── LanguageSelector.razor     ← Selector de idioma (MudSelect)
│   └── NotificationBell.razor     ← Campana de notificaciones
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

### Cuando modifiques código existente:

1. No rompas la arquitectura de capas
2. Mantén las interfaces estables
3. No introduzcas dependencias prohibidas (ej: EF Core en Domain)
4. Mantén los tests existentes pasando
5. Si necesitás cambiar el modelo de datos → propone el cambio SQL primero

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

1. ❌ **NO** usar HTML crudo cuando MudBlazor tiene componente equivalente
2. ❌ **NO** acceder a `DbContext` desde Web layer
3. ❌ **NO** poner lógica de negocio en componentes `.razor`
4. ❌ **NO** hardcodear strings de conexión o secretos
5. ❌ **NO** usar `try/catch` vacíos
6. ❌ **NO** ignorar `CancellationToken` en operaciones async
7. ❌ **NO** crear dependencias circulares entre capas
8. ❌ **NO** usar JavaScript interop cuando MudBlazor lo resuelve nativamente
9. ❌ **NO** almacenar contraseñas (auth solo Google)
10. ❌ **NO** exponer entidades scaffolded directamente a la UI (usar DTOs)
11. ❌ **NO** hacer queries N+1 con EF Core
12. ❌ **NO** usar `StateHasChanged()` sin justificación documentada
13. ❌ **NO** agregar Data Annotations a entidades scaffolded
14. ❌ **NO** usar migrations code-first — cambios van en SQL y re-scaffold
15. ❌ **NO** modificar entidades generadas por scaffold manualmente

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

```bash
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
