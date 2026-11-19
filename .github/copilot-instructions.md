# Copilot Instructions — Control Peso Thiscloud

> Instrucciones para asistentes de código y colaboradores. Describen las convenciones **vigentes** del repositorio; ante una duda, el código y los workflows de `.github/workflows/` mandan sobre este documento.

## Contexto del proyecto

Aplicación **Blazor Server (.NET 10)** con **MudBlazor** como único framework de UI, datos en **PostgreSQL** y despliegue como imagen Docker. Punto de entrada de la documentación: [README.md](../README.md).

### Stack (decisión cerrada)

- **Runtime**: .NET 10 (`net10.0`)
- **UI**: Blazor Server + MudBlazor 9
- **ORM**: Entity Framework Core 10 con Npgsql, **code-first con migraciones**
- **Base de datos**: PostgreSQL 15 (Testcontainers en los tests de integración)
- **Autenticación**: Google OAuth 2.0 (cookie persistente de 30 días)
- **Validación**: FluentValidation
- **Logging**: ThisCloud.Framework.Loggings (Serilog, NDJSON, redacción, correlación)
- **Imágenes**: SkiaSharp (avatares)
- **Paquetes**: Central Package Management (`Directory.Packages.props`)

### Arquitectura Onion (obligatoria)

```
ControlPeso.slnx
├── src/
│   ├── ControlPeso.Domain/            Entidades, enums, excepciones
│   ├── ControlPeso.Application/       Interfaces, servicios, DTOs, validadores, mapeos
│   ├── ControlPeso.Infrastructure/    ControlPesoDbContext, migraciones, servicios externos
│   ├── ControlPeso.Shared.Resources/  Recursos de localización (.resx)
│   └── ControlPeso.Web/               Blazor Server: páginas, componentes, middleware
├── tests/                             Un proyecto de tests por capa
├── tools/                             Utilidades de desarrollo
├── docs/                              Documentación
└── scripts/                           Scripts de operación
```

Dependencias (inquebrantables): `Domain` no depende de nada; `Application` solo de `Domain`; `Infrastructure` de `Domain` y `Application`; `Web` de `Application` e `Infrastructure` (esta última solo para registrar la inyección de dependencias).

---

## Reglas no negociables

### Arquitectura

1. Respetar Onion y SOLID; programar contra interfaces (`Application` define, `Infrastructure` implementa).
2. ❌ Prohibido acceder a `DbContext` desde `Web`; los componentes inyectan servicios de `Application`.
3. ❌ Prohibida la lógica de negocio en `.razor` y `.razor.cs`.
4. ❌ Prohibido exponer entidades a la UI: usar DTOs.
5. Todo peso se almacena en **kg**; la conversión a libras es solo de presentación.
6. Un servicio nuevo: interfaz en `Application/Interfaces/`, implementación en `Application/Services/`, registro en `Application/Extensions`.

### Componentes Blazor y MudBlazor

7. ❌ Prohibidos los bloques `@code { }` en componentes nuevos: usar code-behind (`Component.razor` + `Component.razor.cs` con `partial class`).
8. ❌ Prohibido HTML crudo cuando existe un componente `Mud*` equivalente; revisar la API de MudBlazor antes de crear UI propia.
9. Estados de UI explícitos: carga, vacío, error y éxito. Componentes pequeños (≈150 líneas), `@key` en bucles y `StateHasChanged()` solo si hace falta.
10. Formularios con `MudForm` y FluentValidation; diálogos con `IDialogService`; avisos con `ISnackbar`.
11. Los textos visibles usan `IStringLocalizer` y los `.resx` de `Shared.Resources` (`es-AR` por defecto, `en-US`); nunca texto fijo en la UI.
12. Si un componente se suscribe a eventos (`UserStateService`, por ejemplo), debe implementar `IDisposable` y desuscribirse.
13. JS interop solo después de `OnAfterRenderAsync(firstRender)`; para el almacenamiento del navegador usar `IStorageService`.

```razor
@* AddWeightDialog.razor *@
<MudDialog>
    <DialogContent>
        <MudTextField @bind-Value="Weight" Label="@Localizer["Weight"]" />
    </DialogContent>
</MudDialog>
```

```csharp
// AddWeightDialog.razor.cs
namespace ControlPeso.Web.Components.Shared;

public partial class AddWeightDialog
{
    [Parameter] public decimal Weight { get; set; }
    private void OnSave() { /* delegar en un servicio de Application */ }
}
```

### Datos y Entity Framework (code-first)

14. El esquema se define en código: entidades en `Domain/Entities`, mapeo en `ControlPesoDbContext` y **migraciones EF Core** en `Infrastructure/Migrations`.
15. Flujo de un cambio de esquema: modificar entidad y `DbContext` → `dotnet ef migrations add <Nombre> --project src/ControlPeso.Infrastructure` → regenerar `docs/schema/postgresql_schema_v1.sql` → ajustar DTOs, servicios y tests. Detalle en [docs/DATABASE.md](../docs/DATABASE.md).
16. ❌ Nunca editar una migración ya aplicada. ❌ Nunca migrar al iniciar la aplicación: el esquema se aplica con `scripts/migrate-postgres.sh` antes del despliegue.
17. Los servicios crean contextos de corta vida con `IDbContextFactory` (Blazor Server es concurrente); un `DbContext` compartido no es seguro.
18. Consultas de solo lectura con `AsNoTracking()` y proyecciones `.Select()`; evitar N+1 y SQL crudo.
19. Fechas y horas en UTC (`timestamp without time zone` con valor UTC); `DateOnly` y `TimeOnly` para fecha y hora de un registro de peso.
20. Cadena de conexión solo por configuración: `ConnectionStrings:DefaultConnection`.

### Seguridad

21. ❌ Prohibido hardcodear secretos o cadenas de conexión con contraseña. Desarrollo: `appsettings.Development.json` (ignorado), User Secrets o `docker-compose.override.yml` (ignorado). Producción: variables de entorno. Ver [docs/SECRETS_MANAGEMENT.md](../docs/SECRETS_MANAGEMENT.md).
22. ❌ Prohibido `try/catch` vacíos; todo `catch` registra la excepción.
23. ❌ Prohibido loguear tokens, contraseñas, cadenas de conexión o cuerpos completos de request/response.
24. ❌ No se almacenan contraseñas: el único acceso es Google OAuth.
25. Cookie de autenticación `HttpOnly`, `Secure`, `SameSite=Lax`; CSP y demás cabeceras en `SecurityHeadersMiddleware`. Detalle en [docs/SECURITY.md](../docs/SECURITY.md).
26. Los endpoints de autenticación llevan `RequireRateLimiting("oauth")`.

### Logging

27. Inyectar `ILogger<T>` en los servicios; logging **estructurado** con parámetros con nombre (nunca interpolación de cadenas).
28. `LogInformation` para eventos de negocio, `LogWarning` para situaciones anómalas recuperables, `LogError(ex, ...)` en los `catch`, `LogCritical` solo si la aplicación no puede continuar.
29. Los emojis de los logs son una ayuda visual opcional (🤖 robots, 🗺️ sitemap, 💾 base de datos, ✅/❌ resultado, 🔐 autenticación); no son obligatorios.
30. Los errores no tienen alertas externas: solo los logs NDJSON (`/app/logs`). Ver [docs/soporte.md](../docs/soporte.md).

```csharp
_logger.LogInformation("Creating weight log for user {UserId} - Date: {Date}, Weight: {Weight}kg",
    dto.UserId, dto.Date, dto.Weight);
```

### Código C#

31. Mínima exposición: `private` > `internal` > `protected` > `public`.
32. `Nullable` e `ImplicitUsings` habilitados; `ArgumentNullException.ThrowIfNull(x)` y `string.IsNullOrWhiteSpace(x)` para validar argumentos.
33. Excepciones precisas (`ArgumentException`, `InvalidOperationException`, excepciones de dominio), nunca `Exception` genérica.
34. Métodos asíncronos terminan en `Async` y aceptan `CancellationToken`.
35. Records para DTOs inmutables; sufijos `Dto` y `Service`; interfaces con prefijo `I`.
36. Los comentarios explican el **porqué**, no el qué. No modificar código autogenerado.
37. Validación de entrada con FluentValidation en `Application`, nunca en `Web`.

### Testing y paquetes

38. xUnit con Moq o NSubstitute y FluentAssertions; un proyecto de tests por capa (`tests/*.Tests`); patrón AAA y sin ramas en los tests.
39. Los tests de PostgreSQL usan Testcontainers (`postgres:15`) y se omiten si no hay Docker.
40. Central Package Management: las versiones solo en `Directory.Packages.props`, nunca `Version` en un `PackageReference`.
41. Todo cambio de comportamiento lleva su test; antes de un commit: `dotnet build` y `dotnet test` en verde.

---

## Modelo de datos

Entidades de `src/ControlPeso.Domain/Entities`: `Users`, `WeightLogs`, `UserPreferences`, `UserNotifications` y `AuditLog`. Resumen de las dos principales (tipos reales):

```csharp
public class Users
{
    public Guid Id { get; set; }
    public string? GoogleId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }          // citext, único
    public int Role { get; set; }              // UserRole: 0=User, 1=Administrator
    public string? AvatarUrl { get; set; }
    public DateTime MemberSince { get; set; }  // UTC
    public decimal Height { get; set; }        // cm
    public int UnitSystem { get; set; }        // 0=Metric, 1=Imperial
    public DateOnly? DateOfBirth { get; set; }
    public string Language { get; set; }       // "es" | "en"
    public int Status { get; set; }            // 0=Active, 1=Inactive, 2=Pending
    public decimal? GoalWeight { get; set; }   // kg
    public decimal? StartingWeight { get; set; } // kg
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class WeightLogs
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public decimal Weight { get; set; }        // kg siempre
    public int DisplayUnit { get; set; }       // WeightUnit: 0=Kg, 1=Lb
    public string? Note { get; set; }
    public int Trend { get; set; }             // WeightTrend: 0=Up, 1=Down, 2=Neutral
    public DateTime CreatedAt { get; set; }
}
```

Los enums están en `Domain/Enums`; las conversiones entre entidades y DTOs, en `Application/Mapping`. Esquema completo en [docs/DATABASE.md](../docs/DATABASE.md).

---

## Patrones al generar o modificar código

**Código nuevo**: verificar la capa correcta, respetar las dependencias, reutilizar interfaces existentes, `CancellationToken` en métodos asíncronos, componentes MudBlazor, estados de UI, FluentValidation en la entrada, `ILogger<T>`, code-behind y textos localizados.

**Modificación**: no romper las capas ni las interfaces estables, mantener los tests en verde y, si cambia el modelo de datos, crear la migración (regla 15).

**Prohibiciones**: `@code { }` en componentes nuevos, HTML crudo con equivalente Mud*, `DbContext` en `Web`, lógica de negocio en la UI, secretos hardcodeados, `try/catch` vacíos, ignorar `CancellationToken`, dependencias circulares, migrar al iniciar y editar migraciones aplicadas.

---

## Blazor y rutas

Un controlador con `[HttpGet("/robots.txt")]` nunca se ejecuta porque Blazor captura la ruta primero. Los endpoints de raíz son **Minimal APIs registradas antes de `MapRazorComponents`**, como están en `Program.cs`:

```csharp
app.MapSeoEndpoints();               // /robots.txt y /sitemap.xml dinámicos
app.MapAuthenticationEndpoints();    // /api/auth/*
app.MapApplicationHealthEndpoints(); // /health y /health/ready
app.MapStaticAssets();
app.MapRazorComponents<App>();       // siempre último
```

---

## Configuración y secretos

Estructura en `src/ControlPeso.Web/`:

```
appsettings.json                       versionado, sin secretos
appsettings.Development.json.template  versionado, solo marcadores
appsettings.Production.json.template   versionado, solo marcadores
appsettings.Development.json           IGNORADO (valores reales)
appsettings.Production.json            IGNORADO (valores reales)
```

`pwsh scripts/setup-config.ps1` crea los archivos locales desde las plantillas sin sobrescribir los existentes. Detalle en [docs/CONFIG_MANAGEMENT.md](../docs/CONFIG_MANAGEMENT.md).

### Checklist pre-commit de seguridad

- [ ] No hay en staging `appsettings.Development.json`, `appsettings.Production.json`, `docker-compose.override.yml`, archivos `.env` ni `.secrets.local`.
- [ ] Plantillas, ejemplos y documentación usan solo marcadores (`CHANGE_ME`, `YOUR_*_HERE`, `<contraseña>`).
- [ ] `git diff --staged` no contiene `ClientSecret`, contraseñas, tokens ni claves de API.
- [ ] Scripts y compose reciben los secretos por parámetros o variables de entorno.
- [ ] `dotnet build` y `dotnet test` en verde.

```bash
git diff --staged | grep -iE "(ClientSecret|Password=|api.?key|token)"   # no debe mostrar valores reales
```

CI ejecuta además gitleaks sobre el diff de cada pull request. Si un secreto llega a Git, rotarlo de inmediato ([docs/CREDENTIAL_ROTATION_GUIDE.md](../docs/CREDENTIAL_ROTATION_GUIDE.md)).

---

## Git Flow

- `main`: producción. `develop`: integración. `feature/*` y `hotfix/*`: ramas temporales.
- ❌ Sin commits directos a `main` ni `develop`; todo cambio entra por pull request con CI en verde. Se elimina la rama tras el merge.
- Commits con Conventional Commits: `tipo(scope): descripción` (`feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`, `ci`).

```bash
git switch develop && git pull
git switch -c feature/nombre-descriptivo
# trabajar, probar y recién entonces commitear
git commit -m "feat(scope): descripción"
git push -u origin feature/nombre-descriptivo   # y abrir el pull request hacia develop
```

### CI/CD

- `.github/workflows/ci.yml` (pull requests y pushes a `develop` y `main`): restore, build, tests con cobertura, revisión de paquetes NuGet vulnerables (falla con High o Critical), gitleaks sobre el diff del PR y prueba de humo de la imagen (usuario no root y `/health`).
- `.github/workflows/release.yml` (push a `main`): crea el tag `vX.Y.Z` con `mathieudutour/github-tag-action` y un GitHub Release. El bump lo controlan los Conventional Commits (`feat` minor, `fix` patch, `BREAKING CHANGE` major; por defecto minor).
- ❌ No crear tags a mano. La versión de ensamblado (`Directory.Build.props`) es independiente del tag.

---

## Despliegue

Producción: Docker en Proxmox CT 100 (`/opt/controlpeso`) detrás de Cloudflare y NPMplus; PostgreSQL en un host separado (CT 205) con copias diarias de `pg_dump`. Procedimiento completo en [docs/DEPLOYMENT.md](../docs/DEPLOYMENT.md) y topología en [docs/infraestructura.md](../docs/infraestructura.md).

Reglas:

- La imagen corre como usuario no root (`app`, UID 1654), escucha en 8080 y se construye antes con `docker build`; los compose **no construyen**.
- El esquema se aplica con `scripts/migrate-postgres.sh` antes de arrancar la nueva versión.
- Volúmenes persistentes: `/app/logs`, `/app/keys` (Data Protection) y `/app/wwwroot/uploads`.
- Probar en local antes de desplegar (`docker compose up -d`, `curl http://localhost:8080/health`), y comprobar después del despliegue `/health`, `/health/ready`, `robots.txt`, `sitemap.xml`, el login y los logs.
- Los scripts PowerShell de `scripts/deploy-*.ps1` son anteriores a PostgreSQL y a la imagen no root: revisarlos antes de usarlos.
- Rollback: volver a la imagen anterior y, si hace falta, restaurar la copia de seguridad ([docs/BACKUP_SYSTEM.md](../docs/BACKUP_SYSTEM.md)).

---

## Referencias

- [README.md](../README.md): descripción, stack y arranque rápido.
- [docs/ARCHITECTURE.md](../docs/ARCHITECTURE.md), [docs/documentacionfuncional.md](../docs/documentacionfuncional.md): arquitectura y funcionalidades.
- [docs/DATABASE.md](../docs/DATABASE.md), [docs/schema/postgresql_schema_v1.sql](../docs/schema/postgresql_schema_v1.sql): modelo de datos.
- [docs/soporte.md](../docs/soporte.md): logs, diagnóstico y comandos.
- [MudBlazor API](https://mudblazor.com/api#components)

## FAQ

- **¿Bootstrap o HTML crudo?** No: MudBlazor exclusivamente.
- **¿Cómo cambio el esquema?** Entidad y `DbContext` → migración → script idempotente (regla 15).
- **¿Lógica en `.razor`?** No: code-behind y servicios de `Application`.
- **¿`DbContext` en un componente?** No: se inyectan servicios de `Application`.
- **¿Peso en libras?** No: siempre kg; la conversión es solo de presentación.
- **¿Pull request directo a `main`?** No: `feature/*` → `develop` → `main`.
- **¿Dónde se valida la entrada?** FluentValidation en `Application`.
- **¿Documentación nueva?** Se actualizan los documentos existentes de `docs/`; un `.md` nuevo solo si hay un tema sin documento propio.
