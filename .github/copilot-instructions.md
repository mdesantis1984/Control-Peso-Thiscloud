# Copilot Instructions — Control Peso Thiscloud

> **Proyecto**: ControlPeso.Thiscloud  
> **Archivo**: `.github/copilot-instructions.md`  
> **Modelo Copilot**: Claude Sonnet 4.5 (modo agente)  
> **Última actualización**: 2026-03-03  
> **Estado**: 🟢 EN DESARROLLO — Fase 3/8 (53.2% completado)

---

## Contexto del Proyecto

Proyecto **Blazor Server (.NET 10)** con **MudBlazor** como framework UI exclusivo.

### Stack Tecnológico (DECISIÓN CERRADA)

- **Runtime**: .NET 10 (`net10.0`)
- **UI Framework**: Blazor Server + MudBlazor 8.0.0
- **ORM**: Entity Framework Core 9.0.1, modo **Database First**
- **Base de datos**: SQL Server Express (Linux)
- **Autenticación**: Google OAuth 2.0
- **Logging**: ThisCloud.Framework.Loggings 1.0.86 (Serilog + redaction + correlation)
- **Analytics**: Google Analytics 4 + Cloudflare Analytics
- **IDE**: Visual Studio 2022+ con GitHub Copilot (Claude Sonnet 4.5)

### Arquitectura Onion (OBLIGATORIA)

```
ControlPeso.Thiscloud.sln
├── src/
│   ├── ControlPeso.Domain/         ← Entidades scaffolded, enums, interfaces dominio
│   ├── ControlPeso.Application/    ← Servicios, DTOs, interfaces aplicación
│   ├── ControlPeso.Infrastructure/ ← DbContext, repos, servicios externos
│   └── ControlPeso.Web/            ← Blazor Server, Pages, Components
├── tests/
└── docs/
    └── schema/schema_v1.sql        ← CONTRATO MAESTRO datos
```

**Reglas dependencias (INQUEBRANTABLE)**:
- Domain ← NO depende de NADA externo (zero dependencies)
- Application ← Depende SOLO de Domain
- Infrastructure ← Depende de Domain + Application
- Web ← Depende de Application + Infrastructure (solo para DI)

---

## 🚨 REGLAS NO NEGOCIABLES

### Meta-Reglas (0.1-0.3) — LEER PRIMERO

0.1. ❌ **PROHIBIDO crear archivos .md nuevos** sin pedido explícito del usuario
   - Usuario NO quiere documentación .md adicional (ya pasó 5+ veces)
   - ÚNICO .md permitido: este archivo (copilot-instructions.md)
   - Si necesitas documentar algo → agregar AQUÍ, NO crear nuevo .md

0.2. ✅ **Sistema de secretos**: `.secrets.local` (archivo local, gitignored)
   - Archivo: `.secrets.local` (gitignored)
   - Template: `.secrets.local.example` (trackeado)
   - Uso: `docker compose -f docker-compose.yml -f .secrets.local up -d`
   - NUNCA commitear `.secrets.local`

0.3. ✅ **Rotación de secretos expuestos** (hacer inmediatamente post-merge)
   - Google OAuth: console.cloud.google.com → Delete old ClientID → Create new
   - Telegram Bot: @BotFather → `/revoke` → `/token`
   - SQL SA: `docker exec` → `ALTER LOGIN sa WITH PASSWORD = 'new'`
   - Actualizar `.secrets.local` local + producción

### Arquitectura (1-6)

1. ✅ Respetar Onion: Domain (zero deps) → Application (Domain) → Infrastructure (Domain+App) → Web (App+Infra)
2. ✅ SOLID obligatorio en todas las capas
3. ✅ Programación por interfaces (Application define, Infrastructure implementa)
4. ❌ PROHIBIDO acceder `DbContext` desde Web
5. ❌ PROHIBIDO lógica negocio en `.razor` o `.razor.cs`
6. ❌ PROHIBIDO exponer entidades scaffolded a UI (usar DTOs)

### Blazor Components (7-9)

7. ❌ **PROHIBIDO** bloques `@code { }` en archivos `.razor`
8. ✅ **OBLIGATORIO** code-behind: `Component.razor` (markup) + `Component.razor.cs` (partial class con lógica)
9. ✅ Ejemplo:

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
    private void OnSave() { /* lógica */ }
}
```

### Database First (10-16)

10. ❌ PROHIBIDO modificar entidades scaffolded manualmente
11. ❌ PROHIBIDO Data Annotations en entidades scaffolded
12. ❌ PROHIBIDO migrations code-first
13. ✅ TODO cambio esquema EMPIEZA en `docs/schema/schema_v1.sql`
14. ✅ Flujo: SQL → Aplicar → Scaffold → (Value converters si necesario)
15. ✅ Enums manuales en `Domain/Enums/` (mapean INTEGER del SQL)
16. ✅ SQL define: tipos, restricciones, defaults, CHECKs, FKs, índices (TODO)

**Flujo Database First**:
```bash
1. Modificar docs/schema/schema_v1.sql
2. Aplicar: sqlcmd -S localhost -U sa -P <pwd> -i schema_v1.sql
3. Scaffold:
   dotnet ef dbcontext scaffold "Server=localhost;Database=ControlPeso;User=sa;Password=<pwd>;TrustServerCertificate=true" \
     Microsoft.EntityFrameworkCore.SqlServer \
     --context ControlPesoDbContext \
     --output-dir ../ControlPeso.Domain/Entities \
     --context-dir . \
     --force
4. DbContext en Infrastructure, entidades en Domain/Entities
5. Ajustar value converters si necesario (Guid, DateTime, enums)
```

### MudBlazor (17-21)

17. ❌ PROHIBIDO HTML crudo cuando existe `Mud*` equivalente
18. ✅ Usar SIEMPRE componentes MudBlazor: `MudTextField`, `MudButton`, `MudDataGrid`, `MudCard`, `MudDialog`, `MudSnackbar`
19. ✅ **Pixel Perfect**: Explorar API completa (https://mudblazor.com/api#components) antes de implementar custom UI
20. ✅ **ThemeManager**: Control themes con MudBlazor ThemeManager (dark mode predeterminado)
21. ✅ Providers en `Routes.razor`: `MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider`

### Seguridad (22-29)

22. ❌ PROHIBIDO hardcodear secretos o connection strings
23. ❌ PROHIBIDO `try/catch` vacíos
24. ❌ PROHIBIDO loguear tokens, passwords, PII
25. ❌ PROHIBIDO almacenar contraseñas (auth solo Google)
26. ✅ Secretos en User Secrets (dev) o env vars (prod)
27. ✅ TODO catch DEBE loguear excepción
28. ✅ Cookie: `HttpOnly + Secure + SameSite=Strict`
29. ✅ CSP headers restrictivos

### Logging (30-38)

**Framework**: ThisCloud.Framework.Loggings.Serilog (estructurado + redaction + correlation)

30. ✅ Inyectar `ILogger<T>` en TODOS los servicios
31. ✅ Loguear en TODO método público (inicio/fin) con `LogInformation`
32. ✅ Loguear en TODO catch con `LogError(ex, message, ...)`
33. ✅ Logging estructurado con parámetros nombrados:

```csharp
// ✅ BIEN
_logger.LogInformation("Creating weight log for user {UserId} - Date: {Date}, Weight: {Weight}kg", 
    dto.UserId, dto.Date, dto.Weight);

// ❌ MAL
_logger.LogInformation($"User {userId} logged in");
```

34. ❌ PROHIBIDO loguear secretos (Authorization headers, JWT, API keys, passwords)
35. ❌ PROHIBIDO loguear request/response bodies completos
36. ✅ Niveles: Verbose, Debug, Information, Warning, Error, Critical
37. ✅ Emojis para identificación visual: 🤖 (robots.txt), 🗺️ (sitemap), ✅ (success), ❌ (error), 💾 (DB), 🔐 (auth)
38. ✅ Ejemplo servicio:

```csharp
internal sealed class WeightLogService : IWeightLogService
{
    private readonly ControlPesoDbContext _context;
    private readonly ILogger<WeightLogService> _logger;

    public async Task<WeightLogDto> CreateAsync(CreateWeightLogDto dto, CancellationToken ct = default)
    {
        _logger.LogInformation("💾 Creating weight log for user {UserId}", dto.UserId);
        
        try
        {
            // lógica...
            _logger.LogInformation("✅ Weight log created - Id: {WeightLogId}", result.Id);
            return result;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "❌ Database error creating weight log for user {UserId}", dto.UserId);
            throw;
        }
    }
}
```

### Código C# (39-53)

39. ❌ NO `public` por defecto → Regla mínima exposición: `private` > `internal` > `protected` > `public`
40. ❌ NO modificar código auto-generado (`*.g.cs`, `// <auto-generated>`)
41. ❌ NO agregar métodos/parámetros sin usar
42. ✅ Null checks: `ArgumentNullException.ThrowIfNull(x)`, `string.IsNullOrWhiteSpace(x)`
43. ✅ Excepciones precisas (`ArgumentException`, `InvalidOperationException`, NO `Exception`)
44. ✅ Async: métodos terminan en `Async`, aceptan `CancellationToken`
45. ✅ `ConfigureAwait(false)` en helpers/libraries
46. ✅ Comentarios explican **por qué**, NO qué
47. ✅ Nullable enabled: `<Nullable>enable</Nullable>`
48. ✅ Implicit usings: `<ImplicitUsings>enable</ImplicitUsings>`
49. ✅ Records para DTOs inmutables
50. ✅ Primary constructors donde apropiado
51. ✅ Prefijo `I` interfaces: `IWeightLogService`
52. ✅ Sufijo `Dto`: `WeightLogDto`, `UserProfileDto`
53. ✅ Sufijo `Service`: `WeightLogService`

### Blazor + MudBlazor (54-63)

54. ✅ Componentes pequeños (~150 líneas max)
55. ✅ Lógica compleja en code-behind o servicios
56. ✅ Estados UI: Loading → Empty → Error → Success
57. ✅ Virtualización (`MudVirtualize`) para listas largas
58. ✅ `@key` en loops para optimizar re-render
59. ❌ Evitar `StateHasChanged()` manual salvo necesidad
60. ✅ Formularios: `MudForm` + validación FluentValidation
61. ✅ Diálogos: `IDialogService`
62. ✅ Snackbars: `ISnackbar`
63. ✅ Navegación: `MudNavMenu` + `MudNavLink`

### Entity Framework (64-72)

64. ✅ Database First: SQL es contrato maestro
65. ✅ `DbContext` SOLO en Infrastructure
66. ✅ Entidades scaffolded en Domain/Entities — NO modificar
67. ✅ `AsNoTracking()` para queries solo lectura
68. ✅ Proyecciones `.Select()` evitan over-fetching
69. ✅ SQL Server Express (Linux) todos entornos
70. ✅ Connection string: `Server=localhost;Database=ControlPeso;User=sa;Password=<pwd>;TrustServerCertificate=true`
71. ❌ NO exponer `DbContext` fuera Infrastructure
72. ❌ NO migrations code-first

### Errores y Validación (73-80)

73. ✅ Middleware global excepciones en Web
74. ❌ NO `try/catch` vacíos
75. ✅ Logging estructurado con `ILogger<T>`
76. ✅ Excepciones dominio: `DomainException`, `NotFoundException`, `ValidationException`
77. ✅ Blazor: `ErrorBoundary` para errores componentes
78. ✅ FluentValidation para DTOs entrada
79. ✅ Validación en Application, NO en Web
80. ✅ TODO peso en **kg** (conversión lb solo display)

### Testing y NuGet (81-90)

81. ✅ xUnit + Moq/NSubstitute
82. ✅ Test por capa: `*.Tests`
83. ✅ Nombres: `WhenConditionThenExpectedBehavior` o `Metodo_Escenario_Resultado`
84. ✅ Patrón AAA (Arrange-Act-Assert)
85. ✅ NO branching en tests
86. ✅ Tests ejecutables en paralelo
87. ✅ Central Package Management (`Directory.Packages.props`)
88. ✅ NO `Version` en `PackageReference` de .csproj
89. ✅ Versiones exactas en `Directory.Packages.props`
90. ✅ NuGet Sources: nuget.org + GitHub Packages

### Git Flow (91-94)

91. ❌ PROHIBIDO commits directos a `main` o `develop`
92. ✅ Ramas: `main` (prod) ← `develop` (integración) ← `feature/*` (temporal)
93. ✅ PR obligatorio con CI verde
94. ✅ Feature branches eliminadas post-merge (local + remoto)

### Deployment (95-110)

95. ❌ PROHIBIDO deploy directo desde feature branches
96. ✅ Deploy SOLO desde `main` (production) o `develop` (pre-production)
97. ✅ Test local ANTES de commit: `docker compose -f docker-compose.yml -f .secrets.local up -d`
98. ✅ Verificar `/health` retorna 200 local antes de push
99. ✅ Build + test imagen Docker local antes de transfer
100. ✅ SQL Server password rotation requiere ALTER LOGIN (conectar con antigua → ALTER LOGIN → actualizar env vars → restart)
101. ❌ PROHIBIDO `version:` en `.secrets.local` o archivos overlay
102. ❌ PROHIBIDO `${VAR:?Error}` en `docker-compose.production.yml` (causa merge conflicts, usar `${VAR}`)
103. ✅ Producción DEBE usar sistema `.secrets.local`, NO `docker-compose.override.yml`
104. ✅ Checklist pre-deploy: containers HEALTHY local, `/health` 200, logs sin errores
105. ✅ Checklist post-deploy: containers HEALTHY remoto, `/health` 200, logs sin errores, endpoints (robots.txt, sitemap.xml) funcionando
106. ✅ Script deployment: `scripts/deploy-production.ps1` (test local → build → transfer → deploy → verify)
107. ❌ PROHIBIDO commitear `.secrets.local` (gitignored, contiene credenciales reales)
108. ✅ Template `.secrets.local.example` SÍ commitear (solo placeholders `CHANGE_ME_*`)
109. ✅ Orden deploy: Local test OK → Commit/Push → PR merge → Deploy desde main/develop
110. ✅ Rollback: Revertir imagen Docker anterior + restaurar backup BD si necesario

---

## Modelo de Datos (SQL → EF Scaffold)

### User (post-scaffold)

```csharp
public class User
{
    public Guid Id { get; set; }
    public string GoogleId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public int Role { get; set; }  // 0=User, 1=Admin
    public string? AvatarUrl { get; set; }
    public string MemberSince { get; set; } = null!;  // ISO 8601
    public double Height { get; set; }  // cm
    public int UnitSystem { get; set; }  // 0=Metric, 1=Imperial
    public string? DateOfBirth { get; set; }  // ISO 8601
    public string Language { get; set; } = null!;  // "es", "en"
    public int Status { get; set; }  // 0=Active, 1=Inactive, 2=Pending
    public double? GoalWeight { get; set; }  // kg
    public double? StartingWeight { get; set; }  // kg
    public string CreatedAt { get; set; } = null!;
    public string UpdatedAt { get; set; } = null!;

    public virtual ICollection<WeightLog> WeightLogs { get; set; } = [];
    public virtual UserPreference? UserPreference { get; set; }
}
```

### WeightLog (post-scaffold)

```csharp
public class WeightLog
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;  // FK → Users
    public string Date { get; set; } = null!;  // YYYY-MM-DD
    public string Time { get; set; } = null!;  // HH:MM
    public double Weight { get; set; }  // kg SIEMPRE
    public int DisplayUnit { get; set; }  // 0=Kg, 1=Lb
    public string? Note { get; set; }
    public int Trend { get; set; }  // 0=Up, 1=Down, 2=Neutral
    public string CreatedAt { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
```

### Enums (Domain/Enums — NO scaffolded)

```csharp
public enum UserRole { User = 0, Administrator = 1 }
public enum UserStatus { Active = 0, Inactive = 1, Pending = 2 }
public enum UnitSystem { Metric = 0, Imperial = 1 }
public enum WeightUnit { Kg = 0, Lb = 1 }
public enum WeightTrend { Up = 0, Down = 1, Neutral = 2 }
```

### Conversiones (Application/Mapping)

```csharp
// SQL string → C# tipos
public static WeightLogDto MapToDto(WeightLog entity) => new()
{
    Id = Guid.Parse(entity.Id),
    UserId = Guid.Parse(entity.UserId),
    Date = DateOnly.Parse(entity.Date),
    Time = TimeOnly.Parse(entity.Time),
    Weight = (decimal)entity.Weight,
    DisplayUnit = (WeightUnit)entity.DisplayUnit,
    Trend = (WeightTrend)entity.Trend,
    CreatedAt = DateTime.Parse(entity.CreatedAt)
};
```

---

## Patrones de Respuesta Copilot

### Al generar código nuevo:

1. Verifica capa correcta (Domain/Application/Infrastructure/Web)
2. Respeta dependencias entre capas
3. Usa interfaces existentes
4. Incluye `CancellationToken` en métodos async
5. Componentes MudBlazor — NO HTML crudo
6. Estados UI (Loading/Error/Empty/Success)
7. Validación FluentValidation en DTOs
8. Logging con `ILogger<T>`
9. **NO modifiques entidades scaffolded**
10. **SIEMPRE code-behind** (.razor.cs) — NO `@code { }`

### Al modificar código:

1. NO rompas arquitectura capas
2. Mantén interfaces estables
3. NO dependencias prohibidas (EF en Domain)
4. Tests existentes pasando
5. Cambio modelo datos → SQL primero
6. Respeta code-behind

### Al cambiar modelo datos:

1. **Primero** SQL (ALTER TABLE / CREATE TABLE)
2. **Segundo** re-scaffold
3. **Tercero** actualiza DTOs y servicios
4. **NUNCA** modifiques entidades directamente

---

## Prohibiciones (NUNCA)

1. ❌ Bloques `@code { }` en `.razor`
2. ❌ HTML crudo cuando existe Mud* componente
3. ❌ Acceso `DbContext` desde Web
4. ❌ Lógica negocio en `.razor`/`.razor.cs`
5. ❌ Hardcodear secretos
6. ❌ `try/catch` vacíos
7. ❌ Ignorar `CancellationToken`
8. ❌ Dependencias circulares
9. ❌ JavaScript interop cuando MudBlazor lo resuelve
10. ❌ Almacenar contraseñas
11. ❌ Exponer entidades scaffolded a UI
12. ❌ Queries N+1 EF Core
13. ❌ `StateHasChanged()` sin justificación
14. ❌ Data Annotations en entidades scaffolded
15. ❌ Migrations code-first
16. ❌ Modificar entidades scaffolded manualmente

---

## 🔒 Checklist Pre-Commit SEGURIDAD (OBLIGATORIO)

**ANTES de hacer `git add` / `git commit`, verificar**:

### Archivos con Secretos
- [ ] NO commitear `appsettings.Development.json` (contiene OAuth, Telegram)
- [ ] NO commitear `appsettings.Production.json` (contiene DB password, OAuth)
- [ ] NO commitear `docker-compose.override.yml` (contiene passwords)
- [ ] NO commitear `.env` o `.env.local`
- [ ] Templates `.template` / `.example` solo con placeholders `YOUR_*_HERE`

### Escaneo Rápido
```bash
# Verificar staging NO incluye secretos
git diff --staged | grep -iE "(ClientSecret|BotToken|Password=|API.*KEY)"

# Si aparece algo → DETENER y remover:
git reset HEAD archivo_con_secreto
```

### Logs Inadecuados
- [ ] NO loguear tokens OAuth, JWT, API keys
- [ ] NO loguear contraseñas o connection strings
- [ ] NO loguear request/response bodies completos (solo metadata)

### Connection Strings
- [ ] ✅ Usar placeholders: `Password=YOUR_SQL_PASSWORD_HERE`
- [ ] ✅ Usar variables entorno: `${SQLSERVER_SA_PASSWORD:?Error required}`
- [ ] ❌ NUNCA hardcodear: `Password=Cp2025!Secure#` ← PROHIBIDO

### Docker Compose
- [ ] `docker-compose.yml` → Config base, SIN secretos
- [ ] `docker-compose.production.yml` → Referencias `${VAR:?Error}`, SIN defaults inseguros
- [ ] `docker-compose.override.yml.example` → Placeholders, trackeado
- [ ] `docker-compose.override.yml` → Secretos reales, .gitignore

### Scripts
- [ ] Scripts PowerShell / Bash: NO passwords hardcodeadas
- [ ] Usar parámetros o variables entorno: `$SQLSERVER_PASSWORD`

### Documentación
- [ ] README / docs: Ejemplos genéricos (`YOUR_SECRET_HERE`)
- [ ] NO incluir credentials reales, IPs privadas sensibles, tokens

### Build Local OK
- [ ] `dotnet build` pasa sin warnings
- [ ] Tests pasan: `dotnet test`
- [ ] Aplicación arranca sin errores (si aplica)

### Verificación Final
```bash
# Verificar staging limpio
git status
git diff --staged

# Si TODO OK → commit
git commit -m "tipo(scope): descripción"
```

---

## Git Flow (MANDATORIO)

### Ramas Permanentes

- **`main`** - Producción. PROTEGIDA. Solo PRs desde `develop`
- **`develop`** - Integración. PROTEGIDA. PRs desde `feature/*`

### Flujo

```bash
# 1. Crear feature desde develop
git checkout develop
git pull origin develop
git checkout -b feature/nombre-descriptivo

# 2. Trabajar (PROBAR PRIMERO, commit después)
# - Build local: dotnet build
# - Deploy producción: docker build + transfer + deploy
# - Verificar: curl, logs, containers HEALTHY
# - SOLO SI OK → commit

# 3. Push y PR
git add .
git commit -m "feat(scope): descripción"
git push origin feature/nombre-descriptivo
# → PR en GitHub: feature/* → develop

# 4. Merge y limpieza
git checkout develop
git pull origin develop
git branch -d feature/nombre-descriptivo         # Local
git push origin --delete feature/nombre-descriptivo  # Remoto
```

### Commits

```
Formato: <tipo>(scope): <descripción>

Tipos: feat, fix, docs, style, refactor, test, chore, ci

✅ feat(seo): implement robots.txt with 50+ AI crawlers
✅ fix(auth): resolve Google OAuth redirect loop
❌ "update files"
❌ "WIP"
```

### Filosofía

**"Test First, Commit After"** — NO commitear hasta confirmar que funciona.

---

## Deployment Docker + MCP SSH

### Infraestructura

- **Servidor**: 10.0.0.100 (Proxmox VM)
- **Reverse Proxy**: NPMplus (SSL Certbot)
- **URL**: https://controlpeso.thiscloud.com.ar
- **Puerto**: 8080 (container) → NPMplus
- **Path**: `/opt/controlpeso/`

### Workflow (CRÍTICO: NO commit antes de probar)

```bash
# 1. Build local
dotnet build src/ControlPeso.Web/ControlPeso.Web.csproj -c Release

# 2. Docker image
docker build -t controlpeso-web:latest -f Dockerfile .
docker save controlpeso-web:latest -o controlpeso-web_latest.tar

# 3. Transfer
scp controlpeso-web_latest.tar root@10.0.0.100:/tmp/

# 4. Deploy (MCP SSH tool)
cd /opt/controlpeso
docker load -i /tmp/controlpeso-web_latest.tar
docker compose down
docker compose up -d

# 5. Verificar (WAIT 20s startup)
docker compose ps              # Estado: HEALTHY
docker logs controlpeso-web --tail=50
curl https://controlpeso.thiscloud.com.ar/health

# 6. SOLO SI OK → Git commit + push
```

### Checklist Pre-Deploy

- [ ] `dotnet build` exitoso
- [ ] Secrets verificados (`appsettings.*.json` en .gitignore)
- [ ] Dockerfile actualizado
- [ ] `.gitignore` incluye `*.tar`

### Checklist Post-Deploy

- [ ] Containers `HEALTHY`
- [ ] `/health` retorna 200
- [ ] Logs sin errores críticos
- [ ] Endpoints funcionando (robots.txt, sitemap.xml)
- [ ] SSL válido

### Troubleshooting

```bash
# Logs tiempo real
docker logs -f controlpeso-web

# Filtrar errores
docker logs controlpeso-web --tail=100 | grep -E "ERROR|❌"

# Restart sin rebuild
docker compose restart web

# Rebuild completo
docker compose down
docker compose build --no-cache web
docker compose up -d

# Health containers
docker compose ps
docker inspect controlpeso-web | grep -A 10 Health
```

---

## Blazor Routing (LECCIÓN CRÍTICA)

### Problema: Controllers NO capturan root-level

**Síntoma**: Controller `[HttpGet("/robots.txt")]` nunca ejecuta. Blazor lo captura primero.

### ❌ Solución INCORRECTA

```csharp
// NO FUNCIONA - Blazor lo intercepta
[ApiController]
public class SeoController : ControllerBase
{
    [HttpGet("/robots.txt")]
    public IActionResult GetRobots() { ... }
}
```

### ✅ Solución CORRECTA: Minimal APIs

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
        }).WithName("GetRobotsTxt").WithTags("SEO");

        return endpoints;
    }
}

// Program.cs (ORDEN CRÍTICO)
app.MapSeoEndpoints();          // 1. PRIMERO: Reclama /robots.txt
app.MapRazorComponents<App>();  // 2. SEGUNDO: Ya no captura
```

**Regla**: Minimal APIs registradas ANTES de `MapRazorComponents()` tienen precedencia.

### Orden Middleware

```csharp
app.MapSeoEndpoints();              // 1. Dynamic endpoints
app.MapAuthenticationEndpoints();   // 2. Auth
app.MapHealthChecks("/health");     // 3. Health
app.MapStaticAssets();              // 4. Static
app.MapRazorComponents<App>();      // 5. Blazor (ÚLTIMO)
```

---

## Config Management — Templates

### Problema

`.gitignore` + secretos locales → Al cambiar branch, configs se pierden.

### Solución: Templates

```
src/ControlPeso.Web/
├── appsettings.json                      ← Base (sin secretos)
├── appsettings.Development.json.template ← COMMITED (placeholders)
├── appsettings.Production.json.template  ← COMMITED (placeholders)
├── appsettings.Development.json          ← IGNORED (secretos reales)
└── appsettings.Production.json           ← IGNORED (secretos reales)
```

### Setup

```powershell
# setup-config.ps1
$templates = @(
    @{ Source = "src/.../appsettings.Development.json.template"; Target = "src/.../appsettings.Development.json" }
)

foreach ($item in $templates) {
    if (-not (Test-Path $item.Target)) {
        Copy-Item $item.Source $item.Target
        Write-Host "✅ Created $($item.Target)"
    }
}
```

### .gitignore

```
# ASP.NET configs con secretos (NUNCA commitear)
**/appsettings.Development.json
**/appsettings.Production.json

# Templates SÍ (solo placeholders)
!**/*.template
```

---

## Referencias Rápidas

### Docs Maestros (Consolidados)

- **Infraestructura**: `docs/infraestructura.md` (Arquitectura, Deployment, SEO, Seguridad, DB)
- **Funcional**: `docs/documentacionfuncional.md` (Planes, Sistemas, WCAG, Tests)
- **Soporte**: `docs/soporte.md` (Logging, Troubleshooting, Comandos)

### Otros

- **Schema SQL**: `docs/schema/schema_v1.sql`
- **MudBlazor API**: https://mudblazor.com/api#components
- **ThemeManager**: https://github.com/MudBlazor/ThemeManager
- **CPM**: `Directory.Packages.props`

---

## FAQ

**Q: ¿Puedo usar Bootstrap o HTML crudo?**  
A: ❌ NO. MudBlazor exclusivo.

**Q: ¿Puedo modificar entidades scaffolded?**  
A: ❌ NO. SQL → Aplicar → Re-scaffold.

**Q: ¿Puedo poner lógica en .razor?**  
A: ❌ NO. Code-behind (.razor.cs) obligatorio.

**Q: ¿Puedo inyectar DbContext en componente?**  
A: ❌ NO. Web inyecta servicios Application.

**Q: ¿Puedo almacenar peso en libras?**  
A: ❌ NO. Siempre kg, conversión lb solo display.

**Q: ¿PR directo a main?**  
A: ❌ NO. `feature/*` → `develop` → `main`.

**Q: ¿Dónde validación DTOs?**  
A: ✅ FluentValidation en Application.

**Q: ¿Cómo registro servicio?**  
A: ✅ Interface en `Application/Interfaces/`, implementación en `Application/Services/`, registro en `ServiceCollectionExtensions.cs`.

**Q: ¿Themes dark/light?**  
A: ✅ MudBlazor ThemeManager (https://github.com/MudBlazor/ThemeManager).

**Q: ¿Qué componentes MudBlazor usar?**  
A: ✅ TODOS (https://mudblazor.com/api#components). Pixel perfect.

---

## Checklist Pre-Commit

### Código y Arquitectura
- [ ] `dotnet build` sin warnings críticos
- [ ] Tests pasan (`dotnet test`)
- [ ] NO `@code { }` en `.razor`
- [ ] Code-behind `.razor.cs` separado
- [ ] NO entidades scaffolded en UI (solo DTOs)
- [ ] NO lógica negocio en Web
- [ ] NO acceso directo DbContext
- [ ] Peso en kg (conversión lb solo display)
- [ ] Validación FluentValidation
- [ ] Excepciones específicas
- [ ] `CancellationToken` en async
- [ ] Plan actualizado si tarea completada

### Seguridad (CRÍTICO)
- [ ] NO secretos hardcodeados en código
- [ ] `.secrets.local` NO en staging: `git status` no muestra `.secrets.local`
- [ ] `.secrets.local.example` solo tiene placeholders `CHANGE_ME_*`
- [ ] Verificar con `git diff --staged` que NO hay passwords, tokens, API keys
- [ ] `appsettings.*.json` con secretos están en `.gitignore`
- [ ] Connection strings usan placeholders o variables

### Deployment (si aplica)
- [ ] Test local con `.secrets.local`: `docker compose -f docker-compose.yml -f .secrets.local up -d`
- [ ] `/health` retorna 200: `curl http://localhost:8080/health`
- [ ] Containers HEALTHY: `docker ps` muestra `(healthy)`
- [ ] Logs sin errores: `docker logs controlpeso-web --tail=50`
- [ ] Docker cleanup: `docker compose down` post-test
- [ ] Usar `scripts/deploy-production.ps1` para deployment automatizado
