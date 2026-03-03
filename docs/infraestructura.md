# Infraestructura - Control Peso Thiscloud

> **Documento Maestro de Infraestructura**  
> **Última actualización**: 2026-03-03  
> **Versión**: 1.0.0  
> **Estado**: ✅ Producción

---

## Tabla de Contenido

1. [Arquitectura del Sistema](#1-arquitectura-del-sistema)
2. [Deployment & Containerización](#2-deployment--containerización)
3. [Base de Datos](#3-base-de-datos)
4. [SEO & Crawlers](#4-seo--crawlers)
5. [Seguridad](#5-seguridad)
6. [Analytics](#6-analytics)
7. [Scripts de Automatización](#7-scripts-de-automatización)

---

## 1. Arquitectura del Sistema

### 1.1. Arquitectura Onion (Clean Architecture)

Control Peso Thiscloud sigue **Onion Architecture (Clean Architecture)** con separación estricta de capas y dependencias unidireccionales hacia el centro.

#### Diagrama de Capas

```
┌──────────────────────────────────────────┐
│           Presentation Layer             │
│         (Blazor Server - Web)            │
│   Pages, Components, Middleware          │
└────────────────┬─────────────────────────┘
                 │ Depends on Application
                 ↓
┌──────────────────────────────────────────┐
│          Application Layer               │
│     Services, DTOs, Interfaces           │
│    Validators, Filters, Mappers          │
└────────────────┬─────────────────────────┘
                 │ Depends on Domain
                 ↓
┌──────────────────────────────────────────┐
│       Infrastructure Layer               │
│   DbContext, Repositories, Auth          │
│   External Services, File I/O            │
└────────────────┬─────────────────────────┘
                 │ Depends on Application + Domain
                 ↓
┌──────────────────────────────────────────┐
│            Domain Layer                  │
│   Entities, Enums, Exceptions            │
│        (Zero Dependencies)               │
└──────────────────────────────────────────┘
```

#### Responsabilidades por Capa

| Capa | Responsabilidad | Dependencias | Ejemplos |
|------|-----------------|--------------|----------|
| **Domain** | Entidades de negocio, enums, excepciones core | **Ninguna** (cero dependencias externas) | `User`, `WeightLog`, `UserRole`, `WeightUnit` |
| **Application** | Lógica de negocio, interfaces de servicios, DTOs | Solo Domain | `WeightLogService`, `UserService`, `CreateWeightLogDto` |
| **Infrastructure** | Acceso a datos, servicios externos, I/O | Domain + Application | `ControlPesoDbContext`, `UserRepository`, OAuth |
| **Web (Presentation)** | UI, routing, interacción usuario, middleware | Application + Infrastructure (solo DI) | `Dashboard.razor`, `AddWeightDialog`, Middlewares |

#### Principios SOLID Aplicados

- **Single Responsibility**: Cada servicio tiene una única responsabilidad
- **Open/Closed**: Extensible via interfaces, cerrado a modificación
- **Liskov Substitution**: DTOs y abstracciones substituibles
- **Interface Segregation**: Interfaces específicas por funcionalidad
- **Dependency Inversion**: Capas internas NO conocen capas externas

---

### 1.2. Stack Tecnológico

#### Core Framework

- **.NET 10** (`net10.0`): Target framework
- **C# 14**: Language version (inferred)
- **Blazor Server**: Interactive server-side rendering
- **ASP.NET Core**: Web host, middleware pipeline

#### UI Framework

- **MudBlazor 8.0.0**: Material Design component library (**framework UI exclusivo**)
- **Google Fonts (Roboto)**: Typography

#### Data Access

- **Entity Framework Core 9.0.1**: ORM (**Database First** mode)
- **SQL Server Express (Linux)**: Base de datos en todos los entornos (dev + production)
- **Microsoft.EntityFrameworkCore.SqlServer**: SQL Server provider

#### Authentication

- **ASP.NET Core Identity**: Cookie-based authentication
- **Google OAuth 2.0**: Google sign-in (único método de auth en v1.0)

#### Validation

- **FluentValidation 11.11.0**: DTO validation

#### Logging

- **ThisCloud.Framework.Loggings.Serilog 1.0.86**: Structured logging
  - Serilog sinks: Console + File (NDJSON, rolling)
  - Automatic PII redaction
  - Correlation ID tracking

#### CSV Export

- **CsvHelper 33.0.1**: CSV generation para Admin panel

#### Analytics

- **Google Analytics 4**: Traffic analytics (gtag.js)
- **Cloudflare Analytics**: Privacy-first analytics (opcional)

---

### 1.3. Estructura de Proyecto

```
ControlPeso.Thiscloud.sln
│
├── src/
│   ├── ControlPeso.Domain/                  ← Core business entities
│   │   ├── Entities/                        ← Scaffolded from database
│   │   │   ├── User.cs
│   │   │   ├── WeightLog.cs
│   │   │   ├── UserPreference.cs
│   │   │   └── AuditLog.cs
│   │   ├── Enums/                           ← Manual (map to SQL INTEGERs)
│   │   │   ├── UserRole.cs                  (0=User, 1=Administrator)
│   │   │   ├── UserStatus.cs                (0=Active, 1=Inactive, 2=Pending)
│   │   │   ├── UnitSystem.cs                (0=Metric, 1=Imperial)
│   │   │   ├── WeightUnit.cs                (0=Kg, 1=Lb)
│   │   │   └── WeightTrend.cs               (0=Up, 1=Down, 2=Neutral)
│   │   └── Exceptions/                      ← Domain exceptions
│   │       ├── DomainException.cs
│   │       ├── NotFoundException.cs
│   │       └── ValidationException.cs
│   │
│   ├── ControlPeso.Application/             ← Business logic
│   │   ├── DTOs/                            ← Data Transfer Objects
│   │   ├── Interfaces/                      ← Service contracts
│   │   ├── Services/                        ← Service implementations
│   │   ├── Validators/                      ← FluentValidation
│   │   ├── Filters/                         ← Query filters
│   │   ├── Mapping/                         ← Entity ↔ DTO mappers
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs
│   │
│   ├── ControlPeso.Infrastructure/          ← Data access & external
│   │   ├── Data/
│   │   │   ├── ControlPesoDbContext.cs      ← EF Core DbContext (scaffolded)
│   │   │   ├── IDbSeeder.cs
│   │   │   └── DbSeeder.cs
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs
│   │
│   ├── ControlPeso.Shared.Resources/        ← i18n Resources (Fase 10)
│   │   └── *.resx                           ← Localization files
│   │
│   └── ControlPeso.Web/                     ← Blazor Server presentation
│       ├── Components/
│       │   ├── Layout/
│       │   │   ├── MainLayout.razor         ← Layout principal
│       │   │   ├── MainLayout.razor.cs      ← Code-behind OBLIGATORIO
│       │   │   ├── NavMenu.razor
│       │   │   └── NavMenu.razor.cs
│       │   ├── Pages/                       ← Public pages
│       │   │   ├── Login.razor
│       │   │   ├── Home.razor
│       │   │   ├── Dashboard.razor
│       │   │   ├── Profile.razor
│       │   │   ├── History.razor
│       │   │   ├── Trends.razor
│       │   │   └── Admin/ (Dashboard, Users)
│       │   └── Shared/                      ← Reusable components
│       │       ├── AddWeightDialog.razor
│       │       ├── WeightChart.razor
│       │       ├── LanguageSelector.razor
│       │       └── NotificationBell.razor
│       ├── Extensions/
│       │   └── SeoEndpointsExtensions.cs    ← Minimal APIs SEO
│       ├── Services/
│       │   ├── SitemapService.cs            ← SEO sitemap/robots generation
│       │   └── Storage/                     ← ProtectedBrowserStorage helpers
│       ├── wwwroot/
│       │   ├── css/
│       │   ├── images/
│       │   └── js/
│       ├── Program.cs
│       └── appsettings.json
│
├── tests/
│   ├── ControlPeso.Domain.Tests/
│   ├── ControlPeso.Application.Tests/
│   ├── ControlPeso.Infrastructure.Tests/
│   └── ControlPeso.Web.Tests/
│
├── docs/                                    ← Documentation
│   ├── infraestructura.md                   ← Este documento
│   ├── documentacionfuncional.md
│   ├── soporte.md
│   ├── Plan_*.md                            ← Planes históricos (preservados)
│   ├── legal/                               ← Legal docs (preservados)
│   └── schema/
│       └── schema_v1.sql                    ← Database schema master
│
├── scripts/                                 ← Automation scripts
│   ├── Deploy-Docker-Local.ps1
│   ├── deploy-docker.ps1
│   └── migrate-to-sqlserver.ps1
│
├── .github/
│   ├── copilot-instructions.md              ← GitHub Copilot rules
│   └── workflows/
│       └── ci.yml                           ← CI/CD (planned)
│
├── Dockerfile
├── docker-compose.yml
├── docker-compose.override.yml.example
├── Directory.Packages.props                 ← Central Package Management
├── clearObjBinVs.ps1
└── setup-config.ps1
```

---

### 1.4. Arquitectura de Avatares

**Problema resuelto**: Persistencia de avatares en Blazor Server con seguridad y performance óptima.

#### Flujo Upload & Display

```
Usuario selecciona imagen
         ↓
InputFile component (web)
         ↓
Convertir a Base64 string
         ↓
ProtectedBrowserStorage.SetAsync("UserAvatar", base64)
         ↓
[Encrypted storage en navegador]
         ↓
Componente lee: ProtectedBrowserStorage.GetAsync<string>("UserAvatar")
         ↓
<img src="data:image/png;base64,{base64}" />
```

#### Modelo de Seguridad

- ✅ **Encrypted storage**: `ProtectedBrowserStorage` usa ASP.NET Core Data Protection
- ✅ **Per-circuit isolation**: Cada usuario tiene su storage aislado
- ✅ **No file I/O**: Sin escritura a disco → más seguro
- ✅ **Session-based**: Se pierde al cerrar browser (by design)

#### Limitaciones

- ⚠️ **Storage size**: Max ~5MB por avatar (límite del navegador)
- ⚠️ **Session lifecycle**: No persiste entre sesiones diferentes
- ⚠️ **Circuit cleanup**: Se limpia automáticamente al cerrar conexión

---

## 2. Deployment & Containerización

### 2.1. Overview de Deployment

Control Peso Thiscloud soporta múltiples targets de deployment:

| Target | Status | Use Case |
|--------|--------|----------|
| **Docker (Local)** | ✅ READY | Desarrollo local |
| **Docker (Production)** | ✅ READY | Servidor Proxmox VM (10.0.0.100) |
| **Azure App Service** | 🔵 Planned | Escalado cloud (futuro) |
| **IIS** | 🔵 Planned | On-premises Windows Server |

---

### 2.2. Docker Local Development

#### Quick Start

```bash
# 1. Clone repository
git clone https://github.com/mdesantis1984/Control-Peso-Thiscloud.git
cd Control-Peso-Thiscloud

# 2. Create docker-compose.override.yml (GITIGNORED)
cp docker-compose.override.yml.example docker-compose.override.yml

# 3. Edit docker-compose.override.yml with REAL credentials
nano docker-compose.override.yml

# 4. Start container
docker-compose up -d --build

# 5. Verify health
curl http://localhost:8080/health

# 6. Open app
http://localhost:8080
```

#### Docker Compose Files

**docker-compose.yml** (base - commiteado):

```yaml
version: '3.8'
services:
  controlpeso-web:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    volumes:
      - ./data:/app/data
    restart: unless-stopped
```

**docker-compose.override.yml** (secretos - NO commiteado):

```yaml
version: '3.8'
services:
  controlpeso-web:
    environment:
      # ✅ REQUIRED: Google OAuth 2.0
      - Authentication__Google__ClientId=180510012560-EXAMPLE.apps.googleusercontent.com
      - Authentication__Google__ClientSecret=GOCSPX-EXAMPLE

      # 📊 OPTIONAL: Google Analytics 4
      - GoogleAnalytics__MeasurementId=G-XXXXXXXXXX
```

**Security Notes**:
- ✅ `docker-compose.override.yml` está en `.gitignore`
- ✅ `docker-compose.override.yml.example` tiene placeholders (safe to commit)
- ❌ **NUNCA** commitear credentials reales

#### Verificar Deployment

```bash
# Container status
docker ps

# Logs en tiempo real
docker logs -f controlpeso-web

# Verificar OAuth config dentro del container
docker exec controlpeso-web printenv | grep Authentication__Google

# Test health endpoint
curl http://localhost:8080/health
# Expected: {"status":"healthy","timestamp":"2026-03-03T...","version":"1.0.0"}

# Test OAuth login
# 1. http://localhost:8080/login
# 2. Click "Continuar con Google"
# 3. Authenticate
# 4. Redirect a Dashboard
```

---

### 2.3. Docker Production Deployment

**Target**: Proxmox VM (10.0.0.100) con reverse proxy NPMplus.

#### Workflow de Deployment

```
Build local → Transfer → Deploy remoto → Verify → Commit
```

**Paso 1: Build Local**

```powershell
# Build .NET project
dotnet build src/ControlPeso.Web/ControlPeso.Web.csproj -c Release

# Build Docker image
docker build -t controlpeso-web:latest -f Dockerfile .

# Save image a tar
docker save controlpeso-web:latest -o controlpeso-web_latest.tar
```

**Paso 2: Transfer to Server**

```powershell
# SCP transfer
scp controlpeso-web_latest.tar root@10.0.0.100:/tmp/
```

**Paso 3: Deploy Remoto (MCP SSH)**

```bash
# Load image
cd /opt/controlpeso
docker load -i /tmp/controlpeso-web_latest.tar

# Stop old container
docker compose down

# Start new container
docker compose up -d

# Wait 20s para startup
sleep 20
```

**Paso 4: Verificar**

```bash
# Container health
docker compose ps
# Expected: STATUS = healthy (not "starting")

# Logs (últimas 50 líneas)
docker logs controlpeso-web --tail=50

# Test health endpoint
curl https://controlpeso.thiscloud.com.ar/health

# Test SEO endpoints
curl https://controlpeso.thiscloud.com.ar/robots.txt
curl https://controlpeso.thiscloud.com.ar/sitemap.xml
```

**Paso 5: Commit (SOLO si TODO OK)**

```bash
git add .
git commit -m "feat(deploy): descripción del cambio"
git push origin feature/nombre-rama
```

**🚨 REGLA CRÍTICA**: **Test First, Commit After**  
NUNCA commitear código sin verificar deployment exitoso en producción.

---

### 2.4. Configuración de Puertos

#### Mapping de Puertos

| Service | Internal Port | External Port | Protocol |
|---------|---------------|---------------|----------|
| **ControlPeso.Web** | 8080 | 8080 (local) / 443 (prod) | HTTP/HTTPS |
| **NPMplus Reverse Proxy** | N/A | 443 | HTTPS |

#### NPMplus Reverse Proxy

**Frontend**: `controlpeso.thiscloud.com.ar`  
**Backend**: `http://10.0.0.100:8080`

**SSL**: Certbot (Let's Encrypt) automático  
**Headers**: `X-Forwarded-For`, `X-Real-IP`, `X-Forwarded-Proto`

#### Dockerfile EXPOSE

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ControlPeso.Web.dll"]
```

#### appsettings.Production.json

```json
{
  "App": {
    "BaseUrl": "https://controlpeso.thiscloud.com.ar"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://+:8080"
      }
    }
  }
}
```

---

## 3. Base de Datos

### 3.1. Entity Framework Core Database First

Control Peso Thiscloud usa **Database First** como filosofía arquitectural.

#### Filosofía: SQL como Contrato Maestro

> **El esquema SQL es la fuente de verdad absoluta. Todo el gobierno de datos vive en SQL.**

- ✅ Tipos de datos, precisión, longitudes → DDL
- ✅ Restricciones NOT NULL, UNIQUE, CHECK → DDL
- ✅ Defaults → DDL
- ✅ Foreign Keys, ON DELETE → DDL
- ✅ Índices → DDL

#### Flujo de Trabajo Database First

```
1. Modificar docs/schema/schema_v1.sql
     ↓
2. Aplicar SQL:
   sqlite3 controlpeso.db < docs/schema/schema_v1.sql
     ↓
3. Scaffold EF Core:
   dotnet ef dbcontext scaffold "Data Source=../../controlpeso.db" \
     Microsoft.EntityFrameworkCore.Sqlite \
     --context ControlPesoDbContext \
     --output-dir ../ControlPeso.Domain/Entities \
     --context-dir . \
     --force
     ↓
4. DbContext queda en Infrastructure
   Entidades quedan en Domain/Entities
     ↓
5. Ajustes post-scaffold (solo value converters si necesario)
   - Guid (string → Guid)
   - DateTime (string → DateTime)
   - Enums (int → enum)
     ↓
6. ❌ NO tocar entidades manualmente
```

#### Comando de Scaffold

```bash
# Desde src/ControlPeso.Infrastructure
dotnet ef dbcontext scaffold \
  "Server=localhost;Database=ControlPeso;User=sa;Password=<your_password>;TrustServerCertificate=true" \
  Microsoft.EntityFrameworkCore.SqlServer \
  --context ControlPesoDbContext \
  --output-dir ../ControlPeso.Domain/Entities \
  --context-dir . \
  --project . \
  --force \
  --no-pluralize
```

#### Entidades Generadas

- `User.cs`: Usuario con Google OAuth, altura, sistema de unidades, idioma
- `WeightLog.cs`: Registro de peso con fecha, hora, nota, tendencia
- `UserPreference.cs`: Preferencias de usuario (dark mode, notificaciones, timezone)
- `AuditLog.cs`: Auditoría de cambios (para compliance/debugging)

#### Conversiones en Application Layer

```csharp
// Mapper: Entity (string/int) → DTO (Guid/DateTime/enum)
public WeightLogDto MapToDto(WeightLog entity) => new()
{
    Id = Guid.Parse(entity.Id),                    // string → Guid
    UserId = Guid.Parse(entity.UserId),
    Date = DateOnly.Parse(entity.Date),            // string → DateOnly
    Time = TimeOnly.Parse(entity.Time),            // string → TimeOnly
    Weight = entity.Weight,                         // double (kg siempre)
    DisplayUnit = (WeightUnit)entity.DisplayUnit,  // int → enum
    Trend = (WeightTrend)entity.Trend,
    CreatedAt = DateTime.Parse(entity.CreatedAt)   // string → DateTime
};
```

---

### 3.2. Sistema de Backups Automáticos

**Objetivo**: Protección contra pérdida de datos con backups automáticos diarios.

#### Configuración

**Schedule**: Daily @ 02:00 AM (UTC-3)  
**Retention**: 7 días (rotating)  
**Location**: `/opt/controlpeso/backups/`  
**Format**: `controlpeso_backup_YYYYMMDD_HHMMSS.db`

#### Backup Script (cron)

```bash
#!/bin/bash
# /opt/controlpeso/scripts/backup-db.sh

DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/opt/controlpeso/backups"
SOURCE_DB="/opt/controlpeso/data/controlpeso.db"
BACKUP_FILE="$BACKUP_DIR/controlpeso_backup_$DATE.db"

# Create backup
cp "$SOURCE_DB" "$BACKUP_FILE"

# Compress
gzip "$BACKUP_FILE"

# Delete backups older than 7 days
find "$BACKUP_DIR" -name "controlpeso_backup_*.db.gz" -mtime +7 -delete

echo "Backup completed: $BACKUP_FILE.gz"
```

#### Restore Procedure

```bash
# Stop container
docker compose down

# Restore from backup
cd /opt/controlpeso/data
gunzip -c ../backups/controlpeso_backup_20260303_020000.db.gz > controlpeso.db

# Start container
docker compose up -d
```

#### Retention Policy

| Period | Frequency | Retention |
|--------|-----------|-----------|
| Daily | 02:00 AM | 7 días |
| Weekly | Domingos | 4 semanas (planned) |
| Monthly | Día 1 | 12 meses (planned) |

---

## 4. SEO & Crawlers

### 4.1. Infraestructura SEO

Control Peso Thiscloud implementa una **infraestructura SEO enterprise-grade** con:

- ✅ **Sitemap.xml dinámico** generado en tiempo real
- ✅ **Robots.txt dinámico** con soporte completo para 50+ AI crawlers
- ✅ **Response caching** optimizado (1h sitemap, 24h robots)
- ✅ **Minimal APIs** para endpoints root-level (`/robots.txt`, `/sitemap.xml`)
- ✅ **Structured data** con prioridades y frecuencias de cambio

#### Arquitectura SEO

```
┌─────────────────────────────────────────────────────┐
│                   SEO Layer                         │
├─────────────────────────────────────────────────────┤
│                                                     │
│  ┌───────────────────┐     ┌────────────────────┐ │
│  │ SeoEndpoints      │────▶│ SitemapService     │ │
│  │ (Minimal APIs)    │     │                    │ │
│  │                   │     │ - GenerateSitemap()│ │
│  │ /sitemap.xml      │     │ - GenerateRobots() │ │
│  │ /robots.txt       │     └────────────────────┘ │
│  └───────────────────┘              │             │
│         │                            │             │
│         ▼                            ▼             │
│  [Response Cache]           [Configuration]       │
│  - 1h (sitemap)             - App:BaseUrl         │
│  - 24h (robots)             - Dynamic rules       │
│                                                     │
└─────────────────────────────────────────────────────┘
```

---

### 4.2. Minimal APIs SEO (Solución Blazor Routing)

**Problema**: En Blazor Server, `MapRazorComponents()` captura rutas root-level ANTES que `MapControllers()`, haciendo que Controllers NO funcionen para `/robots.txt` o `/sitemap.xml`.

**Solución**: Usar **Minimal APIs** registradas ANTES de `MapRazorComponents()`.

#### SeoEndpointsExtensions.cs

```csharp
// src/ControlPeso.Web/Extensions/SeoEndpointsExtensions.cs
public static class SeoEndpointsExtensions
{
    public static IEndpointRouteBuilder MapSeoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/robots.txt", async (SitemapService service, HttpContext context) => 
        {
            var content = service.GenerateRobotsTxt();
            context.Response.Headers.CacheControl = "public, max-age=86400"; // 24 hours
            return Results.Text(content, "text/plain; charset=utf-8");
        })
        .WithName("GetRobotsTxt")
        .WithTags("SEO");

        endpoints.MapGet("/sitemap.xml", async (SitemapService service, HttpContext context) => 
        {
            var content = service.GenerateSitemapXml();
            context.Response.Headers.CacheControl = "public, max-age=3600"; // 1 hour
            return Results.Text(content, "application/xml; charset=utf-8");
        })
        .WithName("GetSitemapXml")
        .WithTags("SEO");

        return endpoints;
    }
}
```

#### Program.cs (Orden CRÍTICO)

```csharp
// ORDEN MANDATORIO:
app.MapSeoEndpoints();         // 1. Minimal APIs SEO (PRIMERO)
app.MapHealthChecks("/health");   // 2. Health checks
app.MapStaticAssets();            // 3. Static files
app.MapRazorComponents<App>();    // 4. Blazor (ÚLTIMO)
```

**Regla**: Registrar Minimal APIs ANTES de `MapRazorComponents()` para que tengan precedencia.

---

### 4.3. robots.txt Generation (50+ AI Crawlers)

#### Cobertura COMPLETA: 50+ AI Crawlers en 8 Tiers

**TIER 1: Major LLM Providers** (4 providers, 10+ user-agents)

| Bot | User-Agents | Purpose | Crawl Delay |
|-----|-------------|---------|-------------|
| **OpenAI GPT** | `GPTBot`, `ChatGPT-User`, `OAI-SearchBot` | ChatGPT, GPT-4, o1, o3 training | 2s |
| **Anthropic** | `Claude-Web`, `ClaudeBot`, `anthropic-ai` | Claude 3/3.5/4, Sonnet, Opus | 2s |
| **Google Gemini** | `Google-Extended`, `Googlebot-Image`, `Googlebot-Video` | Gemini 1.5/2.0, Bard, PaLM 2 | 2s |
| **Microsoft** | `Bingbot`, `BingPreview`, `msnbot`, `MSNBot-Media` | Copilot, Bing Chat, GitHub Copilot | 2s |

**TIER 2: Big Tech AI** (4 providers, 9+ user-agents)

| Bot | User-Agents | Purpose | Crawl Delay |
|-----|-------------|---------|-------------|
| **Meta AI** | `FacebookBot`, `Meta-ExternalAgent`, `facebookexternalhit` | LLaMA 2/3/3.1, Meta AI Assistant | 3s |
| **Amazon** | `ia_archiver`, `Amazonbot`, `alexa site audit` | Alexa, Amazon Q, Bedrock | 3s |
| **Apple** | `Applebot`, `Applebot-Extended`, `AppleNewsBot` | Apple Intelligence, Siri, Spotlight | 2s |
| **X/Twitter** | `TwitterBot`, `Twitterbot` | Grok AI by xAI | 3s |

**TIER 3: Specialized AI Search** (4 providers)

- **Perplexity AI**: `PerplexityBot`
- **You.com**: `YouBot`
- **Yandex**: `YandexBot`, `YandexImages`, `YandexAccessibilityBot`
- **Baidu**: `Baiduspider`, `Baiduspider-image`, `Baiduspider-video`

**TIER 4: Emerging AI Companies** (6 providers)

- **Cohere**: `cohere-ai`
- **Diffbot**: `Diffbot`
- **Webz.io**: `omgili`, `omgilibot`
- **DataForSEO**: `DataForSeoBot`
- **BrightData**: `ICC-Crawler`
- **AI2 (Semantic Scholar)**: `Ai2Bot`, `AI2Bot-Dolma`

**TIER 5: Content AI** (4 providers)

- **Common Crawl**: `CCBot`
- **Internet Archive**: `ia_archiver`, `archive.org_bot`
- **Scrapy**: `Scrapy` (framework usado por muchos)
- **Apify**: `ApifyBot`

**TIER 6: Academic/Research AI** (3 providers)

- **SemrushBot**: `SemrushBot`
- **AhrefsBot**: `AhrefsBot`
- **DotBot**: `DotBot` (Moz/OpenSiteExplorer)

**TIER 7: News & Media Aggregators** (2 providers)

- **Google News**: `Googlebot-News`
- **Apple News**: `AppleNewsBot`

**TIER 8: Blocked Bots** (Crawlers agresivos/no deseados)

```
User-agent: SemrushBot
Disallow: /

User-agent: AhrefsBot
Disallow: /

User-agent: DotBot
Disallow: /
```

#### robots.txt Generado (Ejemplo)

```
# Control Peso Thiscloud - robots.txt
# Generated: 2026-03-03T19:35:00Z
# AI Crawlers: 50+ supported

# === TIER 1: Major LLM Providers ===

User-agent: GPTBot
User-agent: ChatGPT-User
User-agent: OAI-SearchBot
Crawl-delay: 2
Allow: /
Disallow: /admin/
Disallow: /dashboard/
Disallow: /profile/
Disallow: /history/
Disallow: /trends/

User-agent: Claude-Web
User-agent: ClaudeBot
User-agent: anthropic-ai
Crawl-delay: 2
Allow: /
Disallow: /admin/
Disallow: /dashboard/
Disallow: /profile/
Disallow: /history/
Disallow: /trends/

# ... (50+ crawlers más)

# === BLOCKED BOTS ===

User-agent: SemrushBot
Disallow: /

User-agent: AhrefsBot
Disallow: /

# === SITEMAP ===

Sitemap: https://controlpeso.thiscloud.com.ar/sitemap.xml
```

---

### 4.4. sitemap.xml Generation

#### URLs Públicas Incluidas

| URL | Priority | Change Freq | Notes |
|-----|----------|-------------|-------|
| `/` | 1.0 | daily | Home page - máxima prioridad |
| `/login` | 0.9 | weekly | High conversion page |
| `/privacy` | 0.7 | monthly | Legal (trust/compliance) |
| `/privacidad` | 0.7 | monthly | Spanish version |
| `/terms` | 0.7 | monthly | Legal (trust/compliance) |
| `/terminos` | 0.7 | monthly | Spanish version |
| `/changelog` | 0.6 | weekly | Product updates |
| `/historial` | 0.6 | weekly | Spanish version |

#### URLs Protegidas (Excluidas)

- `/dashboard` - Requires authentication
- `/profile` - Requires authentication
- `/history` - Requires authentication
- `/trends` - Requires authentication
- `/admin` - Requires Administrator role

#### sitemap.xml Generado (Ejemplo)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
  <url>
    <loc>https://controlpeso.thiscloud.com.ar/</loc>
    <lastmod>2026-03-03</lastmod>
    <changefreq>daily</changefreq>
    <priority>1.0</priority>
  </url>
  <url>
    <loc>https://controlpeso.thiscloud.com.ar/login</loc>
    <lastmod>2026-03-03</lastmod>
    <changefreq>weekly</changefreq>
    <priority>0.9</priority>
  </url>
  <!-- ... más URLs -->
</urlset>
```

---

## 5. Seguridad

### 5.1. Overview de Seguridad

Control Peso Thiscloud implementa múltiples capas de seguridad:

- ✅ **HTTPS obligatorio** (Certbot SSL + NPMplus reverse proxy)
- ✅ **CSP headers** restrictivos (Content Security Policy)
- ✅ **Cookie segura** (`HttpOnly`, `Secure`, `SameSite=Strict`)
- ✅ **Google OAuth 2.0** (único método de auth, sin passwords propias)
- ✅ **Antiforgery tokens** (CSRF protection)
- ✅ **User Secrets** (dev) + **Environment Variables** (prod)
- ✅ **PII redaction** automática en logs (ThisCloud.Framework.Loggings)
- ✅ **Rate limiting** (planned - Fase 11)

---

### 5.2. Google OAuth 2.0 Setup

#### Paso 1: Google Cloud Console

1. Ir a [Google Cloud Console](https://console.cloud.google.com/)
2. Crear nuevo proyecto: **"Control Peso Thiscloud"**
3. Activar **Google+ API** y **People API**
4. Ir a **APIs & Services > Credentials**
5. Crear **OAuth 2.0 Client ID**

#### Paso 2: Configurar Redirect URIs

**Development**:
```
http://localhost:8080/signin-google
```

**Production**:
```
https://controlpeso.thiscloud.com.ar/signin-google
```

#### Paso 3: Obtener Credentials

- **Client ID**: `180510012560-EXAMPLE.apps.googleusercontent.com`
- **Client Secret**: `GOCSPX-EXAMPLE`

#### Paso 4: Configurar en la App

**Development** (User Secrets):

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET"
```

**Production** (Environment Variables):

```bash
# En docker-compose.override.yml
environment:
  - Authentication__Google__ClientId=YOUR_CLIENT_ID
  - Authentication__Google__ClientSecret=YOUR_CLIENT_SECRET
```

#### Program.cs Configuration

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;      // ✅ Seguridad
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // ✅ HTTPS only
    options.Cookie.SameSite = SameSiteMode.Strict;  // ✅ CSRF protection
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;
    
    // Scopes necesarios
    options.Scope.Add("profile");
    options.Scope.Add("email");
});
```

---

### 5.3. Gestión de Configuración & Secretos

#### Templates Pattern

**Problema**: `.gitignore` + secretos en `appsettings.json` local → Al cambiar de branch/clonar repo, configs se pierden.

**Solución**: Template Files con placeholders.

```
src/ControlPeso.Web/
├── appsettings.json                      ← Base config (sin secretos)
├── appsettings.Development.json.template ← COMMITED (placeholders)
├── appsettings.Production.json.template  ← COMMITED (placeholders)
├── appsettings.Development.json          ← IGNORED (real secrets)
└── appsettings.Production.json           ← IGNORED (real secrets)
```

#### Setup Script

**setup-config.ps1**:

```powershell
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

#### Template Format

**appsettings.Development.json.template**:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "REPLACE_WITH_GOOGLE_CLIENT_ID",
      "ClientSecret": "REPLACE_WITH_GOOGLE_CLIENT_SECRET"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=controlpeso.db"
  },
  "GoogleAnalytics": {
    "MeasurementId": "REPLACE_WITH_GA4_MEASUREMENT_ID"
  }
}
```

#### User Secrets (Development Alternative)

```bash
# Initialize user secrets
dotnet user-secrets init --project src/ControlPeso.Web

# Set secrets (stored in user profile, NOT in project)
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID" --project src/ControlPeso.Web
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET" --project src/ControlPeso.Web

# List secrets
dotnet user-secrets list --project src/ControlPeso.Web

# Remove secrets
dotnet user-secrets remove "Authentication:Google:ClientId" --project src/ControlPeso.Web
```

#### .gitignore Rules

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

---

### 5.4. Credential Rotation Guide

#### Google OAuth Rotation

**Cuándo rotar**:
- ⚠️ Credential leak (accidental commit, exposición pública)
- 🔄 Rotación programada (cada 90 días - recomendado)
- 🚨 Actividad sospechosa detectada

**Procedimiento**:

1. **Google Cloud Console**:
   - Ir a **APIs & Services > Credentials**
   - Seleccionar el OAuth 2.0 Client ID actual
   - Click **"Reset Secret"** o crear nuevo Client ID
   - Copiar nuevo `Client Secret`

2. **Actualizar en Development**:
   ```bash
   # User Secrets
   dotnet user-secrets set "Authentication:Google:ClientSecret" "NEW_SECRET" --project src/ControlPeso.Web
   
   # O en appsettings.Development.json (si no usas User Secrets)
   nano src/ControlPeso.Web/appsettings.Development.json
   # Actualizar Authentication:Google:ClientSecret
   ```

3. **Actualizar en Production**:
   ```bash
   # Editar docker-compose.override.yml en servidor
   nano /opt/controlpeso/docker-compose.override.yml
   # Actualizar Authentication__Google__ClientSecret
   
   # Restart container
   docker compose down
   docker compose up -d
   ```

4. **Verificar**:
   ```bash
   # Test OAuth login
   curl -I https://controlpeso.thiscloud.com.ar/login
   # Click "Continuar con Google" → debe funcionar
   ```

5. **Revocar Credential Vieja** (si fue leak):
   - En Google Cloud Console: Delete old Client ID
   - Update Redirect URIs si cambió dominio

#### Connection Strings Rotation

**SQL Server Password Change**:

```bash
# 1. Cambiar password en SQL Server
ALTER LOGIN [controlpeso_user] WITH PASSWORD = 'NEW_STRONG_PASSWORD';

# 2. Actualizar connection string (Production)
nano /opt/controlpeso/docker-compose.override.yml
# ConnectionStrings__DefaultConnection=Server=...;Password=NEW_STRONG_PASSWORD;

# 3. Restart container
docker compose down
docker compose up -d

# 4. Verificar conectividad
docker logs controlpeso-web --tail=50 | grep -i "database"
```

---

## 6. Analytics

### 6.1. Cloudflare Analytics Setup

**Objetivo**: Analytics privacy-first complementario a Google Analytics 4.

#### Ventajas de Cloudflare Analytics

- ✅ **Privacy-first**: No requiere consent cookies (GDPR compliant)
- ✅ **Server-side tracking**: No bloqueado por adblockers
- ✅ **Zero JavaScript**: No impacta performance del sitio
- ✅ **Free tier**: Incluido en plan gratuito de Cloudflare

#### Setup Steps

1. **Domain en Cloudflare**:
   - Agregar domain: `thiscloud.com.ar` a Cloudflare
   - Actualizar nameservers en registrar

2. **SSL/TLS Configuration**:
   - Mode: **Full (strict)**
   - Edge Certificates: **Always Use HTTPS** ✅

3. **Analytics Dashboard**:
   - Ir a **Analytics > Web Analytics**
   - Enable analytics para `controlpeso.thiscloud.com.ar`
   - Copy **Beacon Token** (no necesario con proxy)

4. **Metrics Disponibles**:
   - Page views
   - Unique visitors
   - Countries
   - Referrers
   - Popular pages
   - Browser/Device breakdown

#### Google Analytics 4 (Complementario)

**setup-config.ps1 ya configura GA4**:

```json
{
  "GoogleAnalytics": {
    "MeasurementId": "G-XXXXXXXXX"  // Reemplazar con real
  }
}
```

**_Layout.cshtml head**:

```html
<!-- Google Analytics 4 -->
<script async src="https://www.googletagmanager.com/gtag/js?id=@Configuration["GoogleAnalytics:MeasurementId"]"></script>
<script>
  window.dataLayer = window.dataLayer || [];
  function gtag(){dataLayer.push(arguments);}
  gtag('js', new Date());
  gtag('config', '@Configuration["GoogleAnalytics:MeasurementId"]', {
    'anonymize_ip': true  // GDPR compliance
  });
</script>
```

---

## 7. Scripts de Automatización

### 7.1. Scripts Preservados

#### clearObjBinVs.ps1

**Propósito**: Limpieza de directorios temporales de build.

```powershell
# Limpia obj/, bin/, .vs/ en toda la solución
Get-ChildItem -Path . -Include obj,bin,.vs -Recurse -Directory | Remove-Item -Recurse -Force
Write-Host "✅ Cleaned obj/, bin/, .vs/ directories"
```

**Uso**:
```powershell
.\clearObjBinVs.ps1
```

---

#### setup-config.ps1

**Propósito**: Setup de configs desde templates (ver sección 5.3).

**Uso**:
```powershell
.\setup-config.ps1
```

---

#### Deploy-Docker-Local.ps1

**Propósito**: Deploy completo en Docker local con build, start, y verify.

**Uso**:
```powershell
.\scripts\Deploy-Docker-Local.ps1
```

**Features**:
- ✅ Build .NET Release
- ✅ Build Docker image
- ✅ Start container con docker-compose
- ✅ Health check automático
- ✅ Logs display

---

#### deploy-docker.ps1

**Propósito**: Deploy Docker producción con transfer a servidor remoto.

**Uso**:
```powershell
.\scripts\deploy-docker.ps1
```

**Features**:
- ✅ Build image
- ✅ Save to .tar
- ✅ Transfer via SCP
- ✅ Deploy remoto via SSH
- ✅ Health verification

---

#### migrate-to-sqlserver.ps1

**Propósito**: Migración de SQLite (dev) a SQL Server (production).

**Uso**:
```powershell
.\scripts\migrate-to-sqlserver.ps1
```

**Features**:
- ✅ Export schema de SQLite
- ✅ Convert a T-SQL
- ✅ Create SQL Server database
- ✅ Migrate data
- ✅ Update connection strings

---

### 7.2. Comandos Inline Frecuentes

#### Docker

```powershell
# Build image
docker build -t controlpeso-web:latest -f Dockerfile .

# Save image
docker save controlpeso-web:latest -o controlpeso-web_latest.tar

# Load image
docker load -i controlpeso-web_latest.tar

# Start containers
docker compose up -d

# Stop containers
docker compose down

# View logs
docker logs -f controlpeso-web

# Exec into container
docker exec -it controlpeso-web /bin/bash

# Prune old images
docker image prune -a --filter "until=24h"
```

#### Git (Git Flow)

```powershell
# Create feature branch
git checkout develop
git pull origin develop
git checkout -b feature/nombre-descriptivo

# Commit & push
git add .
git commit -m "feat(scope): descripción"
git push origin feature/nombre-descriptivo

# Merge a develop (via PR en GitHub)
# Después del merge:
git checkout develop
git pull origin develop
git branch -d feature/nombre-descriptivo  # Local cleanup
git push origin --delete feature/nombre-descriptivo  # Remote cleanup
```

#### EF Core

```powershell
# Scaffold database (Database First)
dotnet ef dbcontext scaffold "Data Source=../../controlpeso.db" Microsoft.EntityFrameworkCore.Sqlite --context ControlPesoDbContext --output-dir ../ControlPeso.Domain/Entities --context-dir . --force

# List migrations (Database First NO usa migrations)
# Cambios van en SQL y re-scaffold
```

---

## Referencias

- **Plan Maestro**: `Plan_ControlPeso_Thiscloud_v1_0.md` (histórico)
- **Fase 10**: `Plan_Fase_10_Globalizacion.md` (histórico)
- **Fase 9**: `Plan_Fase_9_Pixel_Perfect.md` (histórico)
- **Copilot Instructions**: `.github/copilot-instructions.md`
- **Schema SQL**: `docs/schema/schema_v1.sql`

---

## Changelog

| Fecha | Cambios |
|-------|---------|
| 2026-03-03 | Documento creado - Consolidación de 15 archivos fuente |
| 2026-03-03 | Agregado: SEO Minimal APIs, 50+ AI crawlers, Deployment workflow |

---

**FIN DEL DOCUMENTO** ✅
