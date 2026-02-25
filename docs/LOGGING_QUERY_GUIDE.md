# Guía de Consulta de Logs - Control Peso Thiscloud

**Archivo de logs**: `src/ControlPeso.Web/logs/controlpeso-.ndjson`  
**Formato**: NDJSON (Newline Delimited JSON)  
**Herramientas recomendadas**: Seq, Grafana Loki, jq (CLI), VS Code con extensión JSON Lines

---

## Propiedades Clave de Correlación

Cada log incluye propiedades estructuradas para correlación:

```json
{
  "@t": "2026-02-21T18:51:49.1801753Z",     // Timestamp ISO 8601
  "@mt": "User retrieved by email...",       // Message template
  "@l": "Information",                       // Level (omitido si Information)
  "@tr": "8d893c0f8c27dcf3...",             // Trace ID (correlación request)
  "@sp": "a4346885f2a9dd0c",                 // Span ID (correlación operación)
  "UserId": "663252fd-8c18-4585-...",       // User ID (correlación usuario)
  "SourceContext": "ControlPeso.Application.Services.UserService",
  "RequestId": "0HNJHF92ABAR2:00000005",    // ASP.NET Core Request ID
  "RequestPath": "/uploads/avatars/...",     // Request path
  "ConnectionId": "0HNJHF92ABAR2",          // Connection ID
  "service": "ControlPeso.Thiscloud",
  "env": "Development",
  "MachineName": "MDESANTIS",
  "ProcessId": 53520,
  "ThreadId": 15,
  "traceId": "8d893c0f8c27dcf3..."          // Duplicate trace (framework)
}
```

---

## Queries con `jq` (Línea de Comandos)

### 1. Filtrar por nivel de log

```bash
# Solo errores
cat controlpeso-.ndjson | jq 'select(."@l" == "Error")'

# Errores + Warnings
cat controlpeso-.ndjson | jq 'select(."@l" == "Error" or ."@l" == "Warning")'

# Fatal (crashes)
cat controlpeso-.ndjson | jq 'select(."@l" == "Fatal")'
```

### 2. Filtrar por usuario específico

```bash
# Logs de un usuario específico
cat controlpeso-.ndjson | jq 'select(.UserId == "663252fd-8c18-4585-9a8f-295e59e6e72a")'

# Contar logs por usuario
cat controlpeso-.ndjson | jq -s 'group_by(.UserId) | map({UserId: .[0].UserId, Count: length})'
```

### 3. Correlación por Trace ID (request completo)

```bash
# Todos los logs de un request específico
TRACE_ID="8d893c0f8c27dcf3a497461ff8a87e7f"
cat controlpeso-.ndjson | jq --arg trace "$TRACE_ID" 'select(."@tr" == $trace or .traceId == $trace)'

# Ver flujo completo de un request ordenado por timestamp
cat controlpeso-.ndjson | jq --arg trace "$TRACE_ID" 'select(."@tr" == $trace)' | jq -s 'sort_by(."@t")'
```

### 4. Filtrar por operación de negocio

```bash
# Operaciones de WeightLog
cat controlpeso-.ndjson | jq 'select(.SourceContext | contains("WeightLog"))'

# Operaciones de autenticación
cat controlpeso-.ndjson | jq 'select(.SourceContext | contains("Auth") or .SourceContext | contains("Claims"))'

# Operaciones de base de datos
cat controlpeso-.ndjson | jq 'select(.SourceContext | contains("EntityFrameworkCore"))'
```

### 5. Operaciones lentas (performance)

```bash
# Requests con duración > 1 segundo
cat controlpeso-.ndjson | jq 'select(.DurationMs > 1000)'

# Top 10 requests más lentos
cat controlpeso-.ndjson | jq 'select(.DurationMs != null)' | jq -s 'sort_by(.DurationMs) | reverse | .[0:10]'

# Promedio de duración por endpoint
cat controlpeso-.ndjson | jq 'select(.DurationMs != null)' | jq -s 'group_by(.RequestPath) | map({Path: .[0].RequestPath, AvgMs: ([.[].DurationMs] | add / length), Count: length})'
```

### 6. Filtrar por rango de tiempo

```bash
# Logs después de una fecha específica
cat controlpeso-.ndjson | jq 'select(."@t" > "2026-02-21T18:52:00Z")'

# Logs entre dos timestamps
cat controlpeso-.ndjson | jq 'select(."@t" >= "2026-02-21T18:51:00Z" and ."@t" <= "2026-02-21T18:52:00Z")'
```

---

## Queries con Seq (Web UI)

### 1. Filtrar por nivel

```sql
@Level = 'Error'
@Level = 'Warning'
@Level = 'Fatal'
```

### 2. Filtrar por usuario

```sql
UserId = '663252fd-8c18-4585-9a8f-295e59e6e72a'
UserId IS NOT NULL  -- Todos los logs con usuario identificado
```

### 3. Correlación completa de request

```sql
@TraceId = '8d893c0f8c27dcf3a497461ff8a87e7f'
```

### 4. Filtrar por fuente

```sql
SourceContext LIKE '%WeightLog%'
SourceContext LIKE '%UserService%'
SourceContext LIKE '%Auth%'
```

### 5. Performance

```sql
DurationMs > 1000  -- Requests lentos
DurationMs IS NOT NULL  -- Todos con medición de duración
```

### 6. Combinaciones complejas

```sql
-- Errores de un usuario específico en las últimas 24h
@Level = 'Error' AND UserId = '663252fd-...' AND @Timestamp > Now()-24h

-- Requests lentos que fallaron
DurationMs > 1000 AND StatusCode >= 400

-- Operaciones de negocio con errores
SourceContext LIKE '%Application.Services%' AND @Level = 'Error'
```

---

## Queries con Grafana Loki (LogQL)

### 1. Básico

```logql
{service="ControlPeso.Thiscloud"} |= "Error"
{service="ControlPeso.Thiscloud"} |= "Warning"
```

### 2. JSON parsing

```logql
{service="ControlPeso.Thiscloud"} | json | UserId = "663252fd-..."
{service="ControlPeso.Thiscloud"} | json | DurationMs > 1000
```

### 3. Rate y agregaciones

```logql
# Rate de errores por minuto
rate({service="ControlPeso.Thiscloud"} | json | __error__=""[1m])

# Count de logs por nivel
sum by (level) (count_over_time({service="ControlPeso.Thiscloud"}[1h]))
```

---

## Patrones de Debugging Comunes

### Debugging de un request específico

1. Identificar el TraceId del request problemático:
```bash
cat controlpeso-.ndjson | jq 'select(."@l" == "Error")' | jq '."@tr"'
```

2. Ver flujo completo del request:
```bash
TRACE_ID="<trace-id>"
cat controlpeso-.ndjson | jq --arg trace "$TRACE_ID" 'select(."@tr" == $trace)' | jq -s 'sort_by(."@t")' | jq -r '.[] | "\(."@t") [\(."@l" // "Information")] \(.SourceContext): \(."@mt")"'
```

### Identificar usuarios con problemas recurrentes

```bash
# Usuarios con más errores
cat controlpeso-.ndjson | jq 'select(."@l" == "Error" and .UserId != null)' | jq -s 'group_by(.UserId) | map({UserId: .[0].UserId, ErrorCount: length}) | sort_by(.ErrorCount) | reverse'
```

### Detectar fugas de performance

```bash
# Endpoints más lentos en promedio
cat controlpeso-.ndjson | jq 'select(.DurationMs > 500)' | jq -s 'group_by(.RequestPath) | map({Path: .[0].RequestPath, AvgMs: ([.[].DurationMs] | add / length), MaxMs: ([.[].DurationMs] | max), Count: length}) | sort_by(.AvgMs) | reverse'
```

### Análisis de sesión de usuario completa

```bash
# Actividad de un usuario en orden cronológico
USER_ID="663252fd-8c18-4585-9a8f-295e59e6e72a"
cat controlpeso-.ndjson | jq --arg user "$USER_ID" 'select(.UserId == $user)' | jq -s 'sort_by(."@t")' | jq -r '.[] | "\(."@t") [\(."@l" // "Info")] \(.SourceContext | split(".") | .[-1]): \(."@mt" | gsub("{[^}]+}"; "***"))"'
```

---

## Propiedades Personalizadas Disponibles

### De negocio
- `UserId` - GUID del usuario autenticado
- `Email` - Email del usuario
- `Role` - Rol del usuario (User, Administrator)
- `WeightLogId` - ID de registro de peso
- `CircuitId` - ID de circuito Blazor Server

### De rendimiento
- `DurationMs` - Duración de request HTTP en milisegundos
- `StatusCode` - Código HTTP de respuesta
- `ElapsedMilliseconds` - Duración total de operación

### De correlación
- `@tr` / `traceId` - Trace ID (correlaciona todas las operaciones de un request)
- `@sp` - Span ID (correlaciona operaciones dentro de un trace)
- `RequestId` - ID interno de request ASP.NET Core
- `ConnectionId` - ID de conexión HTTP
- `TransportConnectionId` - ID de conexión SignalR/Blazor

### De contexto
- `SourceContext` - Namespace.Clase del logger
- `RequestPath` - Path del request HTTP
- `Method` - Método HTTP (GET, POST, etc.)
- `LogType` - Categoría de log (Business, Infrastructure, Security) - cuando se usa scope

---

## Exportar Logs para Análisis Externo

### CSV para Excel

```bash
# Exportar errores a CSV
cat controlpeso-.ndjson | jq -r 'select(."@l" == "Error") | [."@t", .UserId, .SourceContext, ."@mt"] | @csv' > errors.csv
```

### Filtrado y pipe a otros tools

```bash
# Enviar a clipboard (Windows)
cat controlpeso-.ndjson | jq 'select(."@l" == "Error")' | clip

# Grep tradicional para búsqueda rápida
grep -i "exception" controlpeso-.ndjson
```

---

## Best Practices

1. **Siempre usar correlación por TraceId** para debugging de requests completos
2. **Filtrar por SourceContext** para aislar capas de arquitectura (Application, Infrastructure, Web)
3. **Monitorear DurationMs** proactivamente para detectar degradación de performance
4. **Agregar scopes de Business/Security** en código para categorización más granular
5. **Retener logs por al menos 30 días** para análisis de tendencias

---

## Herramientas Recomendadas

| Herramienta | Uso | Link |
|-------------|-----|------|
| **Seq** | Búsqueda interactiva, dashboards | https://datalust.co/seq |
| **Grafana Loki** | Logs + métricas + traces correlacionados | https://grafana.com/loki |
| **jq** | CLI filtering y análisis ad-hoc | https://jqlang.github.io/jq/ |
| **VS Code + JSON Lines** | Visualización local | marketplace: `NDJSON` |

---

**Última actualización**: 2026-02-21  
**Versión de logging framework**: ThisCloud.Framework.Loggings.Serilog 1.0.86
