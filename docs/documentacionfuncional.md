# Documentación Funcional - Control Peso Thiscloud

> **Documento Maestro Funcional**  
> **Última actualización**: 2026-03-03  
> **Versión**: 1.0.0  
> **Estado**: ✅ Producción

---

## Tabla de Contenido

1. [Planes del Proyecto (Referencias)](#1-planes-del-proyecto-referencias)
2. [Sistemas Implementados](#2-sistemas-implementados)
3. [Accesibilidad & UI](#3-accesibilidad--ui)
4. [Testing & Cobertura](#4-testing--cobertura)

---

## 1. Planes del Proyecto (Referencias)

Los planes maestros del proyecto se preservan como documentos separados históricos:

### 1.1. Plan Maestro v1.0

📄 **Archivo**: `Plan_ControlPeso_Thiscloud_v1_0.md`

**Contenido**:
- Objetivo del proyecto
- Alcance funcional completo (8 módulos)
- Arquitectura Onion mandatoria
- Stack tecnológico (.NET 10, Blazor Server, MudBlazor)
- Database First workflow
- Fases 0-8 completadas (75.9% ejecutado)
- Reglas no negociables (SOLID, interfaces, Database First)

**Estado**: ✅ **Histórico** - Fases 0-8 completadas, Fase 10 en progreso

---

### 1.2. Fase 9 - Pixel Perfect

📄 **Archivo**: `Plan_Fase_9_Pixel_Perfect.md`

**Contenido**:
- Sistema de notificaciones históricas
- Mejoras visuales MudBlazor
- Responsive design optimizado
- Animaciones y transiciones
- Dark mode refinements

**Estado**: ✅ **Completado** - 100% implementado

---

### 1.3. Fase 10 - Globalización

📄 **Archivo**: `Plan_Fase_10_Globalizacion.md`

**Contenido**:
- Implementación completa i18n con `IStringLocalizer`
- Archivos `.resx` para soporte bilingüe Español (es-AR) / Inglés (en-US)
- `LanguageSelector` component integrado con ASP.NET Core localization
- Cambio de idioma en tiempo real (CultureInfo + cookie + forceLoad)
- Refactorización de TODOS los componentes/páginas para usar recursos localizables
- 20 tareas (P10.1-P10.20)

**Estado**: 🔵 **En Progreso** - Fase activa

---

## 2. Sistemas Implementados

### 2.1. Sistema de Notificaciones Históricas

**Implementación**: Fase 9 - Pixel Perfect  
**Estado**: ✅ Completado

#### Descripción

Sistema completo de notificaciones históricas con:
- ✅ Guardado de todas las notificaciones en base de datos
- ✅ Historial de notificaciones con panel interactivo (MudPopover)
- ✅ Badge con contador de notificaciones no leídas
- ✅ Respeto de preferencias de usuario (mostrar/ocultar Snackbars)
- ✅ Marcar como leídas y borrar notificaciones
- ✅ Observer pattern para comunicación entre componentes

#### Arquitectura (Onion)

```
Domain
  └── Enums/
      └── NotificationSeverity.cs    (Normal, Info, Success, Warning, Error)

Application
  ├── DTOs/
  │   ├── UserNotificationDto.cs
  │   └── CreateUserNotificationDto.cs
  └── Interfaces/
      └── IUserNotificationService.cs (CRUD completo)

Infrastructure
  ├── Entities/
  │   └── UserNotifications.cs       (Scaffolded desde SQL)
  └── Services/
      └── UserNotificationService.cs (Implementación con EF Core)

Web
  ├── Services/
  │   └── NotificationService.cs     (Wrapper + guardado historial)
  └── Components/Shared/
      ├── NotificationBell.razor     (Badge + toggle panel)
      ├── NotificationBell.razor.cs
      ├── NotificationPanel.razor    (Panel popover con lista)
      └── NotificationPanel.razor.cs
```

#### Base de Datos

**Tabla**: `UserNotifications`

```sql
CREATE TABLE UserNotifications (
    Id          TEXT    NOT NULL PRIMARY KEY,    -- GUID
    UserId      TEXT    NOT NULL,                 -- FK → Users(Id)
    Type        INTEGER NOT NULL DEFAULT 0,       -- Severity (0-4)
    Title       TEXT    NULL,                     -- Max 200 chars
    Message     TEXT    NOT NULL,                 -- Max 1000 chars
    IsRead      INTEGER NOT NULL DEFAULT 0,       -- Boolean (0/1)
    CreatedAt   TEXT    NOT NULL,                 -- ISO 8601 datetime
    ReadAt      TEXT    NULL,                     -- ISO 8601 datetime
    
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
```

**Índices**:
- `IX_UserNotifications_UserId` - Búsqueda por usuario
- `IX_UserNotifications_CreatedAt` - Ordenamiento por fecha
- `IX_UserNotifications_IsRead` - Filtrado por estado
- `IX_UserNotifications_UserId_IsRead` - Consultas compuestas (contador)

#### Servicios

**IUserNotificationService** (Application Layer):

```csharp
public interface IUserNotificationService
{
    // Lectura
    Task<List<UserNotificationDto>> GetUnreadAsync(Guid userId, CancellationToken ct = default);
    Task<PagedResult<UserNotificationDto>> GetAllAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    
    // Escritura
    Task<UserNotificationDto> CreateAsync(CreateUserNotificationDto dto, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
    Task DeleteAsync(Guid notificationId, CancellationToken ct = default);
    Task DeleteAllAsync(Guid userId, CancellationToken ct = default);
}
```

**NotificationService** (Web Layer):

```csharp
public class NotificationService
{
    private readonly ISnackbar _snackbar;
    private readonly IUserNotificationService _notificationService;
    
    public event Action? OnNotificationAdded;  // Observer pattern
    
    public async Task ShowSuccessAsync(string message, string? title = null);
    public async Task ShowInfoAsync(string message, string? title = null);
    public async Task ShowWarningAsync(string message, string? title = null);
    public async Task ShowErrorAsync(string message, string? title = null);
}
```

#### Componentes UI

**NotificationBell** (MudBadge + MudIconButton):

```razor
<MudBadge Content="@UnreadCount" 
          Overlap="true" 
          Color="Color.Error" 
          Visible="@(UnreadCount > 0)">
    <MudIconButton Icon="@Icons.Material.Filled.Notifications" 
                   Color="Color.Inherit" 
                   OnClick="@TogglePanel" />
</MudBadge>

<MudPopover Open="@IsOpen" 
            AnchorOrigin="Origin.BottomRight" 
            TransformOrigin="Origin.TopRight">
    <NotificationPanel UserId="@UserId" OnClose="@ClosePanel" />
</MudPopover>
```

**NotificationPanel** (MudList + paginación):

- Lista de notificaciones con scroll
- Marcar como leída (click en item)
- Marcar todas como leídas (botón)
- Borrar todas (botón)
- Paginación para cargar más (load more)

#### Diagrams de Flujo

**Crear Notificación**:

```
Usuario ejecuta acción (ej: guardar peso)
         ↓
Service layer call
         ↓
NotificationService.ShowSuccessAsync("Peso guardado", "Éxito")
         ↓
[A] MudSnackbar.Add() → UI immediate feedback
[B] IUserNotificationService.CreateAsync() → DB persistence
         ↓
OnNotificationAdded?.Invoke() → Observer notifica componentes
         ↓
NotificationBell actualiza badge (UnreadCount++)
```

**Leer Notificaciones**:

```
Usuario click en NotificationBell
         ↓
TogglePanel() → Open MudPopover
         ↓
NotificationPanel.OnInitializedAsync()
         ↓
IUserNotificationService.GetUnreadAsync(userId)
         ↓
Renderiza lista en MudList
         ↓
Usuario click en notificación
         ↓
IUserNotificationService.MarkAsReadAsync(notificationId)
         ↓
UI actualiza estilo (gris = leída)
         ↓
Badge decrementa contador
```

---

### 2.2. Integración ThisCloud Framework

**Análisis**: Fase preparatoria  
**Estado**: ✅ Análisis completo | 🔵 Integración parcial (solo Loggings)

#### Framework Overview

**ThisCloud.Framework** es un framework modular construido sobre .NET 10 (LTS) con paquetes NuGet públicos que proporciona funcionalidad estandarizada enterprise-grade.

**Repositorio**: https://github.com/mdesantis1984/ThisCloud.Framework  
**Licencia**: ISC (permisiva, compatible)  
**Autor**: Marco Alejandro De Santis (usuario actual)

#### Componentes Evaluados

| Componente | Propósito | Decisión | Razón |
|------------|-----------|----------|-------|
| **ThisCloud.Framework.Loggings.Serilog** | Structured logging | ✅ **USAR** | Mejora significativa vs Serilog directo |
| **ThisCloud.Framework.Loggings.Admin** | Runtime log config | ⚠️ **REVISAR** | Útil pero requiere seguridad extra |
| **ThisCloud.Framework.Web** | Minimal APIs helpers | ❌ **NO USAR** | Blazor Server NO usa Minimal APIs (excepto SEO endpoints) |
| **ThisCloud.Framework.Contracts** | HTTP contracts | ❌ **NO USAR** | No hay APIs REST externas en v1.0 |

#### ThisCloud.Framework.Loggings.Serilog ✅

**Ventajas sobre Serilog directo**:
- ✅ **Redaction automática** de secretos/PII (no replicable fácilmente)
- ✅ **Correlation ID** tracking out-of-the-box
- ✅ **File rolling configurado** (10MB, NDJSON, 30 días retention)
- ✅ **Fail-fast** en Production (config inválida detiene arranque)
- ✅ **Zero boilerplate** (1 línea en Program.cs)

**Configuración típica** (appsettings.json):

```json
{
  "ThisCloud": {
    "Loggings": {
      "IsEnabled": true,
      "MinimumLevel": "Information",
      "Console": {
        "Enabled": true
      },
      "File": {
        "Enabled": true,
        "Path": "logs/log-.ndjson",
        "RollingFileSizeMB": 10,
        "RetainedFileCountLimit": 30,
        "UseCompactJson": true
      },
      "Redaction": {
        "Enabled": true  // ← Auto-redact secretos
      },
      "Correlation": {
        "HeaderName": "X-Correlation-Id",
        "GenerateIfMissing": true
      }
    }
  }
}
```

**Uso mínimo** (Program.cs):

```csharp
// Antes (Serilog directo - ~50 líneas config)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Después (ThisCloud.Framework.Loggings - 2 líneas)
builder.Host.UseThisCloudFrameworkSerilog(builder.Configuration, serviceName: "controlpeso-web");
builder.Services.AddThisCloudFrameworkLoggings(builder.Configuration, serviceName: "controlpeso-web");
```

**Redaction automática**:

```csharp
// ❌ MAL - Sin framework
_logger.LogInformation("User logged in with token {Token}", authToken);
// Output: User logged in with token eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

// ✅ BIEN - Con ThisCloud.Framework.Loggings (auto-redacted)
_logger.LogInformation("User logged in with token {Token}", authToken);
// Output: User logged in with token [REDACTED]
```

**Correlation ID** (requests relacionados):

```csharp
// Request 1: POST /weightlog
// [INF] Creating weight log for user {UserId} | CorrelationId: abc-123

// Request 2: GET /weightlog (mismo usuario)
// [INF] Fetching weight logs for user {UserId} | CorrelationId: abc-123
// ← Mismo CorrelationId para debugging cross-request
```

#### Decisión Final

**Integrar**: `ThisCloud.Framework.Loggings.Serilog` ✅  
**Ignorar**: `ThisCloud.Framework.Web` (no aplica para Blazor Server)  
**Postponer**: `ThisCloud.Framework.Loggings.Admin` (hasta Fase 11 - Admin features)

**Actualización requerida**: Target framework `.NET 10` en framework (actualmente `.NET 9`).

---

## 3. Accesibilidad & UI

### 3.1. Auditoría WCAG AA

**Fecha**: 2026-02-18  
**Estado**: ✅ Auditoría completa realizada  
**Score actual**: ⭐ **AA Compliant** (mayoría de criterios cumplidos)

#### Criterios Evaluados

| Criterio WCAG | Nivel | Estado | Notas |
|---------------|-------|--------|-------|
| **1.1.1 Non-text Content** | A | ✅ | Todos los `<img>` tienen `alt` text |
| **1.3.1 Info and Relationships** | A | ✅ | HTML semántico correcto (header, nav, main, footer) |
| **1.4.3 Contrast (Minimum)** | AA | ✅ | Ratio ≥4.5:1 en modo light, ≥7:1 en dark |
| **1.4.6 Contrast (Enhanced)** | AAA | ⚠️ | Algunos textos secundarios <7:1 |
| **2.1.1 Keyboard** | A | ✅ | Navegación por teclado funcional (Tab, Enter, Esc) |
| **2.4.1 Bypass Blocks** | A | ✅ | Skip links implementados |
| **2.4.3 Focus Order** | A | ✅ | Orden lógico de tab |
| **2.4.7 Focus Visible** | AA | ✅ | MudBlazor maneja focus styles |
| **3.1.1 Language of Page** | A | ✅ | `<html lang="es">` o `lang="en"` |
| **3.2.1 On Focus** | A | ✅ | No hay cambios inesperados |
| **3.3.1 Error Identification** | A | ✅ | FluentValidation + MudForm errors |
| **3.3.2 Labels or Instructions** | A | ✅ | Todos los `MudTextField` tienen `Label` |
| **4.1.1 Parsing** | A | ✅ | HTML válido (W3C validator) |
| **4.1.2 Name, Role, Value** | A | ✅ | ARIA roles correctos (MudBlazor) |

#### Mejoras Aplicadas

1. **Contraste de colores**: 
   - Theme dark con contraste mejorado (background más oscuro)
   - Textos secundarios con `Color.Secondary` (mayor contraste)

2. **Navegación por teclado**:
   - Focus visible en todos los componentes interactivos
   - Shortcuts: `Alt+D` (Dashboard), `Alt+P` (Profile), `Alt+H` (History)

3. **Screen reader support**:
   - `aria-label` en iconos sin texto
   - `aria-live="polite"` en MudSnackbar (notificaciones)
   - `role="region"` en secciones principales

4. **Formularios**:
   - Labels explícitos en todos los inputs
   - Error messages descriptivos con `MudForm` validation
   - Required fields indicados con asterisco

5. **Open Graph meta tags**:
   - `og:title`, `og:description`, `og:image` en todas las páginas públicas
   - Twitter Card metadata

#### Herramientas Usadas

- **axe DevTools**: Chrome extension para auditoría automática
- **WAVE**: Web Accessibility Evaluation Tool
- **Lighthouse**: Chrome DevTools Accessibility audit
- **Manual testing**: Navegación por teclado, screen reader (NVDA)

#### Recomendaciones Pendientes

1. ⚠️ **Mejorar contraste AAA** (Nivel superior a AA):
   - Textos secundarios: ratio actual 5.5:1 → target 7:1
   - Botones disabled: mejorar contraste vs background

2. ⚠️ **ARIA live regions** más granulares:
   - Dashboard stats: `aria-live="polite"` cuando actualizan
   - Chart updates: anunciar cambios para screen readers

3. ⚠️ **Skip navigation** más completo:
   - Skip to main content ✅ (implementado)
   - Skip to search (planned)
   - Skip to filters (planned)

---

## 4. Testing & Cobertura

### 4.1. Test Coverage Report

**Fecha de generación**: 2026-02-26  
**Branch**: `feature/legal-pages-and-branding`  
**Estado**: ⭐ **EXCELENTE** (91% Line Coverage)

#### Resumen Ejecutivo

| Métrica Global | Valor | Estado |
|---------------|-------|--------|
| **Total Tests** | 551 | ✅ Excelente |
| **Tests Exitosos** | 551 (100%) | ✅ Perfecto |
| **Tests Fallidos** | 0 | ✅ Perfecto |
| **Line Coverage** | 91% | ⭐ Excelente |
| **Branch Coverage** | 87% | ⭐ Excelente |
| **Method Coverage** | 97.3% | ⭐ Excelente |

#### Distribución de Tests por Proyecto

| Proyecto | Tests | Line Coverage | Branch Coverage | Method Coverage |
|----------|-------|---------------|-----------------|-----------------|
| **Domain.Tests** | 86 | 91.1% (82/90) | N/A | 95.5% (64/67) |
| **Application.Tests** | 297 | 92.3% (1,251/1,356) | 88.2% | 98.1% |
| **Infrastructure.Tests** | 117 | 89.7% (412/459) | 85.4% | 96.8% |
| **Shared.Resources.Tests** | 51 | 100% (45/45) | 100% | 100% |
| **Web** | 0 | N/A | N/A | N/A |

#### Cobertura por Capa (Onion)

##### 1. Domain Layer - 91.1% ⭐

**Componentes testeados**:
- ✅ `Entities.AuditLog` - 100%
- ✅ `Entities.UserNotifications` - 100%
- ✅ `Entities.UserPreferences` - 100%
- ✅ `Entities.Users` - 100%
- ✅ `Entities.WeightLogs` - 100%
- ✅ `Exceptions.DomainException` - 100%
- ⚠️ `Exceptions.NotFoundException` - 75% (falta testing de constructores)
- ⚠️ `Exceptions.ValidationException` - 64.2% (falta testing de serialización)

**Tests existentes**: `tests/ControlPeso.Domain.Tests/`
- `Entities/UsersTests.cs`
- `Entities/WeightLogsTests.cs`
- `Enums/EnumsTests.cs`
- `Exceptions/ExceptionsTests.cs`

##### 2. Application Layer - 92.3% ⭐

**Servicios testeados**:
- ✅ `WeightLogService` - 95.8%
- ✅ `UserService` - 93.2%
- ✅ `TrendService` - 89.1%
- ✅ `AdminService` - 91.4%
- ✅ `UserNotificationService` - 100% (sistema de notificaciones)

**DTOs testeados**:
- ✅ Todos los DTOs con validación FluentValidation - 100%
- ✅ Mappers (Entity ↔ DTO) - 100%

**Tests existentes**: `tests/ControlPeso.Application.Tests/`
- `Services/WeightLogServiceTests.cs` (75 tests)
- `Services/UserServiceTests.cs` (62 tests)
- `Services/TrendServiceTests.cs` (48 tests)
- `Services/AdminServiceTests.cs` (41 tests)
- `Validators/` (71 tests para FluentValidation)

##### 3. Infrastructure Layer - 89.7% ⭐

**Componentes testeados**:
- ✅ `ControlPesoDbContext` - 92.1%
- ✅ `DbSeeder` - 100%
- ⚠️ `UserRepository` - 78.3% (faltan tests para edge cases)

**Tests existentes**: `tests/ControlPeso.Infrastructure.Tests/`
- `Data/ControlPesoDbContextTests.cs`
- `Data/DbSeederTests.cs`
- `Repositories/UserRepositoryTests.cs`

##### 4. Shared.Resources Layer - 100% ⭐

**Componentes testeados**:
- ✅ Archivos `.resx` (validación de formato)
- ✅ `ResourceKeyValidator` - 100%
- ✅ Consistency checks (es-AR ↔ en-US) - 100%

**Tests existentes**: `tests/ControlPeso.Shared.Resources.Tests/`
- `ResourceValidatorTests.cs`

##### 5. Web Layer - 0% ⚠️

**Estado**: Sin tests (opcional para Blazor components)

**Razón**: 
- Blazor components requieren **bUnit** (framework de testing)
- Tests de componentes son más costosos de mantener
- La lógica de negocio está en Application (ya testeada)
- Components solo tienen UI + binding (mínima lógica)

**Consideración futura**: 
- Si se agregan componentes complejos con lógica → agregar bUnit tests
- Prioridad BAJA (Application layer tiene 92.3% coverage)

---

### 4.2. Testing Strategy

#### Frameworks Usados

| Framework | Propósito | Versión |
|-----------|-----------|---------|
| **xUnit** | Test runner | 2.9.2 |
| **Moq** | Mocking (interfaces, DbContext) | 4.20.72 |
| **FluentAssertions** | Assertions legibles | 7.0.0 |
| **bUnit** | Blazor component testing (planned) | 1.34.7 |

#### Patrón de Tests

**AAA Pattern** (Arrange-Act-Assert):

```csharp
[Fact]
public async Task CreateAsync_ValidDto_ReturnsWeightLogDto()
{
    // Arrange
    var dto = new CreateWeightLogDto
    {
        UserId = Guid.NewGuid(),
        Date = DateOnly.FromDateTime(DateTime.Today),
        Time = TimeOnly.FromDateTime(DateTime.Now),
        Weight = 75.5,
        DisplayUnit = WeightUnit.Kg
    };
    
    var mockContext = new Mock<ControlPesoDbContext>();
    var service = new WeightLogService(mockContext.Object, _logger);
    
    // Act
    var result = await service.CreateAsync(dto);
    
    // Assert
    result.Should().NotBeNull();
    result.Id.Should().NotBeEmpty();
    result.Weight.Should().Be(75.5);
}
```

#### Naming Convention

**Formato**: `MethodName_Scenario_ExpectedResult`

**Ejemplos**:
- `CreateAsync_ValidDto_ReturnsWeightLogDto`
- `CreateAsync_NullDto_ThrowsArgumentNullException`
- `GetByIdAsync_NonExistentId_ThrowsNotFoundException`
- `UpdateAsync_ConcurrentUpdate_ThrowsDbUpdateConcurrencyException`

#### Coverage Goals

| Layer | Target Coverage | Actual | Status |
|-------|-----------------|--------|--------|
| **Domain** | ≥90% | 91.1% | ✅ |
| **Application** | ≥90% | 92.3% | ✅ |
| **Infrastructure** | ≥85% | 89.7% | ✅ |
| **Shared.Resources** | 100% | 100% | ✅ |
| **Web** | ≥70% (opcional) | 0% | ⚠️ Planned |

#### CI/CD Integration

**GitHub Actions** (planned):

```yaml
name: CI - Tests + Coverage

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore -c Release
      
      - name: Run tests
        run: dotnet test --no-build -c Release --collect:"XPlat Code Coverage"
      
      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v4
        with:
          files: ./coverage.xml
          fail_ci_if_error: true
```

---

### 4.3. Análisis de Gaps (Cobertura)

#### Prioridad ALTA

1. **NotFoundException constructors** (Domain)
   - Current: 75%
   - Target: 100%
   - Effort: 1 hora

2. **ValidationException serialization** (Domain)
   - Current: 64.2%
   - Target: 100%
   - Effort: 2 horas

3. **UserRepository edge cases** (Infrastructure)
   - Current: 78.3%
   - Target: ≥90%
   - Effort: 3 horas

#### Prioridad MEDIA

4. **TrendService boundary conditions** (Application)
   - Current: 89.1%
   - Target: ≥95%
   - Effort: 4 horas

5. **Admin CSV export** (Application)
   - Current: no tests específicos
   - Target: ≥90%
   - Effort: 3 horas

#### Prioridad BAJA

6. **Web components con bUnit** (Web)
   - Current: 0%
   - Target: ≥70% (opcional)
   - Effort: 2 semanas (alto costo, beneficio limitado)

---

## Referencias

- **Plan Maestro**: `Plan_ControlPeso_Thiscloud_v1_0.md` (histórico)
- **Fase 10**: `Plan_Fase_10_Globalizacion.md` (en progreso)
- **Fase 9**: `Plan_Fase_9_Pixel_Perfect.md` (completado)
- **Infraestructura**: `infraestructura.md`
- **Soporte**: `soporte.md`
- **Copilot Instructions**: `.github/copilot-instructions.md`

---

## Changelog

| Fecha | Cambios |
|-------|---------|
| 2026-03-03 | Documento creado - Consolidación de 4 archivos fuente + 3 planes referenciados |
| 2026-03-03 | Agregado: Sistema de notificaciones, ThisCloud Framework, WCAG audit, Test coverage 91% |

---

**FIN DEL DOCUMENTO** ✅
