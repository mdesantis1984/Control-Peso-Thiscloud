# Control Peso Thiscloud

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)
![MudBlazor](https://img.shields.io/badge/MudBlazor-8.0.0-594AE2)
![License](https://img.shields.io/badge/License-MIT-green)

Aplicación web minimalista de control de peso corporal construida con **Blazor Server (.NET 9)** y **MudBlazor**.

## ✨ Características

- ✅ Autenticación exclusiva con **Google OAuth 2.0**
- ✅ Dashboard con métricas actuales y gráficos de evolución
- ✅ Registro de peso con fecha, hora, notas y tendencia automática
- ✅ Historial con búsqueda, filtros y paginación
- ✅ Análisis de tendencias con proyecciones y Smart Insights
- ✅ Panel de administración (gestión de usuarios y roles)
- ✅ Soporte bilingüe: **Español** / **English**
- ✅ Soporte de unidades: **Métrico (kg, cm)** / **Imperial (lb, ft/in)**
- ✅ Tema oscuro optimizado para UX

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
| **Framework** | .NET 9.0 |
| **UI** | Blazor Server |
| **Componentes UI** | MudBlazor 8.0.0 |
| **ORM** | Entity Framework Core 9.0.1 (Database First) |
| **Base de datos** | SQLite (dev/MVP) → SQL Server (prod) |
| **Validación** | FluentValidation 11.11.0 |
| **Logging** | Serilog 8.0.3 |
| **Testing** | xUnit 2.9.2 + Moq 4.20.72 + bUnit 1.34.7 |
| **Autenticación** | Google OAuth 2.0 |
| **Analytics** | Google Analytics 4 + Cloudflare Analytics |

## 🚀 Inicio rápido

### Prerrequisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
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

- [Plan del proyecto (v1.0)](docs/Plan_ControlPeso_Thiscloud_v1_0.md)
- [Esquema de base de datos](docs/schema/schema_v1.sql)

## 🧪 Cobertura de tests

Target: **85% mínimo**

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

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
