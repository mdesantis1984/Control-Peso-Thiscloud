# Control Peso Thiscloud

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)
![MudBlazor](https://img.shields.io/badge/MudBlazor-8.0.0-594AE2)
![License](https://img.shields.io/badge/License-MIT-green)
![Progress](https://img.shields.io/badge/Progress-Backend%20100%25%20%7C%20UI%2050%25-yellow)

Aplicación web minimalista de control de peso corporal construida con **Blazor Server (.NET 10)** y **MudBlazor**.

> **Estado del proyecto**: 🟡 **EN DESARROLLO** — Backend completo, UI/UX en progreso  
> **Backend**: 100% completado (63/63 tareas técnicas)  
> **Frontend**: ~50% completado (funcional pero pendiente pixel perfect)  
> **Última actualización**: 2026-02-18  
> **Release**: [v1.0.0-alpha](https://github.com/mdesantis1984/Control-Peso-Thiscloud/releases/tag/v1.0.0) (backend)

## ✨ Características

### ✅ Implementadas (Backend completo)

- ✅ Autenticación con **Google OAuth 2.0** + **LinkedIn OAuth**
- ✅ Dashboard con métricas actuales y gráficos de evolución
- ✅ Registro de peso con fecha, hora, notas y tendencia automática
- ✅ Historial con búsqueda, filtros y paginación
- ✅ Análisis de tendencias con proyecciones
- ✅ Panel de administración (gestión de usuarios y roles)
- ✅ Soporte bilingüe: **Español** / **English** (infraestructura)
- ✅ Soporte de unidades: **Métrico (kg, cm)** / **Imperial (lb, ft/in)**
- ✅ Tema oscuro base

### ⏳ Pendientes (Frontend - Pixel Perfect)

- 🔴 **Refinar diseño visual** → Ajustar a prototipo Google AI Studio
- 🔴 **Optimizar responsive design** → Mobile/tablet perfecto
- 🔴 **Pulir espaciados y alineaciones** → Consistencia visual
- 🔴 **Mejorar transiciones** → Animaciones suaves MudBlazor
- 🔴 **Iconografía consistente** → Revisar todos los iconos
- 🔴 **Testing UX exhaustivo** → Usuarios reales + feedback
- 🔴 **A11y testing manual** → Keyboard nav + screen readers
- 🔴 **Performance optimization** → Lighthouse 90+ score

## 🏗️ Arquitectura

Este proyecto sigue los principios de **Clean Architecture (Onion)** con **SOLID**:

```
ControlPeso.Thiscloud/
├── src/
│   ├── ControlPeso.Domain/         ← Núcleo (sin dependencias)
│   ├── ControlPeso.Application/    ← Lógica de negocio
│   ├── ControlPeso.Infrastructure/ ← Persistencia (EF Core + SQLite)
│   └── ControlPeso.Web/            ← Blazor Server UI
├── tests/
│   ├── ControlPeso.Domain.Tests/
│   ├── ControlPeso.Application.Tests/
│   └── ControlPeso.Infrastructure.Tests/
└── docs/
    ├── Plan_ControlPeso_Thiscloud_v1_0.md
    └── schema/
        └── schema_v1.sql           ← Contrato maestro (Database First)
```

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

El proyecto usa **Database First** con SQLite. El contrato maestro está en:

```
docs/schema/schema_v1.sql
```

**Reglas obligatorias**:
- ✅ Todo cambio de estructura EMPIEZA en el SQL
- ✅ Luego se aplica el SQL contra SQLite
- ✅ Luego se ejecuta scaffold de EF Core
- ❌ NUNCA se modifican las entidades C# manualmente

### Scaffold de entidades

```bash
dotnet ef dbcontext scaffold "Data Source=controlpeso.db" \
  Microsoft.EntityFrameworkCore.Sqlite \
  --context ControlPesoDbContext \
  --output-dir ../ControlPeso.Domain/Entities \
  --context-dir . \
  --project src/ControlPeso.Infrastructure \
  --force
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

### Estado del Proyecto

**✅ Completado (Backend)**:
- ✅ Fase 0: Setup completo (7/7 tareas)
- ✅ Fase 1: Schema SQL + Scaffold + Domain (7/7 tareas)
- ✅ Fase 1.5: Integración ThisCloud.Framework + .NET 10 (10/10 tareas)
- ✅ Fase 2: Application Layer completo (8/8 tareas, 176 tests, 90.7% cobertura)
- ✅ Fase 3: Infrastructure Layer (3/3 tareas)
- ✅ Fase 4: Autenticación OAuth (8/8 tareas - Google + LinkedIn)
- ✅ Fase 5: UI Core (9/9 tareas - Layout + Dashboard + componentes base)
- ✅ Fase 6: Páginas secundarias (5/5 tareas - Profile + History + Trends)
- ✅ Fase 7: Admin Panel (5/5 tareas - gestión usuarios + CSV export)
- ✅ Fase 8: SEO + Analytics + Security (10/10 tareas)

**⏳ Pendiente (Frontend - Pixel Perfect)**:
- 🔴 Refinar diseño visual según prototipo Google AI Studio
- 🔴 Ajustar espaciados, tamaños, colores MudBlazor
- 🔴 Optimizar responsive design (mobile, tablet, desktop)
- 🔴 Pulir transiciones y animaciones
- 🔴 Mejorar iconografía y consistencia visual
- 🔴 Testing UX exhaustivo con usuarios reales
- 🔴 A11y testing manual completo (keyboard nav, screen readers)
- 🔴 Performance optimization (lighthouse score 90+)

### Documentación Técnica

- [Plan del proyecto (v1.0)](docs/Plan_ControlPeso_Thiscloud_v1_0.md) — **Backend: 100% | Frontend: 50%**
- [Esquema de base de datos](docs/schema/schema_v1.sql)
- [Integración ThisCloud.Framework](docs/THISCLOUD_FRAMEWORK_INTEGRATION.md)
- [Arquitectura Onion](docs/ARCHITECTURE.md)
- [Seguridad](docs/SECURITY.md)
- [SEO](docs/SEO.md)
- [Deployment](docs/DEPLOYMENT.md)
- [Docker](docs/DOCKER.md)
- [WCAG AA Audit](docs/WCAG_AA_AUDIT.md)

## 🧪 Cobertura de tests

Target: **85% mínimo** | Actual: **90.7% (Application layer)**

```bash
# Ejecutar tests con cobertura
dotnet test --collect:"XPlat Code Coverage"

# Ver reporte HTML (requiere reportgenerator)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report
```

**Estado actual**:
- ✅ Application layer: 90.7% (1036/1181 líneas)
- ✅ 176/176 tests passing
- ✅ Branch coverage: 96.7%

**Testing Backend**: ✅ Completo  
**Testing Frontend**: ⏳ Pendiente (UI/UX testing manual)

## 🤝 Contribuir

Este proyecto sigue **Git Flow**:

- `main` — Producción estable
- `develop` — Rama de desarrollo
- `feature/*` — Nuevas funcionalidades

**Pull Requests obligatorios** para cambios. No commits directos a `main/develop`.

## 📄 Licencia

Este proyecto está bajo licencia **MIT**. Ver [LICENSE](LICENSE) para más detalles.

## 📧 Contacto

**Thiscloud Services**  
Email: [contacto@thiscloud.com](mailto:contacto@thiscloud.com)  
GitHub: [@mdesantis1984](https://github.com/mdesantis1984)

---

© 2026 Thiscloud Services. All rights reserved.
