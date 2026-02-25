# Guía de Best Practices de Logging - Control Peso Thiscloud

**Objetivo**: Mantener logs útiles, limpios y eficientes para troubleshooting productivo.

---

## 🎯 Principios Fundamentales

### 1. **Signal over Noise**
- ❌ NO loguear TODO - logs ruidosos ocultan problemas reales
- ✅ Loguear eventos IMPORTANTES con contexto accionable
- ✅ Usar niveles correctos para filtrado efectivo

### 2. **Structured Logging Always**
- ❌ NO usar string interpolation: `$"User {userId} logged in"`
- ✅ Usar parámetros nombrados: `"User {UserId} logged in", userId`
- **Por qué**: Permite queries y agregaciones por propiedades

### 3. **Contexto es Rey**
- ❌ Log: `"Error saving data"`
- ✅ Log: `"Error saving weight log - UserId: {UserId}, Date: {Date}", userId, date`
- **Incluir**: IDs de entidades, operación fallida, valores de entrada

---

## 📊 Niveles de Log - Cuándo Usar Cada Uno

### `Debug` (Development only)
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

### `Information` (Default)
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

### `Warning` (Situaciones anómalas recuperables)
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

### `Error` (Operación falló)
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
- SIEMPRE incluir la excepción como primer parámetro
- SIEMPRE incluir contexto de lo que se estaba haciendo
- NO usar para validación fallida (usar Warning)

---

### `Critical` / `Fatal` (Aplicación crasheando)
**Cuándo**: SOLO cuando la aplicación está cayendo o en estado corrupto

```csharp
// ❌ MAL - esto NO es fatal
_logger.LogCritical("Circuit {CircuitId} opened", circuitId);

// ✅ BIEN - esto SI es fatal
_logger.LogCritical(ex, "CRITICAL: Database connection failed and app cannot start");
throw; // App debe terminar
```

**Características**:
- Reservado para crashes reales
- Implica que la app no puede continuar
- Dispara alertas de máxima prioridad

---

## ✅ Patrones Correctos

### Logging en Servicios (Application Layer)

```csharp
public sealed class WeightLogService : IWeightLogService
{
    private readonly DbContext _context;
    private readonly ILogger<WeightLogService> _logger;

    public async Task<WeightLogDto> CreateAsync(CreateWeightLogDto dto, CancellationToken ct = default)
    {
        // Usar scope para categorización
        using var scope = _logger.BeginBusinessScope("CreateWeightLog");
        
        _logger.LogInformation(
            "Creating weight log - UserId: {UserId}, Date: {Date}, Weight: {Weight}kg",
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
                "Database error creating weight log - UserId: {UserId}",
                dto.UserId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error creating weight log - UserId: {UserId}",
                dto.UserId);
            throw;
        }
    }
}
```

### Logging en Componentes Blazor

```csharp
public partial class Dashboard
{
    [Inject] private ILogger<Dashboard> Logger { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        Logger.LogDebug("Dashboard initializing for user {UserId}", _userId);

        try
        {
            await LoadDataAsync();
            Logger.LogInformation("Dashboard loaded successfully - UserId: {UserId}", _userId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading dashboard - UserId: {UserId}", _userId);
            _errorMessage = "Error cargando el dashboard";
        }
    }
}
```

### Logging en Middleware

```csharp
public async Task InvokeAsync(HttpContext context)
{
    var path = context.Request.Path;
    var method = context.Request.Method;

    _logger.LogDebug("Processing request - Method: {Method}, Path: {Path}", method, path);

    try
    {
        await _next(context);
        _logger.LogInformation(
            "Request completed - Method: {Method}, Path: {Path}, StatusCode: {StatusCode}",
            method, path, context.Response.StatusCode);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "Unhandled exception in middleware - Method: {Method}, Path: {Path}",
            method, path);
        throw;
    }
}
```

---

## ❌ Anti-Patrones (NO Hacer)

### 1. String Interpolation

```csharp
// ❌ MAL - no se pueden hacer queries por UserId
_logger.LogInformation($"User {userId} logged in");

// ✅ BIEN - UserId queda como propiedad estructurada
_logger.LogInformation("User {UserId} logged in", userId);
```

### 2. Logging Sin Contexto

```csharp
// ❌ MAL - no sabemos QUÉ falló ni DÓNDE
_logger.LogError("Error saving data");

// ✅ BIEN - contexto completo
_logger.LogError(ex, "Error saving weight log - UserId: {UserId}, Date: {Date}", userId, date);
```

### 3. Try/Catch Vacío

```csharp
// ❌ MAL - error silenciado, imposible de troubleshoot
try
{
    await DoSomethingAsync();
}
catch { }

// ✅ BIEN - loguear SIEMPRE
try
{
    await DoSomethingAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error executing operation - Context: {Context}", context);
    throw; // O manejar apropiadamente
}
```

### 4. Logging de Secretos

```csharp
// ❌ MAL - expone secretos en logs
_logger.LogInformation("Auth header: {AuthHeader}", authHeader);
_logger.LogDebug("Password: {Password}", password);
_logger.LogInformation("API key: {ApiKey}", apiKey);

// ✅ BIEN - NUNCA loguear secretos
_logger.LogInformation("Authentication successful - UserId: {UserId}", userId);
// Password, tokens, API keys NUNCA van al log
```

### 5. Nivel Incorrecto

```csharp
// ❌ MAL - Fatal para evento normal
_logger.LogCritical("User logged in");

// ❌ MAL - Error para validación fallida
_logger.LogError("Invalid email format");

// ✅ BIEN
_logger.LogInformation("User logged in - UserId: {UserId}", userId);
_logger.LogWarning("Validation failed - Field: Email, Reason: Invalid format");
```

### 6. Logging en Loops

```csharp
// ❌ MAL - genera miles de logs
foreach (var item in items)
{
    _logger.LogInformation("Processing item {ItemId}", item.Id);
    ProcessItem(item);
}

// ✅ BIEN - log agregado
_logger.LogInformation("Processing {Count} items", items.Count);
foreach (var item in items)
{
    ProcessItem(item); // Solo log si hay error específico
}
_logger.LogInformation("Processed {Count} items successfully", items.Count);
```

---

## 🔒 Seguridad en Logs

### ✅ Permitido loguear:
- User ID (GUID)
- Email (para correlación de soporte)
- Timestamps
- Códigos de error
- Nombres de entidades
- Valores de negocio (peso, fecha, etc.)

### ❌ NUNCA loguear:
- **Passwords** (plain text o hashed)
- **Tokens** (JWT, OAuth, API keys)
- **Authorization headers**
- **Session IDs**
- **Credit card numbers**
- **Datos de salud sensibles** (si aplica)

### Framework de Redaction

El framework `ThisCloud.Framework.Loggings` tiene **redaction automática** activada:

```json
"Redaction": {
  "Enabled": true
}
```

Pero **NO confiar solo en redaction** - evitar loguear secretos explícitamente.

---

## 📈 Performance Considerations

### 1. Usar Guards para Logs Costosos

```csharp
// ❌ MAL - serialización JSON costosa siempre ejecuta
_logger.LogDebug("Complex object: {Object}", JsonSerializer.Serialize(complexObject));

// ✅ BIEN - solo ejecuta si Debug está habilitado
if (_logger.IsEnabled(LogLevel.Debug))
{
    _logger.LogDebug("Complex object: {Object}", JsonSerializer.Serialize(complexObject));
}
```

### 2. Evitar Logging Excesivo en Hot Paths

```csharp
// ❌ MAL - log en loop de alta frecuencia
while (true)
{
    var value = await sensor.ReadAsync();
    _logger.LogInformation("Sensor value: {Value}", value); // 1000s de logs/segundo
}

// ✅ BIEN - log periódico o por cambios significativos
var lastLogTime = DateTime.UtcNow;
while (true)
{
    var value = await sensor.ReadAsync();
    if (DateTime.UtcNow - lastLogTime > TimeSpan.FromSeconds(10))
    {
        _logger.LogInformation("Sensor value: {Value}", value);
        lastLogTime = DateTime.UtcNow;
    }
}
```

---

## 🎨 Scopes para Categorización

Usar scopes para agregar contexto estructurado automático:

```csharp
// Business operations
using var scope = _logger.BeginBusinessScope("CreateWeightLog");
// Todos los logs dentro del scope tendrán: LogType="Business", Operation="CreateWeightLog"

// Infrastructure operations
using var scope = _logger.BeginInfrastructureScope("DatabaseBackup");

// Security operations
using var scope = _logger.BeginSecurityScope("PasswordChange");
```

**Beneficio**: Logs filtrables por categoría en dashboards.

---

## 📊 Métricas de Calidad de Logs

### Indicadores de Logs Saludables:

| Métrica | Target | Malo |
|---------|--------|------|
| Debug % en Production | 0% | >5% |
| Fatal/Critical % | <0.1% | >1% |
| Logs con contexto (propiedades) | >90% | <70% |
| Logs con excepciones en catch | 100% | <100% |
| Logs con secretos | 0 | >0 |

### Revisión Periódica:

```bash
# Análisis de niveles de log
cat logs/*.ndjson | jq '."@l"' | sort | uniq -c

# Detección de secretos (keywords a buscar)
grep -i "password\|token\|secret\|api.*key" logs/*.ndjson
```

---

## 🚀 Checklist para Nuevos Features

Antes de marcar feature como "Done":

- [ ] ✅ Logs con niveles correctos (no Fatal para eventos normales)
- [ ] ✅ Structured logging (parámetros nombrados, NO interpolation)
- [ ] ✅ Contexto incluido (IDs, operación, valores relevantes)
- [ ] ✅ Excepciones logueadas en TODO catch
- [ ] ✅ NO logs de secretos (passwords, tokens, keys)
- [ ] ✅ Scopes de categorización agregados (Business/Infrastructure/Security)
- [ ] ✅ Performance: no logs excesivos en loops
- [ ] ✅ Tests: verificar que logs tienen propiedades esperadas

---

## 🔍 Troubleshooting con Logs

Ver guía completa en: [`docs/LOGGING_QUERY_GUIDE.md`](./LOGGING_QUERY_GUIDE.md)

**Flujo típico de debugging**:

1. Identificar TraceId del request problemático
2. Filtrar todos los logs por ese TraceId
3. Ordenar por timestamp
4. Analizar flujo completo (entrada → operaciones → salida)
5. Identificar punto de falla con contexto

---

## 📚 Referencias

- **Logging extensions**: `src/ControlPeso.Application/Logging/LoggingExtensions.cs`
- **Framework config**: `appsettings.json` → `ThisCloud.Loggings`
- **Query guide**: `docs/LOGGING_QUERY_GUIDE.md`
- **Fix history**: `docs/LOGGING_FIXES_SUMMARY.md`

---

**Última actualización**: 2026-02-21  
**Versión**: 1.0  
**Revisar**: Cada release mayor
