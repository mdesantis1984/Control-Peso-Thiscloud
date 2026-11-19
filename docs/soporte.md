# Soporte & Troubleshooting - Control Peso Thiscloud

> **Documento Maestro de Soporte**  
> **Última actualización**: 2026-03-03  
> **Versión**: 1.0.0  
> **Estado**: ✅ Producción

---

## Tabla de Contenido

1. [Logging & Diagnóstico](#1-logging--diagnóstico)
2. [Alertas de errores críticos](#2-alertas-de-errores-críticos)
3. [Blazor Técnico](#3-blazor-técnico)
4. [Comandos & Scripts de Referencia](#4-comandos--scripts-de-referencia)

---

## 1. Logging & Diagnóstico

### 1.1. Best Practices

#### Principios Fundamentales

**1. Signal over Noise**
- ❌ NO loguear TODO - logs ruidosos ocultan problemas reales
- ✅ Loguear eventos IMPORTANTES con contexto accionable
- ✅ Usar niveles correctos para filtrado efectivo

**2. Structured Logging Always**
- ❌ NO usar string interpolation: `$"User {userId} logged in"`
- ✅ Usar parámetros nombrados: `"User {UserId} logged in", userId`
- **Por qué**: Permite queries y agregaciones por propiedades

**3. Contexto es Rey**
- ❌ Log: `"Error saving data"`
- ✅ Log: `"Error saving weight log - UserId: {UserId}, Date: {Date}", userId, date`
- **Incluir**: IDs de entidades, operación fallida, valores de entrada

---

#### Niveles de Log - Cuándo Usar Cada Uno

##### `Debug` (Development only)

**Cuándo**: Detalles técnicos internos para diagnóstico de desarrollo

```csharp
_logger.LogDebug("Initializing Cropper.js for element: {ElementId}", elementId);
_logger.LogDebug("Circuit opened - CircuitId: {CircuitId}", circuitId);
```

**Características**:
- Deshabilitado en Production
- Puede ser verbose
- Para entender flujo interno

---

##### `Information` (Default)

**Cuándo**: Eventos normales de negocio que documentan la actividad de la aplicación

```csharp
_logger.LogInformation("User logged in - UserId: {UserId}, Email: {Email}", userId, email);
_logger.LogInformation("Weight log created - Id: {WeightLogId}, UserId: {UserId}, Weight: {Weight}kg", id, userId, weight);
_logger.LogInformation("Request completed - Path: {Path}, Duration: {DurationMs}ms", path, duration);
```

**Características**:
- Nivel por defecto en Production
- Documenta flujo de negocio normal
- No spam - solo eventos significativos

---

##### `Warning` (Situaciones anómalas recuperables)

**Cuándo**: Algo raro pero manejado, comportamiento subóptimo, degradación

```csharp
_logger.LogWarning("Slow request detected - Duration: {DurationMs}ms exceeds threshold", durationMs);
_logger.LogWarning("Cropper.js library not loaded - this may indicate CDN issue");
_logger.LogWarning("Blazor circuit connection lost - CircuitId: {CircuitId}", circuitId);
```

**Características**:
- Indica problema potencial que necesita investigación
- Sistema sigue funcionando
- Puede requerir acción correctiva

---

##### `Error` (Operación falló)

**Cuándo**: Una operación NO se completó, pero la app sigue corriendo

```csharp
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Error saving weight log - UserId: {UserId}", userId);
    throw; // Re-throw para que el caller maneje
}
```

**Características**:
- ✅ **SIEMPRE** incluir la excepción como primer parámetro
- ✅ **SIEMPRE** incluir contexto de lo que se estaba haciendo
- ❌ **NO** usar para validación fallida (usar Warning)

---

##### `Critical` / `Fatal` (Aplicación crasheando)

**Cuándo**: SOLO cuando la aplicación está cayendo o en estado corrupto

```csharp
_logger.LogCritical(ex, "Database connection lost - application cannot start");
_logger.LogCritical("Configuration validation failed - missing required settings");
```

**Características**:
- Rarísimo - menos de 1 vez por mes
- Requiere intervención INMEDIATA
- Generalmente acompañado de crash

---

#### Emojis en Logs (Identificación Visual)

Control Peso Thiscloud usa **emojis como marcadores visuales** para identificar rápidamente el origen en logs.

| Emoji | Contexto | Ejemplo |
|-------|----------|---------|
| 🤖 | `/robots.txt` requests | `🤖 /robots.txt requested from {UserAgent}` |
| 🗺️ | `/sitemap.xml` requests | `🗺️ /sitemap.xml requested from {UserAgent}` |
| 🚀 | Controller methods | `🚀 Controller method executed` |
| 💾 | Database operations | `💾 Creating weight log for user {UserId}` |
| ✅ | Success, operación completada | `✅ Weight log created - Id: {Id}` |
| ❌ | Error, operación fallida | `❌ Database error creating weight log` |
| ⚠️ | Warning, situación anómala | `⚠️ Slow query detected - Duration: {Ms}ms` |
| ℹ️ | Information, evento normal | `ℹ️ User logged in successfully` |
| 🔐 | Authentication/Authorization | `🔐 Google OAuth callback received` |

**Ejemplo de uso**:

```csharp
_logger.LogInformation("🤖 /robots.txt requested from {UserAgent}", userAgent);
_logger.LogInformation("💾 Creating weight log for user {UserId} - Date: {Date}, Weight: {Weight}kg", userId, date, weight);
_logger.LogInformation("✅ Weight log created successfully - Id: {WeightLogId}", id);
_logger.LogError(ex, "❌ Database error creating weight log for user {UserId}", userId);
```

**Ventajas**:
- ✅ Identificación instantánea en consola Docker
- ✅ Grep/filter rápido: `docker logs controlpeso-web | grep "🤖"`
- ✅ Debug visual sin leer mensaje completo
- ✅ Diferenciación Controller vs Minimal API (🚀 vs 🤖)

---

#### Reglas PROHIBIDAS

**❌ NUNCA loguear secretos**:
- Tokens de autenticación
- Google ClientSecret
- Connection strings con passwords
- JWT tokens
- API keys

**❌ NUNCA loguear PII sin necesidad**:
- Passwords (obvio)
- Números de tarjeta de crédito
- Direcciones completas (solo ciudad/país si necesario)
- Números de teléfono completos

**❌ NUNCA usar `try/catch` vacíos**:

```csharp
// ❌ MAL
try { await _context.SaveChangesAsync(); } catch { }

// ✅ BIEN
try 
{ 
    await _context.SaveChangesAsync(); 
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Error saving weight log for user {UserId}", userId);
    throw; // Re-throw para que caller maneje
}
```

---

### 1.2. Query Guide

#### Archivo de Logs

**Location**: `src/ControlPeso.Web/logs/controlpeso-.ndjson`  
**Format**: NDJSON (Newline Delimited JSON)  
**Tools**: Seq, Grafana Loki, jq (CLI), VS Code con JSON Lines extension

---

#### Propiedades Clave de Correlación

Cada log incluye propiedades estructuradas:

```json
{
  "@t": "2026-03-03T19:35:00.1234567Z",     // Timestamp ISO 8601
  "@mt": "User retrieved by email...",       // Message template
  "@l": "Information",                       // Level (omitido si Information)
  "@tr": "8d893c0f8c27dcf3...",             // Trace ID (correlación request)
  "@sp": "a4346885f2a9dd0c",                 // Span ID (correlación operación)
  "UserId": "663252fd-8c18-4585-...",       // User ID (correlación usuario)
  "SourceContext": "ControlPeso.Application.Services.UserService",
  "RequestId": "0HNJHF92ABAR2:00000005",    // ASP.NET Core Request ID
  "RequestPath": "/dashboard",               // Request path
  "ConnectionId": "0HNJHF92ABAR2",          // Connection ID
  "service": "ControlPeso.Thiscloud",
  "env": "Development",
  "MachineName": "MDESANTIS",
  "ProcessId": 53520,
  "ThreadId": 15
}
```

---

#### Queries con `jq` (Línea de Comandos)

##### 1. Filtrar por nivel de log

```bash
# Solo errores
cat controlpeso-.ndjson | jq 'select(."@l" == "Error")'

# Errores + Warnings
cat controlpeso-.ndjson | jq 'select(."@l" == "Error" or ."@l" == "Warning")'

# Fatal (crashes)
cat controlpeso-.ndjson | jq 'select(."@l" == "Fatal")'
```

##### 2. Filtrar por usuario específico

```bash
# Logs de un usuario específico
cat controlpeso-.ndjson | jq 'select(.UserId == "663252fd-8c18-4585-9a8f-295e59e6e72a")'

# Contar logs por usuario
cat controlpeso-.ndjson | jq -s 'group_by(.UserId) | map({UserId: .[0].UserId, Count: length})'
```

##### 3. Correlación por Trace ID (request completo)

```bash
# Todos los logs de un request específico
TRACE_ID="8d893c0f8c27dcf3a497461ff8a87e7f"
cat controlpeso-.ndjson | jq --arg trace "$TRACE_ID" 'select(."@tr" == $trace or .traceId == $trace)'

# Ver flujo completo de un request ordenado por timestamp
cat controlpeso-.ndjson | jq --arg trace "$TRACE_ID" 'select(."@tr" == $trace)' | jq -s 'sort_by(."@t")'
```

##### 4. Filtrar por operación de negocio

```bash
# Operaciones de WeightLog
cat controlpeso-.ndjson | jq 'select(.SourceContext | contains("WeightLog"))'

# Operaciones de autenticación
cat controlpeso-.ndjson | jq 'select(.SourceContext | contains("Auth") or .SourceContext | contains("Claims"))'

# Operaciones de Admin
cat controlpeso-.ndjson | jq 'select(.SourceContext | contains("Admin"))'
```

##### 5. Filtrar con emojis

```bash
# Solo logs de robots.txt
docker logs controlpeso-web | grep "🤖"

# Solo logs de sitemap.xml
docker logs controlpeso-web | grep "🗺️"

# Todos los SEO endpoints
docker logs controlpeso-web | grep -E "🤖|🗺️"

# Solo errores
docker logs controlpeso-web | grep "❌"

# Flow completo con emojis
docker logs controlpeso-web | grep -E "🚀|🤖|🗺️|✅|❌"
```

---

#### Queries con Docker

##### Ver logs en tiempo real

```bash
# Últimas 50 líneas + follow
docker logs -f controlpeso-web --tail=50

# Solo errores/warnings en tiempo real
docker logs -f controlpeso-web 2>&1 | grep -E "Error|Warning"

# Solo SEO endpoints
docker logs -f controlpeso-web 2>&1 | grep -E "🤖|🗺️"
```

##### Búsqueda específica

```bash
# Buscar por usuario
docker logs controlpeso-web | grep "UserId.*663252fd"

# Buscar por fecha
docker logs controlpeso-web | grep "2026-03-03"

# Buscar errores de DB
docker logs controlpeso-web | grep -i "database.*error"

# Últimas 100 líneas con filtro
docker logs controlpeso-web --tail=100 | grep "Weight log"
```

##### Exportar logs

```bash
# Exportar logs a archivo
docker logs controlpeso-web > logs_$(date +%Y%m%d_%H%M%S).txt

# Exportar solo errores
docker logs controlpeso-web 2>&1 | grep Error > errors_$(date +%Y%m%d_%H%M%S).txt
```

---

### 1.3. ThisCloud.Framework.Loggings

Control Peso Thiscloud usa **ThisCloud.Framework.Loggings.Serilog** para logging estructurado enterprise-grade.

#### Features

- ✅ **Redaction automática** de secretos/PII
- ✅ **Correlation ID** tracking out-of-the-box
- ✅ **File rolling** configurado (10MB, NDJSON, 30 días retention)
- ✅ **Fail-fast** en Production (config inválida detiene arranque)
- ✅ **Zero boilerplate** (1 línea en Program.cs)

#### Configuración (appsettings.json)

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

#### Usage (Program.cs)

```csharp
// Setup Serilog
builder.Host.UseThisCloudFrameworkSerilog(
    builder.Configuration, 
    serviceName: "controlpeso-web"
);

// Register logging services
builder.Services.AddThisCloudFrameworkLoggings(
    builder.Configuration, 
    serviceName: "controlpeso-web"
);
```

#### Redaction Automática

```csharp
// ❌ Sin framework - secretos expuestos
_logger.LogInformation("User logged in with token {Token}", authToken);
// Output: User logged in with token eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

// ✅ Con ThisCloud.Framework.Loggings (auto-redacted)
_logger.LogInformation("User logged in with token {Token}", authToken);
// Output: User logged in with token [REDACTED]
```

**Palabras redactadas automáticamente**:
- `password`, `secret`, `token`, `authorization`, `api-key`
- `ClientSecret`, `ConnectionString` (si contiene password)
- Cualquier propiedad con nombre terminando en `Password` o `Secret`

---

## 2. Alertas de errores críticos

Los errores críticos se registran en `/app/logs` (Serilog, NDJSON); no hay alertas externas. Para consultarlos, ver la sección 1.

---

## 3. Blazor Técnico

### 3.1. Prerendering & JS Interop

#### Problema: JS Interop en Prerendering

Blazor Server **prerenderiza** componentes en el servidor antes de establecer la conexión SignalR. Durante el prerendering:

- ❌ **NO** hay JavaScript disponible
- ❌ **NO** hay `IJSRuntime` funcional
- ❌ **NO** hay localStorage/sessionStorage

**Síntoma**: `InvalidOperationException: JavaScript interop calls cannot be issued during server-side prerendering.`

---

#### Solución: OnAfterRender Lifecycle

```csharp
public partial class MyComponent : ComponentBase
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    
    private bool _isPrerendering = true;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _isPrerendering = false;
            
            // AHORA es seguro llamar JS interop
            await JSRuntime.InvokeVoidAsync("console.log", "Component rendered");
            
            StateHasChanged(); // Re-render si necesario
        }
    }
}
```

---

#### LocalStorage Access Pattern

```csharp
public partial class LanguageSelector : ComponentBase
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    
    private string? _selectedLanguage;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Leer de localStorage (solo después de render)
            _selectedLanguage = await JSRuntime.InvokeAsync<string?>(
                "localStorage.getItem", 
                "SelectedLanguage"
            );
            
            StateHasChanged();
        }
    }
    
    private async Task OnLanguageChanged(string newLanguage)
    {
        _selectedLanguage = newLanguage;
        
        // Guardar en localStorage
        await JSRuntime.InvokeVoidAsync(
            "localStorage.setItem", 
            "SelectedLanguage", 
            newLanguage
        );
        
        // Reload page para aplicar cultura
        await JSRuntime.InvokeVoidAsync("location.reload");
    }
}
```

---

#### Patrón Seguro: Check Prerendering

```csharp
private async Task<string?> GetFromLocalStorageAsync(string key)
{
    // Evitar JS interop durante prerendering
    if (_isPrerendering) return null;
    
    try
    {
        return await JSRuntime.InvokeAsync<string?>("localStorage.getItem", key);
    }
    catch (InvalidOperationException)
    {
        // Prerendering aún activo
        return null;
    }
}
```

---

### 3.2. Unit System Global State Management

#### Problema: State Sincronizado entre Componentes

Necesidad:
- Usuario cambia sistema de unidades (Metric ↔ Imperial) en Profile
- Dashboard, History, Trends deben actualizar INMEDIATAMENTE sin refresh
- State debe persistir entre sesiones

---

#### Solución: ProtectedBrowserStorage + Observer Pattern

**Service Layer**:

```csharp
public class UnitSystemService
{
    private readonly ProtectedSessionStorage _sessionStorage;
    
    public event Action<UnitSystem>? OnUnitSystemChanged;
    
    private UnitSystem _currentSystem = UnitSystem.Metric;
    
    public UnitSystemService(ProtectedSessionStorage sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }
    
    public async Task<UnitSystem> GetCurrentSystemAsync()
    {
        var result = await _sessionStorage.GetAsync<int>("UnitSystem");
        
        if (result.Success)
        {
            _currentSystem = (UnitSystem)result.Value;
        }
        
        return _currentSystem;
    }
    
    public async Task SetCurrentSystemAsync(UnitSystem newSystem)
    {
        _currentSystem = newSystem;
        
        await _sessionStorage.SetAsync("UnitSystem", (int)newSystem);
        
        // Notificar a todos los componentes suscritos
        OnUnitSystemChanged?.Invoke(newSystem);
    }
}
```

**Component Subscription**:

```csharp
public partial class Dashboard : ComponentBase, IDisposable
{
    [Inject] private UnitSystemService UnitSystemService { get; set; } = default!;
    
    private UnitSystem _currentSystem;
    
    protected override async Task OnInitializedAsync()
    {
        _currentSystem = await UnitSystemService.GetCurrentSystemAsync();
        
        // Suscribirse a cambios
        UnitSystemService.OnUnitSystemChanged += OnUnitSystemChanged;
    }
    
    private void OnUnitSystemChanged(UnitSystem newSystem)
    {
        _currentSystem = newSystem;
        
        // Re-render con nuevo sistema
        StateHasChanged();
    }
    
    public void Dispose()
    {
        // CRÍTICO: Desuscribirse para evitar memory leaks
        UnitSystemService.OnUnitSystemChanged -= OnUnitSystemChanged;
    }
}
```

**Profile Change**:

```csharp
private async Task OnUnitSystemChangedAsync(UnitSystem newSystem)
{
    // Guardar en DB
    await UserService.UpdateUnitSystemAsync(UserId, newSystem);
    
    // Actualizar state global (notifica a todos)
    await UnitSystemService.SetCurrentSystemAsync(newSystem);
    
    // Mostrar confirmación
    await NotificationService.ShowSuccessAsync("Sistema de unidades actualizado");
}
```

---

#### Ventajas de ProtectedBrowserStorage

- ✅ **Encrypted**: Datos cifrados en el navegador
- ✅ **Per-circuit isolation**: Cada usuario tiene su storage aislado
- ✅ **Session-based**: Se limpia al cerrar browser
- ✅ **No cookies**: No ocupa espacio en headers HTTP

**Limitaciones**:
- ⚠️ Max ~5MB de storage total
- ⚠️ Solo disponible después de `OnAfterRenderAsync` (no en prerendering)
- ⚠️ No persiste entre sesiones diferentes (by design)

---

## 4. Comandos & Scripts de Referencia

### 4.1. Docker Commands

#### Build & Deploy

```powershell
# Build image
docker build -t controlpeso-web:latest -f Dockerfile .

# Save image to tar
docker save controlpeso-web:latest -o controlpeso-web_latest.tar

# Transfer to server
scp controlpeso-web_latest.tar root@10.0.0.100:/tmp/

# Load image on server (via SSH)
ssh root@10.0.0.100 "cd /opt/controlpeso && docker load -i /tmp/controlpeso-web_latest.tar"

# Deploy on server
ssh root@10.0.0.100 "cd /opt/controlpeso && docker compose down && docker compose up -d"
```

#### Container Management

```powershell
# Start containers
docker compose up -d

# Stop containers
docker compose down

# Restart containers
docker compose restart

# View logs (last 50 lines + follow)
docker logs -f controlpeso-web --tail=50

# Exec into container
docker exec -it controlpeso-web /bin/bash

# Check container health
docker compose ps
docker inspect controlpeso-web | grep -A 10 Health
```

#### Cleanup

```powershell
# Remove old images (older than 24h)
docker image prune -a --filter "until=24h"

# Remove stopped containers
docker container prune

# Remove unused volumes
docker volume prune

# Full cleanup (⚠️ DANGEROUS)
docker system prune -a --volumes
```

---

### 4.2. Git Commands (Git Flow)

#### Feature Branch Workflow

```powershell
# Create feature branch
git checkout develop
git pull origin develop
git checkout -b feature/nombre-descriptivo

# Work + commit
git add .
git commit -m "feat(scope): descripción del cambio"

# Push to GitHub
git push origin feature/nombre-descriptivo

# Después del merge via PR:
git checkout develop
git pull origin develop

# Cleanup local branch
git branch -d feature/nombre-descriptivo

# Cleanup remote branch
git push origin --delete feature/nombre-descriptivo
```

#### Merge Conflicts

```powershell
# Pull latest develop
git checkout develop
git pull origin develop

# Merge develop into feature (resolve conflicts)
git checkout feature/mi-rama
git merge develop

# Resolve conflicts in VS Code
# Después de resolver:
git add .
git commit -m "chore: resolve merge conflicts with develop"
git push origin feature/mi-rama
```

---

### 4.3. EF Core Commands

#### Scaffold (Database First)

```powershell
# Desde src/ControlPeso.Infrastructure
dotnet ef dbcontext scaffold \
  "Server=localhost;Database=ControlPeso;User=sa;Password=<your_password>;TrustServerCertificate=true" \
  Microsoft.EntityFrameworkCore.SqlServer \
  --context ControlPesoDbContext \
  --output-dir ../ControlPeso.Domain/Entities \
  --context-dir . \
  --force \
  --no-pluralize
```

**IMPORTANTE**:
- ❌ **NO** usar migrations code-first
- ✅ Cambios de schema → modificar `docs/schema/schema_v1.sql` → aplicar con sqlcmd → re-scaffold
- ✅ Base de datos: **SQL Server Express (Linux)** en todos los entornos

---

### 4.4. Scripts Preservados

#### clearObjBinVs.ps1

**Propósito**: Limpieza de directorios temporales de build

```powershell
.\clearObjBinVs.ps1
```

**Qué hace**:
- Elimina `obj/` folders (compilación temporal)
- Elimina `bin/` folders (output de build)
- Elimina `.vs/` folders (Visual Studio cache)

**Cuándo usar**:
- Build corrupto
- Errores raros de compilación
- Cambio de branch con conflictos de build

---

#### setup-config.ps1

**Propósito**: Setup de configs desde templates

```powershell
.\setup-config.ps1
```

**Qué hace**:
- Crea `appsettings.Development.json` desde `.template`
- Crea `appsettings.Production.json` desde `.template`
- Preserva configs existentes (no sobreescribe)

**Cuándo usar**:
- Primer clone del repo
- Después de cambiar de branch (si perdiste configs)

---

#### Deploy-Docker-Local.ps1

**Propósito**: Deploy completo en Docker local

```powershell
.\scripts\Deploy-Docker-Local.ps1
```

**Qué hace**:
- Build .NET Release
- Build Docker image
- Start container con docker-compose
- Health check automático
- Display logs

**Cuándo usar**:
- Test local antes de push
- Verificar build Docker funcional

---

#### deploy-docker.ps1

**Propósito**: Deploy Docker producción con transfer a servidor

```powershell
.\scripts\deploy-docker.ps1
```

**Qué hace**:
- Build image
- Save to .tar
- Transfer via SCP
- Deploy remoto via SSH
- Health verification

**Cuándo usar**:
- Deployment a producción (Proxmox VM)
- SOLO después de verificar localmente

---

#### migrate-to-sqlserver.ps1

**Propósito**: Migración de SQLite (dev) a SQL Server (prod)

```powershell
.\scripts\migrate-to-sqlserver.ps1
```

**Qué hace**:
- Export schema de SQLite
- Convert a T-SQL
- Create SQL Server database
- Migrate data
- Update connection strings

**Cuándo usar**:
- Migración a production SQL Server (Fase 11 planned)

---

## Referencias

- **Infraestructura**: `infraestructura.md`
- **Documentación Funcional**: `documentacionfuncional.md`
- **Plan Maestro**: `Plan_ControlPeso_Thiscloud_v1_0.md` (histórico)
- **Copilot Instructions**: `.github/copilot-instructions.md`

---

## Changelog

| Fecha | Cambios |
|-------|---------|
| 2026-03-03 | Documento creado - Consolidación de 6 archivos fuente |
| 2026-03-03 | Agregado: Logging best practices, query guide, Blazor técnico, Docker/Git/EF commands |

---

**FIN DEL DOCUMENTO** ✅
