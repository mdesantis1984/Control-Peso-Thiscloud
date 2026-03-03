# Control Peso Thiscloud

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)
![MudBlazor](https://img.shields.io/badge/MudBlazor-8.0.0-594AE2)
![SQL Server Express](https://img.shields.io/badge/SQL%20Server-Express-CC2927?logo=microsoftsqlserver)
![License](https://img.shields.io/badge/License-MIT-green)
![Progress](https://img.shields.io/badge/Progress-53.2%25-yellow)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)

Aplicación web de control de peso corporal construida con **Blazor Server (.NET 10)** y **MudBlazor**, siguiendo **Clean Architecture (Onion)** y **Database First**.

> **Estado del proyecto**: 🟢 **EN DESARROLLO ACTIVO** — Fase 3/8 completada (53.2%)  
> **Backend**: ✅ 100% completado — Application Layer (90.7% test coverage)  
> **Frontend**: 🔵 ~60% completado — Funcional + i18n, pixel perfect pendiente  
> **i18n**: ✅ 100% completado — Español (es-AR) / English (en-US)  
> **Database**: SQL Server Express (Linux) — Database First workflow  
> **Deployment**: ✅ Producción — Docker + NPMplus (SSL)  
> **Última actualización**: 2025-03-03  
> **Release**: [v5.0.0](https://github.com/mdesantis1984/Control-Peso-Thiscloud/releases/tag/v5.0.0)  
> **URL**: [https://controlpeso.thiscloud.com.ar](https://controlpeso.thiscloud.com.ar)

## ✨ Características

### ✅ Implementadas (Backend + Core UI)

- ✅ **Autenticación OAuth 2.0**: Google OAuth (LinkedIn OAuth disponible)
- ✅ **Dashboard interactivo**: Métricas actuales, gráficos de evolución (MudBlazor)
- ✅ **Registro de peso**: Fecha, hora, notas, tendencia automática
- ✅ **Historial completo**: Búsqueda, filtros avanzados, paginación
- ✅ **Análisis de tendencias**: Proyecciones con gráficos interactivos
- ✅ **Panel de administración**: Gestión de usuarios, roles, CSV export
- ✅ **Sistema de notificaciones**: Históricas con badge, panel interactivo, persistencia DB
- ✅ **i18n bilingüe**: Español (es-AR) / English (en-US) — 452 strings traducidos
- ✅ **Unidades duales**: Métrico (kg, cm) / Imperial (lb, ft/in)
- ✅ **Temas**: Dark/Light mode con MudBlazor ThemeManager
- ✅ **SEO optimizado**: robots.txt (50+ AI crawlers), sitemap.xml dinámico, meta tags
- ✅ **Analytics**: Google Analytics 4 + Cloudflare Analytics
- ✅ **Accesibilidad**: WCAG 2.1 AA parcial (keyboard nav, ARIA labels)
- ✅ **Seguridad**: HTTPS obligatorio, CSP headers, antiforgery tokens, cookie segura

### ⏳ Pendientes (Frontend - Pixel Perfect)

- 🔴 **Refinar diseño visual** → Ajustar a prototipo Google AI Studio
- 🔴 **Optimizar responsive design** → Mobile/tablet/desktop perfecto
- 🔴 **Pulir espaciados y alineaciones** → Consistencia visual completa
- 🔴 **Mejorar transiciones** → Animaciones suaves MudBlazor
- 🔴 **Iconografía consistente** → Revisar todos los iconos (Material Design)
- 🔴 **Testing UX exhaustivo** → Usuarios reales + iteraciones de feedback
- 🔴 **A11y testing manual** → Keyboard navigation + screen readers completo
- 🔴 **Performance optimization** → Lighthouse 90+ score (todas las métricas)

## 🌍 Internacionalización (i18n)

Control Peso Thiscloud soporta **múltiples idiomas** usando **ASP.NET Core `IStringLocalizer`** + archivos de recursos `.resx`.

### Idiomas Soportados

- 🇦🇷 **Español (Argentina)** — `es-AR` — Idioma por defecto
- 🇺🇸 **English (United States)** — `en-US`

### Cambiar Idioma

1. Click en el **selector de idioma** (LanguageSelector) en la barra de navegación superior
2. Seleccionar idioma deseado del dropdown
3. La página se **recarga automáticamente** (`forceLoad: true`) aplicando el nuevo idioma
4. La selección se **persiste** vía cookie `.AspNetCore.Culture` (duración: 1 año)

### Características i18n

✅ **452 strings traducidos** (UI + validators + meta tags + mensajes)  
✅ **36 archivos `.resx`** (18 componentes/páginas × 2 idiomas)  
✅ **8 páginas traducidas**: Dashboard, Profile, History, Trends, Admin, Login, Error, Home  
✅ **7 componentes traducidos**: MainLayout, NavMenu, AddWeightDialog, StatsCard, TrendCard, WeightChart, NotificationBell  
✅ **Validators traducidos**: Mensajes de error de FluentValidation en ambos idiomas  
✅ **Meta tags SEO dinámicos**: `<title>`, `description`, `keywords` según idioma seleccionado  
✅ **Persistencia cross-session**: Cookie `.AspNetCore.Culture` con 1 año de duración  
✅ **Recarga instantánea**: `NavigationManager.NavigateTo(uri, forceLoad: true)` aplica nuevo idioma inmediatamente

### Arquitectura i18n

```
src/ControlPeso.Web/
└── Resources/
    ├── Pages/
    │   ├── Dashboard.resx          ← Español (default)
    │   ├── Dashboard.en-US.resx    ← Inglés
    │   ├── Profile.resx
    │   ├── Profile.en-US.resx
    │   └── ... (8 páginas × 2 idiomas)
    └── Components/
        ├── Shared/
        │   ├── MainLayout.resx
        │   ├── MainLayout.en-US.resx
        │   ├── NavMenu.resx
        │   ├── NavMenu.en-US.resx
        │   └── ... (7 componentes × 2 idiomas)
```

**Middleware**:
- `RequestLocalizationOptions` en `Program.cs`
- `supportedCultures`: `es-AR` (default), `en-US`
- `CookieRequestCultureProvider` para persistencia
- Fallback automático: Si falta traducción en inglés → cae a español (default)

### Agregar Nuevos Idiomas (Futuro)

Para agregar soporte de idiomas adicionales (ej: francés `fr-FR`, portugués `pt-BR`):

1. Crear archivos `.resx` con código de cultura: `Dashboard.fr-FR.resx`, `Dashboard.pt-BR.resx`
2. Agregar cultura a `supportedCultures` en `Program.cs`:
   ```csharp
   var supportedCultures = new[] { "es-AR", "en-US", "fr-FR", "pt-BR" };
   ```
3. Agregar opción en `LanguageSelector.razor.cs`:
   ```csharp
   { "fr-FR", "🇫🇷 Français" },
   { "pt-BR", "🇧🇷 Português" }
   ```
4. Traducir todos los strings existentes (~452 strings por idioma)

## 🏗️ Arquitectura

Control Peso Thiscloud sigue **Clean Architecture (Onion)** con **SOLID** y separación estricta de capas:

```
ControlPeso.Thiscloud/
├── src/
│   ├── ControlPeso.Domain/         ← Núcleo (ZERO dependencias)
│   │   ├── Entities/               ← Entidades scaffolded (EF Core)
│   │   ├── Enums/                  ← UserRole, UnitSystem, WeightTrend
│   │   └── Exceptions/             ← DomainException, NotFoundException
│   ├── ControlPeso.Application/    ← Lógica de negocio (Depende: Domain)
│   │   ├── Services/               ← WeightLogService, UserService
│   │   ├── DTOs/                   ← WeightLogDto, UserProfileDto
│   │   ├── Interfaces/             ← IWeightLogService, IUserService
│   │   ├── Validators/             ← FluentValidation (CreateWeightLogDtoValidator)
│   │   └── Mapping/                ← Mappers (Entity ↔ DTO)
│   ├── ControlPeso.Infrastructure/ ← Persistencia (Depende: Domain + Application)
│   │   ├── Data/                   ← ControlPesoDbContext (EF Core)
│   │   ├── Repositories/           ← Repository implementations (si necesario)
│   │   └── Configuration/          ← Service registration extensions
│   └── ControlPeso.Web/            ← Blazor Server UI (Depende: Application + Infrastructure para DI)
│       ├── Pages/                  ← Dashboard.razor, Profile.razor, History.razor
│       ├── Components/Shared/      ← AddWeightDialog, NotificationBell, NavMenu
│       ├── Resources/              ← Archivos .resx para i18n (36 archivos)
│       ├── Services/               ← NotificationService, ThemeService
│       └── Program.cs              ← Startup, DI, Middleware pipeline
├── tests/
│   ├── ControlPeso.Application.Tests/  ← 176 tests, 90.7% coverage
│   └── ControlPeso.Infrastructure.Tests/
├── docs/
│   ├── infraestructura.md          ← Doc maestro infraestructura
│   ├── documentacionfuncional.md   ← Doc maestro funcional
│   ├── soporte.md                  ← Logging, troubleshooting
│   └── schema/schema_v1.sql        ← CONTRATO MAESTRO base de datos
├── Dockerfile                      ← Multi-stage build (.NET 10)
├── docker-compose.yml              ← Orquestación containers
└── Directory.Packages.props        ← Central Package Management (CPM)
```

### Dependencias entre Capas (INQUEBRANTABLE)

```
Web (Presentation)
    ↓ Depends on
Application
    ↓ Depends on
Domain ← (Zero dependencies)
    ↑ Depended by
Infrastructure
```

**Reglas**:
- ❌ Domain NO depende de NADA externo
- ✅ Application depende SOLO de Domain
- ✅ Infrastructure depende de Domain + Application
- ✅ Web depende de Application + Infrastructure (solo para DI)
- ❌ NUNCA acceso `DbContext` desde Web
- ❌ NUNCA lógica de negocio en `.razor` files

---

## 🛠️ Stack Tecnológico

### Core Framework
- **.NET 10** (`net10.0`) — Target framework
- **C# 14** — Latest language features
- **Blazor Server** — Interactive server-side rendering
- **ASP.NET Core** — Web host, middleware pipeline

### UI Framework
- **MudBlazor 8.0.0** — Material Design component library (**exclusivo**)
- **MudBlazor.ThemeManager** — Dark/Light mode switcher
- **Google Fonts (Roboto)** — Typography

### Database & ORM
- **SQL Server Express (Linux)** — Base de datos en todos los entornos
- **Entity Framework Core 9.0.1** — ORM (**Database First** mode)
- **Microsoft.EntityFrameworkCore.SqlServer** — SQL Server provider

### Authentication
- **ASP.NET Core Identity** — Cookie-based authentication
- **Google OAuth 2.0** — External authentication provider

### Validation & Mapping
- **FluentValidation 11.11.0** — DTO validation
- **Manual mapping** — Entity ↔ DTO (no AutoMapper)

### Logging & Analytics
- **ThisCloud.Framework.Loggings.Serilog 1.0.86** — Structured logging + redaction + correlation
- **Google Analytics 4** — User behavior tracking
- **Cloudflare Analytics** — Traffic insights

### Testing
- **xUnit** — Unit testing framework
- **Moq** — Mocking framework
- **FluentAssertions** — Assertion library
- **Coverlet** — Code coverage (90.7% Application layer)

### Deployment
- **Docker** — Containerización multi-stage build
- **Docker Compose** — Orquestación containers
- **NPMplus** — Reverse proxy (Nginx) + SSL (Certbot Let's Encrypt)
- **Proxmox VM** — Servidor Ubuntu 22.04 LTS (10.0.0.100)

---

## 🛠️ Tecnologías

| Componente | Tecnología |
|------------|-----------|
| **Framework** | .NET 10.0 (LTS) |
| **UI** | Blazor Server |
| **Componentes UI** | MudBlazor 8.0.0 |
| **ORM** | Entity Framework Core 9.0.1 (Database First) |
| **Base de datos** | SQLite (dev/MVP) → SQL Server (prod) |
| **Validación** | FluentValidation 11.11.0 |
| **Logging** | Serilog 8.0.3 + **ThisCloud.Framework.Loggings 1.0.86** |
| **Testing** | xUnit 2.9.2 + Moq 4.20.72 + FluentAssertions 7.0.0 |
| **Autenticación** | Google OAuth 2.0 |
| **Analytics** | Google Analytics 4 + Cloudflare Analytics |

### ThisCloud.Framework Integration

Este proyecto utiliza **ThisCloud.Framework.Loggings** para logging estructurado enterprise-grade:
- ✅ Serilog con Console + File sinks (NDJSON rolling)
- ✅ Redaction automática de secretos
- ✅ Correlation ID en todos los logs
- ✅ Configuración centralizada en `appsettings.json`

Ver [THISCLOUD_FRAMEWORK_INTEGRATION.md](docs/THISCLOUD_FRAMEWORK_INTEGRATION.md) para detalles.

## 🚀 Inicio rápido

### Prerrequisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio 2022 17.12+](https://visualstudio.microsoft.com/) o [VS Code](https://code.visualstudio.com/)
- [SQLite](https://www.sqlite.org/download.html) (opcional, para inspeccionar DB)

### Clonar repositorio

```bash
git clone https://github.com/mdesantis1984/Control-Peso-Thiscloud.git
cd Control-Peso-Thiscloud
```

### Restaurar dependencias

```bash
dotnet restore
```

### Compilar solución

```bash
dotnet build
```

### Ejecutar tests

```bash
dotnet test
```

### Ejecutar aplicación

```bash
cd src/ControlPeso.Web
dotnet run
```

Abre tu navegador en `https://localhost:5001`

## 🐳 Docker Deployment

Despliegue local con **Docker Compose** (recomendado para producción):

```bash
# 1. Copiar template de credenciales
cp docker-compose.override.yml.example docker-compose.override.yml

# 2. Editar docker-compose.override.yml con tus credenciales OAuth REALES
nano docker-compose.override.yml  # o notepad en Windows

# 3. Construir y ejecutar
docker-compose up -d --build

# 4. Acceder a la aplicación
# http://localhost:8080
```

⚠️ **IMPORTANTE**: `docker-compose.override.yml` contiene tus credenciales sensibles y **NO** se sube al repositorio Git (ya está en `.gitignore`).

### Scripts de ayuda

```bash
# Linux/macOS
chmod +x docker-helper.sh
./docker-helper.sh

# Windows PowerShell
.\docker-helper.ps1
```

Ver [docs/DOCKER.md](docs/DOCKER.md) para documentación completa de Docker.

## 🗄️ Base de datos (Database First)

Control Peso Thiscloud usa **SQL Server Express (Linux)** con **Entity Framework Core 9.0.1** en modo **Database First**.

### Filosofía Database First

El contrato maestro de datos está en **SQL** (`docs/schema/schema_v1.sql`). Las entidades C# se generan mediante **scaffold** desde la base de datos.

**Reglas obligatorias (INQUEBRANTABLE)**:
1. ✅ **TODO cambio de estructura EMPIEZA en el SQL** (`schema_v1.sql`)
2. ✅ Luego se **aplica el SQL** contra SQL Server
3. ✅ Luego se ejecuta **scaffold de EF Core** para regenerar entidades
4. ❌ **NUNCA** se modifican las entidades C# manualmente (son auto-generated)
5. ❌ **NUNCA** se usan Data Annotations en entidades scaffolded
6. ❌ **NUNCA** se usan migrations code-first

### Flujo Completo Database First

```bash
# 1. Modificar el contrato SQL
nano docs/schema/schema_v1.sql

# 2. Aplicar contra SQL Server (dev/producción)
sqlcmd -S localhost -U sa -P <password> -i docs/schema/schema_v1.sql

# 3. Scaffold entidades desde DB (forzar regeneración)
dotnet ef dbcontext scaffold \
  "Server=localhost;Database=ControlPeso;User=sa;Password=<password>;TrustServerCertificate=true" \
  Microsoft.EntityFrameworkCore.SqlServer \
  --context ControlPesoDbContext \
  --output-dir ../ControlPeso.Domain/Entities \
  --context-dir . \
  --project src/ControlPeso.Infrastructure \
  --force

# 4. Mover DbContext a Infrastructure (si quedó en Domain)
# 5. Ajustar value converters si necesario (Guid, DateTime, enums)
# 6. Actualizar DTOs y servicios según nuevos campos
# 7. Ejecutar tests para validar cambios
dotnet test

# 8. Commit (SOLO si tests pasan)
git add docs/schema/schema_v1.sql src/ControlPeso.Domain/Entities/
git commit -m "feat(db): add new field to WeightLog table"
```

### Connection String

**Desarrollo** (User Secrets):
```json
{
  "ConnectionStrings": {
    "ControlPesoDb": "Server=localhost;Database=ControlPeso;User=sa;Password=<your_password>;TrustServerCertificate=true"
  }
}
```

**Producción** (Environment Variables):
```bash
ConnectionStrings__ControlPesoDb="Server=localhost;Database=ControlPeso;User=sa;Password=<password>;TrustServerCertificate=true"
```

### Modelo de Datos (Resumen)

**Tablas principales**:
- `Users` — Usuarios del sistema (Google OAuth, roles, preferencias)
- `WeightLogs` — Registros de peso (fecha, hora, peso kg, tendencia, notas)
- `UserPreferences` — Preferencias de usuario (dark mode, unidades, notificaciones)
- `UserNotifications` — Notificaciones históricas (título, mensaje, leídas, fecha)

**Tipos de datos**:
- IDs: `UNIQUEIDENTIFIER` (SQL) → `Guid` (C#) con value converters
- Fechas: `DATETIME2` (SQL) → `DateTime` (C#)
- Booleans: `BIT` (SQL) → `bool` (C#)
- Enums: `INT` (SQL) → `int` (C#) → Manual enums en Domain/Enums

**Enums (No scaffolded — Manuales)**:
```csharp
public enum UserRole { User = 0, Administrator = 1 }
public enum UserStatus { Active = 0, Inactive = 1, Pending = 2 }
public enum UnitSystem { Metric = 0, Imperial = 1 }
public enum WeightUnit { Kg = 0, Lb = 1 }
public enum WeightTrend { Up = 0, Down = 1, Neutral = 2 }
public enum NotificationSeverity { Normal = 0, Info = 1, Success = 2, Warning = 3, Error = 4 }
```

## 📦 Gestión de paquetes (CPM)

El proyecto usa **Central Package Management** (`Directory.Packages.props`).

- ✅ Versiones centralizadas
- ✅ Control de dependencias transitivas
- ✅ Evita conflictos de versiones

## 🔒 Seguridad

- ✅ HTTPS obligatorio
- ✅ Cookie segura: HttpOnly + Secure + SameSite=Strict
- ✅ Antiforgery tokens
- ✅ CSP headers restrictivos
- ✅ X-Frame-Options DENY
- ✅ Validación de entrada con FluentValidation
- ✅ Secretos en User Secrets / env vars (nunca hardcoded)

## 📝 Documentación

### 📊 Estado del Proyecto — Fase 3/8 (53.2% completado)

**✅ Fases Completadas**:
- ✅ **Fase 0**: Setup completo (7/7 tareas)
- ✅ **Fase 1**: Schema SQL + Scaffold + Domain (7/7 tareas)
- ✅ **Fase 1.5**: Integración ThisCloud.Framework + .NET 10 (10/10 tareas)
- ✅ **Fase 2**: Application Layer completo (8/8 tareas, 176 tests, 90.7% cobertura)
- ✅ **Fase 3**: Infrastructure Layer (3/3 tareas)

**🔵 Fase Actual**:
- 🔵 **Fase 4**: Autenticación OAuth (en progreso)
  - ✅ Google OAuth 2.0 implementado
  - ⏳ LinkedIn OAuth disponible (no activado)

**⏳ Fases Pendientes**:
- ⏳ **Fase 5**: UI Core (Layout + Dashboard + componentes base)
- ⏳ **Fase 6**: Páginas secundarias (Profile + History + Trends)
- ⏳ **Fase 7**: Admin Panel (gestión usuarios + CSV export)
- ⏳ **Fase 8**: SEO + Analytics + Security
  - ✅ SEO parcial: robots.txt (50+ AI crawlers), sitemap.xml dinámico
  - ✅ Analytics: Google Analytics 4 + Cloudflare
  - ✅ Security: HTTPS, CSP headers, antiforgery tokens

**📈 Progreso Global**: 53.2% (Fase 3/8 completada)

### 📚 Documentación Técnica (Consolidada)

- **[infraestructura.md](docs/infraestructura.md)** — Doc maestro: Arquitectura, Deployment, SEO, Seguridad, DB, Analytics
- **[documentacionfuncional.md](docs/documentacionfuncional.md)** — Doc maestro: Planes, Sistemas, WCAG, Tests
- **[soporte.md](docs/soporte.md)** — Doc maestro: Logging, Troubleshooting, Comandos útiles
- **[schema_v1.sql](docs/schema/schema_v1.sql)** — Contrato maestro base de datos (Database First)
- **[.github/copilot-instructions.md](.github/copilot-instructions.md)** — Reglas Copilot: Arquitectura, código, deployment

### 📄 Documentación Histórica (Planes)

- **[Plan_ControlPeso_Thiscloud_v1_0.md](docs/Plan_ControlPeso_Thiscloud_v1_0.md)** — Plan maestro v1.0 (Fases 0-8)
- **[Plan_Fase_9_Pixel_Perfect.md](docs/Plan_Fase_9_Pixel_Perfect.md)** — Fase 9: UI refinements (completado)
- **[Plan_Fase_10_Globalizacion.md](docs/Plan_Fase_10_Globalizacion.md)** — Fase 10: i18n completo (completado)

## 🧪 Testing & Cobertura

### Target vs. Actual

| Capa | Target Mínimo | Cobertura Actual | Tests | Estado |
|------|---------------|------------------|-------|--------|
| **Application** | 85% | **90.7%** | 176/176 passing | ✅ Completo |
| **Infrastructure** | 85% | ⏳ Pendiente | 0 tests | 🔴 Pendiente |
| **Domain** | N/A | N/A | N/A | ⚪ Sin lógica testeable |

### Ejecutar Tests

```bash
# Ejecutar todos los tests
dotnet test

# Ejecutar tests con cobertura
dotnet test --collect:"XPlat Code Coverage"

# Generar reporte HTML (requiere reportgenerator)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:**/coverage.cobertura.xml \
  -targetdir:coverage-report \
  -reporttypes:Html

# Abrir reporte
start coverage-report/index.html  # Windows
open coverage-report/index.html   # macOS
xdg-open coverage-report/index.html  # Linux
```

### Estado Actual Application Layer

- ✅ **176/176 tests passing** (100% success rate)
- ✅ **90.7% cobertura líneas** (1036/1141 líneas cubiertas)
- ✅ **96.7% branch coverage** (condicionales cubiertos)
- ✅ **Servicios testeados**: WeightLogService, UserService, UserPreferencesService, UserNotificationService
- ✅ **Validators testeados**: FluentValidation (todos los DTOs)
- ✅ **Mappers testeados**: Entity ↔ DTO conversions

### Próximos Pasos

**Infrastructure Layer** (🔴 Pendiente):
- [ ] Tests unitarios para `ControlPesoDbContext`
- [ ] Tests de integración con SQL Server (in-memory o Testcontainers)
- [ ] Tests de repositories (si se implementan)
- [ ] Tests de servicios externos (OAuth, email, etc.)

**End-to-End (E2E)** (🔴 Futuro):
- [ ] Playwright para tests UI
- [ ] Selenium para tests cross-browser
- [ ] Tests de flujos completos (registro, login, CRUD peso)

---

## 🐳 Docker & Deployment

### Infraestructura Producción

- **Servidor**: Proxmox VM — Ubuntu 22.04 LTS (IP: 10.0.0.100)
- **Reverse Proxy**: NPMplus (Nginx) + SSL (Certbot Let's Encrypt)
- **URL Pública**: [https://controlpeso.thiscloud.com.ar](https://controlpeso.thiscloud.com.ar)
- **Puerto Container**: 8080 → NPMplus (HTTPS 443)
- **Path Deployment**: `/opt/controlpeso/`

### Build & Deploy Workflow

**Filosofía**: "Test First, Commit After" — NO commitear hasta confirmar que funciona en producción.

```bash
# 1. Build local (.NET 10)
dotnet build src/ControlPeso.Web/ControlPeso.Web.csproj -c Release

# 2. Docker build (multi-stage)
docker build -t controlpeso-web:latest -f Dockerfile .

# 3. Save image (para transferir)
docker save controlpeso-web:latest -o controlpeso-web_latest.tar

# 4. Transfer a servidor
scp controlpeso-web_latest.tar root@10.0.0.100:/tmp/

# 5. Deploy en servidor (via SSH/MCP)
ssh root@10.0.0.100
cd /opt/controlpeso
docker load -i /tmp/controlpeso-web_latest.tar
docker compose down
docker compose up -d

# 6. Verificar (WAIT 20s startup)
docker compose ps              # Estado: HEALTHY
docker logs controlpeso-web --tail=50
curl https://controlpeso.thiscloud.com.ar/health  # 200 OK

# 7. SOLO SI OK → Git commit + push
git add .
git commit -m "feat: implement new feature"
git push origin feature/branch-name
```

### docker-compose.yml (Producción)

```yaml
version: '3.8'

services:
  web:
    image: controlpeso-web:latest
    container_name: controlpeso-web
    restart: unless-stopped
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__ControlPesoDb=${DB_CONNECTION_STRING}
      - Google__ClientId=${GOOGLE_CLIENT_ID}
      - Google__ClientSecret=${GOOGLE_CLIENT_SECRET}
    volumes:
      - ./logs:/app/logs
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    networks:
      - controlpeso-net

networks:
  controlpeso-net:
    driver: bridge
```

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

# Health check manual
docker inspect controlpeso-web | grep -A 10 Health
curl https://controlpeso.thiscloud.com.ar/health
```

### Checklist Pre-Deploy

- [ ] `dotnet build` exitoso (sin warnings)
- [ ] Secrets verificados (`appsettings.*.json` en .gitignore)
- [ ] Dockerfile actualizado (si cambió estructura proyecto)
- [ ] `.gitignore` incluye `*.tar` (no commitear images)
- [ ] Tests pasan (`dotnet test`)

### Checklist Post-Deploy

- [ ] Containers `HEALTHY` (`docker compose ps`)
- [ ] `/health` endpoint retorna 200 OK
- [ ] Logs sin errores críticos (`docker logs`)
- [ ] Endpoints funcionando (robots.txt, sitemap.xml, login)
- [ ] SSL válido (certificado no expirado)
- [ ] Google OAuth funcionando (login test)

---

## 🤝 Contribuir — Git Flow

Control Peso Thiscloud sigue **Git Flow estricto** con Pull Requests obligatorios:

### Ramas Permanentes

- **`main`** — Producción estable (⚠️ PROTEGIDA — Solo PRs desde `develop`)
- **`develop`** — Integración continua (⚠️ PROTEGIDA — Solo PRs desde `feature/*`)

### Flujo de Trabajo

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

# 3. Push y crear PR
git add .
git commit -m "feat(scope): descripción"
git push origin feature/nombre-descriptivo

# → Crear PR en GitHub: feature/* → develop

# 4. Merge y limpieza (post-PR merge)
git checkout develop
git pull origin develop
git branch -d feature/nombre-descriptivo           # Local
git push origin --delete feature/nombre-descriptivo  # Remoto
```

### Formato Commits (Conventional Commits)

```
<tipo>(scope): <descripción corta>

[cuerpo opcional]

[footer opcional]
```

**Tipos**:
- `feat` — Nueva funcionalidad
- `fix` — Bug fix
- `docs` — Solo documentación
- `style` — Formateo (no cambia lógica)
- `refactor` — Refactorización (no cambia comportamiento)
- `test` — Agregar/modificar tests
- `chore` — Mantenimiento (deps, build, CI)
- `ci` — CI/CD pipelines

**Ejemplos**:
```bash
✅ feat(seo): implement robots.txt with 50+ AI crawlers
✅ fix(auth): resolve Google OAuth redirect loop on production
✅ docs(readme): update deployment section with Docker instructions
❌ "update files" (demasiado genérico)
❌ "WIP" (commits incompletos)
```

### Reglas Inquebrantables

1. ❌ **NUNCA** commits directos a `main` o `develop`
2. ✅ **TODO** cambio pasa por PR con CI verde
3. ✅ **Feature branches** se eliminan post-merge (local + remoto)
4. ✅ **Test First, Commit After** — NO commitear hasta confirmar que funciona
5. ✅ **Build + Deploy + Verify** antes de commit (filosofía deployment)

---

## 📄 Licencia

Este proyecto está bajo licencia **MIT**. Ver [LICENSE](LICENSE) para más detalles.

---

## 📧 Contacto

**Thiscloud Services**  
📧 Email: [contacto@thiscloud.com.ar](mailto:contacto@thiscloud.com.ar)  
🌐 Web: [https://thiscloud.com.ar](https://thiscloud.com.ar)  
💻 GitHub: [@mdesantis1984](https://github.com/mdesantis1984)  
🚀 Proyecto: [Control Peso Thiscloud](https://github.com/mdesantis1984/Control-Peso-Thiscloud)  
🌍 App: [https://controlpeso.thiscloud.com.ar](https://controlpeso.thiscloud.com.ar)

---

**Hecho con ❤️ usando .NET 10, Blazor Server, MudBlazor y Clean Architecture**

© 2025 Thiscloud Services. All rights reserved.
