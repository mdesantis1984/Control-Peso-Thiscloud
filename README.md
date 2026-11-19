# Control Peso Thiscloud

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)
![MudBlazor](https://img.shields.io/badge/MudBlazor-9.0-594AE2)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-4169E1?logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)

Aplicación web de control de peso corporal construida con **Blazor Server (.NET 10)** y **MudBlazor**, siguiendo **Clean Architecture (Onion)**. Los datos se guardan en **PostgreSQL** y la aplicación se despliega como imagen Docker.

- **Producción**: <https://controlpeso.thiscloud.com.ar>
- **Repositorio**: <https://github.com/mdesantis1984/Control-Peso-Thiscloud>

## Características

- **Inicio de sesión con Google OAuth 2.0** (sin contraseñas locales). La cookie de sesión es persistente: 30 días con renovación deslizante.
- **Dashboard** con métricas actuales, gráfico de evolución, registro de peso (fecha, hora, nota y tendencia automática) y exportación de los registros a CSV.
- **Historial** con búsqueda, filtros y paginación.
- **Tendencias** con proyecciones sobre el historial del usuario.
- **Perfil**: altura, fecha de nacimiento, objetivo de peso, sistema de unidades (métrico o imperial) y avatar con recorte; el servidor lo procesa con SkiaSharp (MIT) y lo guarda como WebP.
- **Panel de administración** (rol `Administrator`): gestión de usuarios, roles y estado, exportación CSV.
- **Notificaciones** persistidas en base de datos con campana y panel.
- **Internacionalización**: español (`es-AR`, idioma por defecto) e inglés (`en-US`) mediante `IStringLocalizer` y recursos `.resx`.
- **Tema claro y oscuro**, con la preferencia guardada en la base de datos (usuarios autenticados) o en `localStorage`.
- **SEO**: `robots.txt` y `sitemap.xml` generados dinámicamente con Minimal APIs.
- **Analítica opcional**: Google Analytics 4 solo se activa si hay un `GoogleAnalytics:MeasurementId` real; Cloudflare Web Analytics se habilita desde Cloudflare.
- **Páginas legales**: privacidad, términos, licencias de terceros y changelog (`/privacy`, `/terms`, `/licenses`, `/changelog`).
- **Operación**: `/health` (liveness) y `/health/ready` (readiness, consulta la base de datos), cabeceras de seguridad (CSP, HSTS), rate limiting en el login y logs estructurados NDJSON con redacción de secretos.

## Stack

| Área | Tecnología |
|------|------------|
| Runtime / UI | .NET 10, Blazor Server, MudBlazor 9 |
| Datos | PostgreSQL 15, Entity Framework Core 10, Npgsql (`citext` para el email) |
| Autenticación | Cookies + Google OAuth (LinkedIn está implementado pero desactivado: solo se registra si se configura) |
| Validación | FluentValidation |
| Imágenes | SkiaSharp (avatares) |
| Logging | Serilog vía `ThisCloud.Framework.Loggings` (NDJSON, redacción, `X-Correlation-Id`) |
| Tests | xUnit, Moq, NSubstitute, FluentAssertions, bUnit, Testcontainers (PostgreSQL) |
| Paquetes | Central Package Management (`Directory.Packages.props`) |
| Despliegue | Docker (imagen no root), Docker Compose, Cloudflare + NPMplus como proxy inverso |

## Arquitectura

```
ControlPeso.slnx
├── src/
│   ├── ControlPeso.Domain/            Entidades, enums, excepciones (sin dependencias)
│   ├── ControlPeso.Application/       Servicios, DTOs, validadores, interfaces
│   ├── ControlPeso.Infrastructure/    DbContext (EF Core + Npgsql), migraciones, servicios externos
│   ├── ControlPeso.Shared.Resources/  Recursos de localización (.resx)
│   └── ControlPeso.Web/               Blazor Server: páginas, componentes, middleware
├── tests/                             Un proyecto de tests por capa
├── docs/                              Documentación
└── scripts/                           Utilidades (migración PostgreSQL, despliegue)
```

Dependencias entre capas: `Domain` no depende de nada; `Application` solo de `Domain`; `Infrastructure` de `Domain` y `Application`; `Web` de `Application` e `Infrastructure` (solo para registrar la inyección de dependencias). Detalle en [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Inicio rápido

### Prerrequisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker (Compose v2)
- Un cliente OAuth de Google para iniciar sesión (ver [docs/GOOGLE_OAUTH_SETUP.md](docs/GOOGLE_OAUTH_SETUP.md))

### Compilar y probar

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

Los tests de integración de PostgreSQL usan Testcontainers: necesitan Docker y se omiten si no está disponible.

### Ejecutar con Docker Compose (PostgreSQL incluido)

```bash
# 1. Construir la imagen (el compose usa la imagen local, no construye)
docker build -t controlpeso-web:latest -f Dockerfile .

# 2. Contraseña de PostgreSQL (variable de entorno o archivo .env local, fuera de Git)
export POSTGRES_PASSWORD='<contraseña>'

# 3. Levantar solo la base de datos
docker compose up -d postgres

# 4. Crear el esquema: la aplicación NO migra al arrancar
PGHOST=localhost PGDATABASE=controlpeso PGUSER=controlpeso PGPASSWORD="$POSTGRES_PASSWORD" \
  scripts/migrate-postgres.sh

# 5. Credenciales de Google en docker-compose.override.yml (ignorado por Git), luego:
docker compose up -d

# 6. Verificar
curl http://localhost:8080/health
curl http://localhost:8080/health/ready
```

La aplicación queda en <http://localhost:8080>. Las variables que debe definir el `docker-compose.override.yml` local están en [docs/SECRETS_MANAGEMENT.md](docs/SECRETS_MANAGEMENT.md); para probar por HTTP plano agregar `DISABLE_HTTPS_REDIRECTION=true`.

### Ejecutar con `dotnet run`

```bash
docker compose up -d postgres     # o cualquier PostgreSQL 15 accesible
# Configuración local: copiar src/ControlPeso.Web/appsettings.Development.json.template
# a appsettings.Development.json (ignorado por Git) y completar los valores
# (también: pwsh scripts/setup-config.ps1)
dotnet run --project src/ControlPeso.Web
```

Detalle de la configuración en [docs/CONFIG_MANAGEMENT.md](docs/CONFIG_MANAGEMENT.md).

## Base de datos

- El esquema vive en el código: migraciones EF Core en `src/ControlPeso.Infrastructure/Migrations` (migración inicial `InitialPostgreSql`).
- `docs/schema/postgresql_schema_v1.sql` es el script idempotente generado a partir de esas migraciones.
- `scripts/migrate-postgres.sh` genera y aplica el script con `psql`; la aplicación nunca migra al iniciar.
- Cadena de conexión: `ConnectionStrings:DefaultConnection` (`ConnectionStrings__DefaultConnection` como variable de entorno).

Más información en [docs/DATABASE.md](docs/DATABASE.md).

## Configuración y secretos

Ningún secreto se versiona. En desarrollo se usan `appsettings.Development.json` (a partir de la plantilla), User Secrets o un `docker-compose.override.yml` local; en producción, variables de entorno. Ver [docs/CONFIG_MANAGEMENT.md](docs/CONFIG_MANAGEMENT.md), [docs/SECRETS_MANAGEMENT.md](docs/SECRETS_MANAGEMENT.md) y [docs/CREDENTIAL_ROTATION_GUIDE.md](docs/CREDENTIAL_ROTATION_GUIDE.md).

## Despliegue

La imagen Docker ejecuta la aplicación como usuario no root (`app`, UID 1654) en el puerto 8080 y guarda logs (`/app/logs`), claves de Data Protection (`/app/keys`) y avatares (`/app/wwwroot/uploads`) en volúmenes. Producción usa `docker-compose.production.yml` contra una base PostgreSQL externa. Procedimiento completo en [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md); topología en [docs/infraestructura.md](docs/infraestructura.md); copias de seguridad en [docs/BACKUP_SYSTEM.md](docs/BACKUP_SYSTEM.md).

## CI/CD

- `.github/workflows/ci.yml` (pull requests y pushes a `develop` y `main`): build, tests, revisión de paquetes NuGet vulnerables (falla con High o Critical), gitleaks sobre el diff del PR y prueba de humo de la imagen Docker (contenedor no root y `/health`).
- `.github/workflows/release.yml`: cada push a `main` crea un tag semántico `vX.Y.Z` y un GitHub Release. La versión de ensamblado (`Directory.Build.props`) es independiente de ese tag.

## Contribuir

- Ramas: `feature/*` → PR a `develop` → `main`. No se hacen commits directos a `main` ni `develop`.
- Commits con [Conventional Commits](https://www.conventionalcommits.org/) (`feat(scope): ...`, `fix(scope): ...`). No crear tags a mano: lo hace el workflow de release.
- Antes del commit: `dotnet build` y `dotnet test` en verde, y ningún secreto en el diff.
- Convenciones de código y arquitectura para asistentes y colaboradores: [.github/copilot-instructions.md](.github/copilot-instructions.md).

## Documentación

| Documento | Contenido |
|-----------|-----------|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Arquitectura y decisiones técnicas |
| [docs/documentacionfuncional.md](docs/documentacionfuncional.md) | Documentación funcional |
| [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) | Despliegue con Docker y migración de esquema |
| [docs/infraestructura.md](docs/infraestructura.md) | Topología de producción, puertos, volúmenes |
| [docs/DATABASE.md](docs/DATABASE.md) | Modelo de datos y migraciones |
| [docs/BACKUP_SYSTEM.md](docs/BACKUP_SYSTEM.md) | Copias de seguridad y restauración de PostgreSQL |
| [docs/CONFIG_MANAGEMENT.md](docs/CONFIG_MANAGEMENT.md) | Plantillas de configuración |
| [docs/SECRETS_MANAGEMENT.md](docs/SECRETS_MANAGEMENT.md) | Gestión de secretos |
| [docs/CREDENTIAL_ROTATION_GUIDE.md](docs/CREDENTIAL_ROTATION_GUIDE.md) | Rotación de credenciales |
| [docs/GOOGLE_OAUTH_SETUP.md](docs/GOOGLE_OAUTH_SETUP.md) | Configuración de Google OAuth |
| [docs/SECURITY.md](docs/SECURITY.md) | Modelo de seguridad |
| [docs/soporte.md](docs/soporte.md) | Soporte y resolución de problemas |
| [docs/CLOUDFLARE_ANALYTICS.md](docs/CLOUDFLARE_ANALYTICS.md) | Cloudflare Web Analytics |
| [docs/LOGGING_BEST_PRACTICES.md](docs/LOGGING_BEST_PRACTICES.md), [docs/LOGGING_QUERY_GUIDE.md](docs/LOGGING_QUERY_GUIDE.md) | Logging y consulta de logs |
| [docs/SEO_INFRASTRUCTURE.md](docs/SEO_INFRASTRUCTURE.md) | `robots.txt` y `sitemap.xml` |
| [docs/AVATAR_ARCHITECTURE.md](docs/AVATAR_ARCHITECTURE.md) | Avatares |
| [docs/NOTIFICATION_SYSTEM.md](docs/NOTIFICATION_SYSTEM.md) | Notificaciones |
| [docs/technical/](docs/technical) | Notas técnicas de Blazor |
| [docs/legal/](docs/legal) | Política de privacidad, términos, licencias de terceros y changelog |

## Licencia y contacto

El repositorio no incluye un archivo de licencia; las dependencias de terceros y sus licencias están en [docs/legal/THIRD_PARTY_LICENSES.es.md](docs/legal/THIRD_PARTY_LICENSES.es.md). © Thiscloud Services.

- Web: <https://thiscloud.com.ar>
- Correo: <contacto@thiscloud.com.ar>
- GitHub: [@mdesantis1984](https://github.com/mdesantis1984)
