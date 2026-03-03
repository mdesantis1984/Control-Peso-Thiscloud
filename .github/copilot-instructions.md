# Copilot Instructions — Control Peso Thiscloud

> **Proyecto**: ControlPeso.Thiscloud
> **Archivo**: `.github/copilot-instructions.md`
> **Modelo Copilot**: Claude Sonnet 4.5 (modo agente)
> **Última actualización**: 2026-03-03
> **Estado del proyecto**: 🟢 EN DESARROLLO — Fase 3/8 (53.2% completado)

---

## Contexto del Proyecto

Este es un proyecto **Blazor Server (.NET 10)** con **MudBlazor** como framework de UI exclusivo.

### Stack Tecnológico (DECISIÓN CERRADA)

- **Runtime**: .NET 10 (`net10.0`)
- **UI Framework**: Blazor Server + MudBlazor 8.0.0
- **ORM**: Entity Framework Core 9.0.1, modo **Database First**
- **Base de datos**: SQLite (desarrollo/MVP) — preparado para migrar a SQL Server
- **Autenticación**: Google OAuth 2.0 (ASP.NET Core Identity + Google provider)
- **Logging**: ThisCloud.Framework.Loggings 1.0.86 (Serilog + redaction + correlation)
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
    - Consultar **API completa**: https://mudblazor.com/api#components
19. ✅ **Pixel Perfect Obligatorio**: Usar componentes MudBlazor en su **integridad** para conseguir diseño exacto
    - Explorar TODOS los componentes disponibles en MudBlazor antes de implementar custom UI
    - Preferir componentes nativos vs soluciones custom (ej: `MudChip`, `MudBadge`, `MudAvatar`, `MudTimeline`, etc.)
    - Si un componente MudBlazor existe para el caso de uso, DEBE usarse (no reinventar)
20. ✅ **ThemeManager**: Control de themes mediante **MudBlazor ThemeManager** (https://github.com/MudBlazor/ThemeManager)
    - MudBlazor incluye theme dark **predeterminado** (no crear custom dark theme desde cero)
    - Usar ThemeManager para:
      - Toggle Dark/Light mode
      - Customización de paleta de colores
      - Ajustes de tipografía
      - Preview en tiempo real
    - Integración: Agregar `MudThemeManagerButton` en layout para acceso rápido
21. ✅ Providers obligatorios en `Routes.razor`: `MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider`

### Seguridad

22. ❌ PROHIBIDO hardcodear secretos o connection strings
23. ❌ PROHIBIDO `try/catch` vacíos
24. ❌ PROHIBIDO loguear tokens, passwords, PII
25. ❌ PROHIBIDO almacenar contraseñas (auth solo Google)
26. ✅ Secretos en User Secrets (dev) o env vars (prod)
27. ✅ TODO catch DEBE loguear la excepción
28. ✅ Cookie: `HttpOnly + Secure + SameSite=Strict`
29. ✅ CSP headers restrictivos

### Logging (MANDATORIO - ThisCloud.Framework.Loggings)

**Contexto**: Usamos ThisCloud.Framework.Loggings.Serilog para logging estructurado enterprise-grade con redaction automática, correlation ID y file rolling.

30. ✅ **OBLIGATORIO** inyectar `ILogger<T>` en TODOS los servicios (Application, Infrastructure, Web)
31. ✅ **OBLIGATORIO** loguear en TODO método público (inicio/fin) con `LogInformation`
32. ✅ **OBLIGATORIO** loguear en TODO catch con `LogError(ex, message, ...)`
33. ✅ **OBLIGATORIO** usar logging estructurado con parámetros nombrados:
    ```csharp
    _logger.LogInformation("Creating weight log for user {UserId} - Date: {Date}, Weight: {Weight}kg", dto.UserId, dto.Date, dto.Weight);
    ```
34. ❌ **PROHIBIDO** string interpolation o concatenación en logs:
    ```csharp
    // ❌ MAL
    _logger.LogInformation($"User {userId} logged in");

    // ✅ BIEN
    _logger.LogInformation("User {UserId} logged in", userId);
    ```
35. ❌ **PROHIBIDO** loguear secretos: `Authorization` headers, JWT tokens, API keys, passwords, Google ClientSecret
    - El framework tiene redaction automática activada (`Redaction.Enabled=true`)
    - Pero NO confiar solo en redaction: NO intentar loguear secretos explícitamente
36. ❌ **PROHIBIDO** loguear request/response bodies completos (payload masivo)
    - Solo loguear propiedades relevantes con parámetros nombrados
37. ✅ Niveles de log según propósito:
    - `Verbose`: Trazas detalladas de desarrollo/debugging (valores intermedios, payload JSON resumido)
    - `Debug`: Información de diagnóstico (flujo de ejecución, decisiones lógicas)
    - `Information`: Eventos normales de negocio (inicio/fin operaciones, cambios de estado)
    - `Warning`: Situaciones anómalas recuperables (retry exitoso, valores por defecto aplicados)
    - `Error`: Errores manejados que afectan operación actual (API timeout, validación fallida)
    - `Critical`: Errores graves que afectan toda la aplicación (init failure, data corruption)
38. ✅ Ejemplo completo de servicio con logging:
    ```csharp
    internal sealed class WeightLogService : IWeightLogService
    {
        private readonly ControlPesoDbContext _context;
        private readonly ILogger<WeightLogService> _logger;

        public WeightLogService(
            ControlPesoDbContext context,
            ILogger<WeightLogService> logger)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(logger);

            _context = context;
            _logger = logger;
        }

        public async Task<WeightLogDto> CreateAsync(CreateWeightLogDto dto, CancellationToken ct = default)
        {
            _logger.LogInformation(
                "Creating weight log for user {UserId} - Date: {Date}, Weight: {Weight}kg",
                dto.UserId, dto.Date, dto.Weight);

            try
            {
                // ... lógica de negocio ...

                _logger.LogInformation(
                    "Weight log created successfully - Id: {WeightLogId}, UserId: {UserId}",
                    result.Id, dto.UserId);

                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "Database error creating weight log for user {UserId}",
                    dto.UserId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error creating weight log for user {UserId}",
                    dto.UserId);
                throw;
            }
        }
    }
    ```

### Código C#

39. ❌ NO usar `public` por defecto → Regla de mínima exposición: `private` > `internal` > `protected` > `public`
40. ❌ NO modificar código auto-generado (`*.g.cs`, `// <auto-generated>`)
41. ❌ NO agregar métodos/parámetros sin usar
42. ✅ Null checks: `ArgumentNullException.ThrowIfNull(x)` para objetos, `string.IsNullOrWhiteSpace(x)` para strings
43. ✅ Excepciones precisas (`ArgumentException`, `InvalidOperationException`, NO `Exception` genérica)
44. ✅ Async end-to-end: métodos async terminan en `Async`, aceptan `CancellationToken`
45. ✅ `ConfigureAwait(false)` en helpers/libraries
46. ✅ Comentarios explican **por qué**, NO qué hace el código
47. ✅ **Nullable enabled** en todos los proyectos: `<Nullable>enable</Nullable>`
48. ✅ **Implicit usings** habilitado: `<ImplicitUsings>enable</ImplicitUsings>`
49. ✅ Usar **records** para DTOs y value objects cuando aporte inmutabilidad
50. ✅ Usar **primary constructors** donde sea apropiado
51. ✅ Prefijo `I` para interfaces: `IWeightLogService`, `IUserRepository`
52. ✅ Sufijo `Dto` para DTOs: `WeightLogDto`, `UserProfileDto`
53. ✅ Sufijo `Service` para servicios de aplicación: `WeightLogService`

### Blazor + MudBlazor (Detalles Adicionales)

54. ✅ Componentes `.razor` pequeños y cohesivos (máximo ~150 líneas)
55. ✅ Lógica compleja en **code-behind** (`.razor.cs`) o servicios inyectados
56. ✅ Estados UI explícitos: Loading → Empty → Error → Success
57. ✅ **Virtualización** (`MudVirtualize`) para listas largas
58. ✅ **`@key`** en loops de componentes para optimizar re-renderizado
59. ❌ Evitar `StateHasChanged()` manual salvo necesidad justificada
60. ✅ Formularios con `MudForm` + `MudTextField` + validación FluentValidation
61. ✅ Diálogos con `IDialogService` de MudBlazor
62. ✅ Snackbars con `ISnackbar` para notificaciones
63. ✅ Navegación con `MudNavMenu` + `MudNavLink`

### Entity Framework Core (Database First)

64. ✅ Modo **Database First**: el esquema SQL es el contrato maestro
65. ✅ `DbContext` en **Infrastructure** únicamente
66. ✅ Entidades scaffolded en **Domain/Entities** — no se modifican manualmente
67. ✅ **AsNoTracking()** para consultas de solo lectura
68. ✅ **Proyecciones selectivas** (`.Select()`) para evitar over-fetching
69. ✅ Conexión SQLite en Development: `Data Source=controlpeso.db`
70. ✅ Preparado para swap a SQL Server: solo cambiar connection string + provider
71. ❌ **No** exponer `DbContext` fuera de Infrastructure
72. ❌ **No** usar migrations code-first — los cambios se hacen en SQL y se re-scaffold

### Manejo de Errores

73. ✅ Middleware global de excepciones en Web
74. ❌ **No** `try/catch` vacíos (PROHIBIDO)
75. ✅ Logging estructurado con `ILogger<T>` (ver sección Logging arriba)
76. ✅ Excepciones de dominio tipadas: `DomainException`, `NotFoundException`, `ValidationException`
77. ✅ En Blazor: `ErrorBoundary` para errores de componentes

### Validación y Lógica de Negocio

78. ✅ FluentValidation para DTOs de entrada
79. ✅ Validación en Application layer, NO en Web
80. ✅ Mappers manuales en `Application/Mapping/` (conversiones string→Guid, string→DateTime, int→enum)
81. ✅ TODO peso almacenado SIEMPRE en **kg** (conversión a lb solo en display)

### Testing

82. ✅ xUnit + Moq/NSubstitute
83. ✅ Proyecto de test separado por capa (`*.Tests`)
84. ✅ Nombre de tests: `WhenConditionThenExpectedBehavior` o `Metodo_Escenario_ResultadoEsperado`
85. ✅ Patrón AAA (Arrange-Act-Assert)
86. ✅ NO branching/conditionals en tests
87. ✅ Tests deben poder correr en cualquier orden o en paralelo

### NuGet y Versiones

88. ✅ Central Package Management (`Directory.Packages.props`)
89. ✅ NO especificar `Version` en `PackageReference` de .csproj
90. ✅ Versiones exactas en `Directory.Packages.props`

### Git y Documentación

91. ❌ PROHIBIDO commits directos a `main` o `develop`
92. ✅ Git Flow: `main` → `develop` → `feature/*`
93. ✅ PR obligatorio con CI verde
94. ✅ Plan `ControlPeso/docs/Plan_ControlPeso_Thiscloud_v1_0.md` es contractual → actualizar ANTES de marcar tareas "Done"

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

## Git Flow ControlPeso.Thiscloud (MANDATORIO)

### Ramas Permanentes

- **`main`** - Producción. PROTEGIDA. Solo recibe PRs desde `develop`.
- **`develop`** - Integración/desarrollo. PROTEGIDA. Recibe PRs desde `feature/*`.

**CRÍTICO**: Estas son las **ÚNICAS** ramas que permanecen. Todas las `feature/*` se eliminan después del merge.

### Ramas Temporales

- **`feature/*`** - Trabajo temporal (ej: `feature/fase-2`, `feature/deploy-production-v4.0.0`).
  - Se crean desde `develop`
  - Se eliminan después del merge (local + remoto)

### Flujo Simplificado (workflow real)

```
1. Crear feature desde develop:
   git checkout develop
   git pull origin develop
   git checkout -b feature/nombre-descriptivo

2. Trabajar en la feature:
   - PROBAR primero (deploy, test, validar)
   - Commits al FINAL cuando todo funciona
   - Commit descriptivo por tarea completada

3. Push y PR a develop:
   git add .
   git commit -m "feat(scope): descripción completa"
   git push origin feature/nombre-descriptivo
   → Crear PR en GitHub: feature/* → develop

4. Aprobar PR:
   - Review + CI verde
   - Merge a develop (create merge commit, NO squash)

5. Limpieza de rama feature (local + remoto):
   git checkout develop
   git pull origin develop
   git branch -d feature/nombre-descriptivo        # Local
   git push origin --delete feature/nombre-descriptivo  # Remoto

6. PR de develop a main (cuando esté estable):
   → Crear PR en GitHub: develop → main
   - Review + CI verde + QA completo
   - Merge a main
   - Crear tag de release: git tag v1.0.0 && git push origin v1.0.0

7. RESULTADO FINAL:
   Solo existen 2 ramas remotas: main + develop
   Todas las features eliminadas después del merge
```

### Filosofía de Commits

**"Test First, Commit After"** — NO commitear hasta confirmar que funciona.

```
# ❌ MAL - Commit antes de probar
git add .
git commit -m "feat: add robots.txt endpoint"  ← ¿Funciona? ¿Está testeado?

# ✅ BIEN - Probar primero, commit después
1. Implementar código
2. Build local: dotnet build (sin errores)
3. Deploy a producción: docker build + transfer + deploy
4. Verificar: curl endpoints, logs, containers HEALTHY
5. SOLO SI TODO OK → git add . && git commit && git push
```

### Reglas Obligatorias

- ❌ **PROHIBIDO** commits directos a `main` o `develop`
- ❌ **PROHIBIDO** PR directo de `feature/*` a `main` (siempre pasar por `develop`)
- ❌ **PROHIBIDO** squash commits (usar "Create a merge commit")
- ❌ **PROHIBIDO** dejar ramas `feature/*` después del merge (limpiar local + remoto)
- ✅ **OBLIGATORIO** PR con CI verde antes de merge
- ✅ **OBLIGATORIO** probar ANTES de commit (deploy, test, verificar)
- ✅ **OBLIGATORIO** commits descriptivos por tarea completada
- ✅ **OBLIGATORIO** eliminar rama feature después de merge (local + remoto)
- ✅ **OBLIGATORIO** pull de develop antes de crear nueva feature
- ✅ **OBLIGATORIO** solo 2 ramas permanentes: `main` + `develop`

### Estructura de Commits

```
Formato: <tipo>(scope): <descripción>

Tipos:
- feat: Nueva funcionalidad (ej: feat(seo): add robots.txt with 50+ AI crawlers)
- fix: Corrección de bug
- docs: Cambios en documentación
- style: Formato de código (no afecta lógica)
- refactor: Refactorización sin cambio de funcionalidad
- test: Agregar o modificar tests
- chore: Tareas de mantenimiento (build, CI, deps)
- ci: Cambios en CI/CD workflows

Ejemplos:
✅ feat(seo): implement Minimal APIs for robots.txt and sitemap.xml with 50+ AI crawlers
✅ fix(auth): resolve Google OAuth redirect loop
✅ test(application): add unit tests for WeightLogService
✅ docs: update deployment checklist with MCP SSH workflow
✅ ci: add Docker build validation step

❌ "update files"
❌ "WIP"
❌ "fix stuff"
```

### Workflow Visual

```
main (producción)
  ↑
  PR (cuando develop está estable)
  ↑
develop (integración) ←───────────────┐
  ↓                                    │
  git checkout -b feature/X            │
  ↓                                    │
feature/X (trabajo)                    │
  ↓ (probar, validar, testear)        │
  ↓ git commit (al final)              │
  ↓ git push origin feature/X          │
  ↓ PR → develop ──────────────────────┘
  ↓ Merge exitoso
  ↓ git branch -d feature/X (local)
  ↓ git push origin --delete feature/X (remoto)
  ↓
RESULTADO: Solo quedan main + develop
```

### Comandos de Limpieza

```
# Ver ramas locales
git branch

# Ver ramas remotas
git branch -r

# Eliminar rama local
git branch -d feature/nombre-rama

# Eliminar rama remota
git push origin --delete feature/nombre-rama

# Limpiar referencias obsoletas
git fetch --prune

# Verificar estado final (solo debe mostrar main + develop)
git branch -a
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

## Deployment a Producción — Docker + MCP SSH (MANDATORIO)

### Infraestructura

- **Servidor**: 10.0.0.100 (Proxmox VM)
- **Reverse Proxy**: NPMplus (Nginx Proxy Manager) con Certbot SSL
- **URL Producción**: https://controlpeso.thiscloud.com.ar
- **Puerto interno**: 8080 (contenedor) → NPMplus proxy
- **Path deployment**: `/opt/controlpeso/`
- **Compose file**: `docker-compose.yml` (web + sqlserver)

### Workflow de Deployment (OBLIGATORIO)

**REGLA CRÍTICA**: NUNCA commitear a Git antes de probar en producción.

```bash
# Flujo completo: Build → Transfer → Deploy → Test → Commit

# 1. Build local
dotnet build src/ControlPeso.Web/ControlPeso.Web.csproj -c Release

# 2. Build Docker image
docker build -t controlpeso-web:latest -f Dockerfile .

# 3. Save image
docker save controlpeso-web:latest -o controlpeso-web_latest.tar

# 4. Transfer to server
scp controlpeso-web_latest.tar root@10.0.0.100:/tmp/

# 5. Deploy usando MCP SSH tool
cd /opt/controlpeso
docker load -i /tmp/controlpeso-web_latest.tar
docker compose down
docker compose up -d

# 6. Verificar (WAIT 20s para startup)
docker compose ps              # Estado: HEALTHY
docker logs controlpeso-web --tail=50
curl https://controlpeso.thiscloud.com.ar/health

# 7. SOLO SI TODO OK → Git commit + push
git add .
git commit -m "feat(scope): descripción"
git push origin feature/nombre
```

### Herramientas de Deployment

- **Local**: PowerShell con comandos inline (preferencia usuario: `pw inline`)
- **Remoto**: MCP SSH (`mi_proxmox_ssh_exec`) para operaciones sin password
- **Containers**: Docker Compose para orquestación
- **Verificación**: `curl`, `docker logs`, `docker compose ps`

### Checklist Pre-Deployment

- [ ] `dotnet build` exitoso (sin errores, warnings de formato OK)
- [ ] Secrets verificados: `appsettings.*.json` en .gitignore
- [ ] Dockerfile actualizado (comentarios coherentes)
- [ ] `.gitignore` incluye `*.tar` (no subir Docker images)
- [ ] Static files eliminados si se sirven dinámicamente
- [ ] Logs con emojis para identificación visual (🚀🤖🗺️✅❌)

### Checklist Post-Deployment

- [ ] Containers status: `HEALTHY` (no `starting` infinito)
- [ ] Health endpoint: `/health` retorna 200
- [ ] Logs sin errores críticos (check últimas 50 líneas)
- [ ] Endpoints críticos funcionando (robots.txt, sitemap.xml, etc.)
- [ ] NPMplus proxy serving correctamente (test desde navegador)
- [ ] SSL válido (Certbot renewal OK)

### Comandos de Troubleshooting

```bash
# Ver logs en tiempo real
docker logs -f controlpeso-web

# Ver últimas 100 líneas con filtro
docker logs controlpeso-web --tail=100 | grep -E "ERROR|WARN|🚀"

# Restart sin rebuild
docker compose restart web

# Rebuild completo
docker compose down
docker compose build --no-cache web
docker compose up -d

# Verificar salud de containers
docker compose ps
docker inspect controlpeso-web | grep -A 10 Health

# Limpiar images viejas
docker image prune -a --filter "until=24h"
```

### Seguridad en Producción

- ❌ **PROHIBIDO** tocar recursos que NO sean Control Peso
- ❌ **PROHIBIDO** ejecutar comandos destructivos sin confirmación
- ❌ **PROHIBIDO** exponer secretos en logs o comandos
- ✅ Usar User Secrets (dev) o env vars (prod) para configuración sensible
- ✅ Verificar `.gitignore` ANTES de `git add .`
- ✅ Templates (`.template` files) para configs con placeholders

---

## Blazor Server — Routing Precedence Issues (LECCIONES CRÍTICAS)

### Problema: Controllers NO capturan rutas root-level

**Contexto**: En Blazor Server, `MapRazorComponents()` captura rutas root-level (`/robots.txt`, `/sitemap.xml`) ANTES que `MapControllers()`, incluso si se llama primero en el pipeline.

**Síntoma**: Controller definido con `[HttpGet("/robots.txt")]` nunca se ejecuta. Blazor intenta resolver la ruta, falla con 404.

**Causa raíz**: Blazor routing tiene MAYOR precedencia que API Controllers para rutas root-level.

### ❌ Solución INCORRECTA: Controllers

```csharp
// ❌ NO FUNCIONA - Blazor lo captura primero
[ApiController]
public class SeoController : ControllerBase
{
    [HttpGet("/robots.txt")]  // ← Blazor intercepta esta ruta
    public IActionResult GetRobots() { ... }
}

// En Program.cs (NO FUNCIONA):
app.MapControllers();           // ← Registrado primero
app.MapRazorComponents<App>();  // ← Pero captura /robots.txt igual
```

### ❌ Solución TEMPORAL: Usar `/api/` prefix

```csharp
[HttpGet("/api/robots.txt")]  // ← Funciona pero motores de búsqueda NO lo encuentran
```

**Problema**: Search engines (Google, Bing) esperan `/robots.txt`, NO `/api/robots.txt`.

### ✅ Solución CORRECTA: Minimal APIs en Extension

**Estrategia**: Registrar Minimal APIs con `MapGet()` ANTES de `MapRazorComponents()` en un extension method separado.

```csharp
// Extensions/SeoEndpointsExtensions.cs
public static class SeoEndpointsExtensions
{
    public static IEndpointRouteBuilder MapSeoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/robots.txt", async (SitemapService service) => 
        {
            var content = service.GenerateRobotsTxt();
            return Results.Text(content, "text/plain; charset=utf-8");
        })
        .WithName("GetRobotsTxt")
        .WithTags("SEO");

        return endpoints;
    }
}

// Program.cs (ORDEN CRÍTICO)
app.MapSeoEndpoints();          // ← PRIMERO: Reclama /robots.txt
app.MapRazorComponents<App>();  // ← SEGUNDO: Ya no puede capturar /robots.txt
```

**Por qué funciona**: Minimal APIs registradas temprano tienen precedencia de routing sobre Blazor components.

### Reglas de Routing en Blazor Server

1. ✅ Minimal APIs (`MapGet`, `MapPost`) → Mayor precedencia si se registran PRIMERO
2. ❌ Controllers (`MapControllers`) → Menor precedencia que Blazor en rutas root-level
3. ✅ Extension methods para endpoints críticos → Limpia separación de concerns
4. ✅ Registrar en este orden:

```csharp
app.MapSeoEndpoints();         // 1. Minimal APIs (SEO)
app.MapAuthenticationEndpoints(); // 2. Minimal APIs (Auth)
app.MapHealthChecks("/health");   // 3. Health checks
app.MapStaticAssets();            // 4. Static files
app.MapRazorComponents<App>();    // 5. Blazor (ÚLTIMO)
```

### Debugging Routing Issues

```csharp
// Agregar logging con emojis para identificación visual
logger.LogInformation("🤖 /robots.txt requested from {UserAgent}", userAgent);
logger.LogInformation("🗺️ /sitemap.xml requested from {UserAgent}", userAgent);
logger.LogInformation("🚀 Controller method executed");  // Si ves esto, es Controller
```

**Verificación**: Si ves emojis de Minimal API (🤖🗺️) en logs → Funcionando correctamente.

### Cuándo usar Minimal APIs vs Controllers

- ✅ **Minimal APIs**: Endpoints root-level (`/robots.txt`, `/sitemap.xml`, `/health`)
- ✅ **Controllers**: APIs REST complejas bajo prefijo (`/api/v1/...`)
- ❌ **NUNCA**: Controllers para rutas root-level en Blazor Server

### Referencia

- **Archivo**: `src/ControlPeso.Web/Extensions/SeoEndpointsExtensions.cs`
- **Docs**: `docs/SEO_INFRASTRUCTURE.md` (sección Blazor Routing)
- **Issue resuelto**: 2026-03-03 (commit: `b5bcd90`)

---

## Logging Patterns — Identificación Visual con Emojis

### Estrategia de Logging

Usar **emojis como marcadores visuales** en logs para identificar rápidamente el origen de ejecución.

### Emojis por Categoría

```
// SEO Endpoints
🤖 - /robots.txt requests (Minimal API)
🗺️ - /sitemap.xml requests (Minimal API)
🚀 - Controller methods (legacy, si existen)
🔧 - Service initialization

// Estados
✅ - Success, operación completada
❌ - Error, operación fallida
⚠️ - Warning, situación anómala
ℹ️ - Information, evento normal

// Operaciones
💾 - Database operations
🔐 - Authentication/Authorization
📤 - Upload/Send
📥 - Download/Receive
🔄 - Sync/Refresh
```

### Ejemplo de Implementación

```csharp
public class WeightLogService : IWeightLogService
{
    private readonly ILogger<WeightLogService> _logger;

    public async Task<WeightLogDto> CreateAsync(CreateWeightLogDto dto, CancellationToken ct)
    {
        _logger.LogInformation(
            "💾 Creating weight log for user {UserId} - Date: {Date}, Weight: {Weight}kg",
            dto.UserId, dto.Date, dto.Weight);

        try
        {
            // ... lógica ...

            _logger.LogInformation(
                "✅ Weight log created - Id: {WeightLogId}, UserId: {UserId}",
                result.Id, dto.UserId);

            return result;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex,
                "❌ Database error creating weight log for user {UserId}",
                dto.UserId);
            throw;
        }
    }
}
```

### Ventajas

1. **Identificación instantánea**: `grep -E "🤖|🗺️"` en logs
2. **Debugging visual**: Scrolling rápido en consola Docker
3. **Diferenciación**: Controller vs Minimal API (🚀 vs 🤖)
4. **Status at a glance**: ✅/❌ sin leer mensaje completo

### Búsqueda en Logs

```bash
# Ver SOLO requests de robots.txt
docker logs controlpeso-web | grep "🤖"

# Ver TODOS los SEO endpoints
docker logs controlpeso-web | grep -E "🤖|🗺️"

# Ver errores rápidamente
docker logs controlpeso-web | grep "❌"

# Ver flow completo con emojis
docker logs controlpeso-web | grep -E "🚀|🤖|🗺️|✅|❌"
```

### Reglas

- ✅ Usar emojis en `LogInformation` y `LogWarning` (visibilidad)
- ✅ Usar en INICIO de mensaje (fácil scan visual)
- ❌ NO usar en `LogError` exception message (puede romper parsing)
- ❌ NO abusar (máximo 1-2 emojis por categoría)
- ✅ Documentar emojis en esta sección para consistency

---

## Static Files vs Dynamic Endpoints — Precedence Rules

### Problema: Conflicto entre wwwroot y Endpoints

Si existe `wwwroot/robots.txt` (static file) Y un endpoint `/robots.txt` (dynamic), ASP.NET Core sirve el **static file** por defecto.

### Solución: Eliminar Static Files

```bash
# 1. Eliminar del source
rm src/ControlPeso.Web/wwwroot/robots.txt
rm src/ControlPeso.Web/wwwroot/sitemap.xml

# 2. Eliminar del Dockerfile (por si se regeneran en build)
# Dockerfile (después de COPY --from=publish)
RUN rm -f /app/wwwroot/robots.txt* /app/wwwroot/sitemap.xml*

# 3. Verificar en container
docker exec controlpeso-web ls -la /app/wwwroot/ | grep -E "robots|sitemap"
# Output esperado: (vacío - no debe existir)
```

### Orden de Middleware (CRÍTICO)

```csharp
// Program.cs
app.MapSeoEndpoints();        // 1. Dynamic endpoints PRIMERO
app.MapStaticAssets();        // 2. Static files DESPUÉS
app.MapRazorComponents<App>();
```

**Regla**: Registrar endpoints dinámicos ANTES de `MapStaticAssets()` o `UseStaticFiles()`.

### Verificación

```bash
# Test endpoint dinámico (debe retornar contenido generado)
curl -I https://controlpeso.thiscloud.com.ar/robots.txt

# Verificar headers:
# ✅ Content-Type: text/plain; charset=utf-8  (dinámico)
# ❌ Content-Type: text/plain                 (estático, sin charset)

# ✅ Cache-Control: public, max-age=86400     (dinámico con cache)
# ❌ Cache-Control: (vacío o diferente)       (estático)
```

### Cuándo usar Static vs Dynamic

| Caso de Uso | Static File | Dynamic Endpoint |
|-------------|-------------|------------------|
| Imágenes, CSS, JS | ✅ Siempre | ❌ Nunca |
| robots.txt con contenido variable | ❌ | ✅ (50+ AI crawlers) |
| sitemap.xml con URLs dinámicas | ❌ | ✅ (cambios frecuentes) |
| Archivos grandes (PDFs) | ✅ (CDN ideal) | ❌ (performance) |
| Contenido personalizado por usuario | ❌ | ✅ (request context) |

### Diagnóstico de Issues

```bash
# Si /robots.txt retorna contenido viejo/incorrecto:

# 1. Verificar que NO existe static file
docker exec controlpeso-web ls -la /app/wwwroot/ | grep robots

# 2. Verificar logs de ejecución
docker logs controlpeso-web --tail=50 | grep -E "🤖|robots"
# Debe mostrar: "🤖 /robots.txt requested"

# 3. Verificar orden de middleware en Program.cs
# MapSeoEndpoints() DEBE estar ANTES de MapStaticAssets()

# 4. Rebuild si es necesario (static files pueden quedar en cache)
docker compose down
docker compose build --no-cache web
docker compose up -d
```

---

## Configuration Management — Templates Pattern

### Problema: Secretos en Git + Config Loss

**Anti-pattern**: `.gitignore` + secretos en `appsettings.json` local → Al cambiar de branch/clonar repo, configs se pierden.

### Solución: Template Files

```
src/ControlPeso.Web/
├── appsettings.json                      ← Base config (sin secretos)
├── appsettings.Development.json.template ← COMMITED (placeholders)
├── appsettings.Production.json.template  ← COMMITED (placeholders)
├── appsettings.Development.json          ← IGNORED (real secrets)
└── appsettings.Production.json           ← IGNORED (real secrets)
```

### Setup Script

```powershell
# setup-config.ps1
# Crea configs reales desde templates con placeholders

$templates = @(
    @{
        Source = "src/ControlPeso.Web/appsettings.Development.json.template"
        Target = "src/ControlPeso.Web/appsettings.Development.json"
    },
    @{
        Source = "src/ControlPeso.Web/appsettings.Production.json.template"
        Target = "src/ControlPeso.Web/appsettings.Production.json"
    }
)

foreach ($item in $templates) {
    if (-not (Test-Path $item.Target)) {
        Copy-Item $item.Source $item.Target
        Write-Host "✅ Created $($item.Target) from template"
    } else {
        Write-Host "⚠️ $($item.Target) already exists - skipping"
    }
}

Write-Host "`n📝 NEXT STEPS:"
Write-Host "1. Edit appsettings.Development.json with real secrets"
Write-Host "2. Edit appsettings.Production.json with real secrets"
Write-Host "3. These files are in .gitignore - they WON'T be committed"
```

### Workflow

```bash
# 1. Primer clone del repo
git clone https://github.com/usuario/repo.git
cd repo

# 2. Setup configs desde templates
./setup-config.ps1

# 3. Editar configs con secretos REALES
# Editar: src/ControlPeso.Web/appsettings.Development.json
# Agregar: Google ClientId/ClientSecret, Connection Strings, etc.

# 4. Los .json reales NUNCA se commitean (.gitignore)
# 5. Cambiar de branch mantiene configs locales
```

### .gitignore Rules

```gitignore
# ASP.NET Core configs con secretos (NUNCA commitear)
**/appsettings.Development.json
**/appsettings.Production.json

# Templates SÍ se commitean (solo placeholders)
!**/*.template

# User Secrets también están protegidos
**/secrets.json

# Docker artifacts
*.tar
```

### Template Format

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "REPLACE_WITH_GOOGLE_CLIENT_ID",
      "ClientSecret": "REPLACE_WITH_GOOGLE_CLIENT_SECRET"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "REPLACE_WITH_CONNECTION_STRING"
  },
  "App": {
    "BaseUrl": "https://controlpeso.thiscloud.com.ar"
  }
}
```

### Verificación Pre-Commit

```bash
# SIEMPRE verificar ANTES de git add
git status

# Verificar que templates existen pero configs reales NO
# ✅ DEBE aparecer: appsettings.*.json.template (A = added/modified)
# ❌ NO DEBE aparecer: appsettings.Development.json
# ❌ NO DEBE aparecer: appsettings.Production.json

# Si aparecen configs reales:
git reset HEAD appsettings.Development.json
git reset HEAD appsettings.Production.json

# Agregar a .gitignore si no están
echo "**/appsettings.Development.json" >> .gitignore
echo "**/appsettings.Production.json" >> .gitignore
```

### Referencia

- **Script**: `setup-config.ps1` (root del repo)
- **Docs**: `docs/CONFIG_MANAGEMENT.md` (detalles completos)
- **Templates**: `src/ControlPeso.Web/appsettings.*.json.template`

---

## Referencias Rápidas

- **Plan del Proyecto**: `ControlPeso/docs/Plan_ControlPeso_Thiscloud_v1_0.md`
- **Schema SQL**: `docs/schema/schema_v1.sql`
- **MudBlazor Components API**: https://mudblazor.com/api#components
- **MudBlazor ThemeManager**: https://github.com/MudBlazor/ThemeManager
- **CPM**: `Directory.Packages.props`
- **CI Workflow**: `.github/workflows/ci.yml`
- **Framework Integration**: `docs/THISCLOUD_FRAMEWORK_INTEGRATION.md`
- **SEO Infrastructure**: `docs/SEO_INFRASTRUCTURE.md`
- **Config Management**: `docs/CONFIG_MANAGEMENT.md`

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

**Q: ¿Debo usar ThisCloud.Framework?**  
A: ✅ SÍ para Loggings (Serilog estructurado + redaction + correlation). Ver `docs/THISCLOUD_FRAMEWORK_INTEGRATION.md` para análisis completo. NO para Web (Blazor Server no usa Minimal APIs). Requiere actualizar target a .NET 10.

**Q: ¿Cómo controlo los themes (dark/light)?**  
A: ✅ Usar **MudBlazor ThemeManager** (https://github.com/MudBlazor/ThemeManager). MudBlazor incluye theme dark **predeterminado** (no crear custom desde cero). ThemeManager provee: toggle Dark/Light, customización de paleta, ajustes de tipografía, y preview en tiempo real. Agregar `MudThemeManagerButton` en layout para acceso rápido.

**Q: ¿Qué componentes MudBlazor debo usar?**  
A: ✅ **TODOS** los componentes disponibles en https://mudblazor.com/api#components para conseguir **pixel perfect**. Explorar la API completa ANTES de implementar UI. Si existe un componente nativo para el caso de uso, DEBE usarse (no reinventar). Ejemplos: `MudChip`, `MudBadge`, `MudAvatar`, `MudTimeline`, `MudCarousel`, `MudSkeleton`, etc.

---

**Última actualización**: 2026-02-28 (Post CSS Cleanup - ButtonGroup Fixed)
