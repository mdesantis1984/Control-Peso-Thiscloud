# Integración de ThisCloud.Framework - ControlPeso.Thiscloud

> **Verificado contra el código**: 2026-11-19 (versión 1.3.0)  
> **Framework**: https://github.com/mdesantis1984/ThisCloud.Framework (licencia ISC)

ControlPeso usa **solo** los paquetes de logging del framework. El resto de sus componentes (`Contracts`, `Web`, `Loggings.Admin`) no se usan: la aplicación es Blazor Server y no expone una API REST propia, y los endpoints de administración de logs en runtime no están habilitados.

---

## Paquetes en uso

Versiones centralizadas en `Directory.Packages.props`, referenciadas desde `src/ControlPeso.Web/ControlPeso.Web.csproj`:

| Paquete | Versión | Uso |
|---------|---------|-----|
| `ThisCloud.Framework.Loggings.Abstractions` | 1.0.94 | Contratos de logging |
| `ThisCloud.Framework.Loggings.Serilog` | 1.0.94 | Serilog con sinks Console + File (NDJSON), enriquecimiento, redacción y correlación |

El framework apunta a .NET 10, igual que la aplicación.

## Qué aporta

- **Redacción automática** de secretos y datos personales en los logs (`Redaction:Enabled`).
- **Correlation ID** (`X-Correlation-Id`, generado si falta) y enriquecimiento con servicio, entorno, máquina y trace/span.
- **Archivo NDJSON con rotación por tamaño** (10 MB) y retención configurable.
- **Fail-fast**: una configuración inválida detiene el arranque en producción.

## Cableado (`src/ControlPeso.Web/Program.cs`)

```csharp
builder.Host.UseThisCloudFrameworkSerilog(
    builder.Configuration,
    serviceName: "ControlPeso.Thiscloud");

builder.Services.AddThisCloudFrameworkLoggings(
    builder.Configuration,
    serviceName: "ControlPeso.Thiscloud");
```

Los servicios obtienen `ILogger<T>` por inyección; los scopes `BeginBusinessScope`, `BeginInfrastructureScope` y `BeginSecurityScope` viven en `src/ControlPeso.Application/Logging/LoggingExtensions.cs`.

## Configuración

Sección `ThisCloud:Loggings` de `appsettings.json` (valores por defecto del repositorio):

```json
"ThisCloud": {
  "Loggings": {
    "IsEnabled": true,
    "MinimumLevel": "Information",
    "Console": { "Enabled": true },
    "File": {
      "Enabled": true,
      "Path": "logs/controlpeso-.ndjson",
      "RollingFileSizeMb": 10,
      "RetainedFileCountLimit": 30,
      "UseCompactJson": true
    },
    "Redaction": { "Enabled": true },
    "Correlation": { "HeaderName": "X-Correlation-Id", "GenerateIfMissing": true }
  }
}
```

En producción (`docker-compose.production.yml`) se sobrescribe con variables de entorno `ThisCloud__Loggings__*`:

| Variable | Valor en producción |
|----------|--------------------|
| `MinimumLevel` | `Warning` |
| `Console__Enabled` | `false` |
| `File__Path` | `/app/logs/controlpeso-.ndjson` (volumen `controlpeso-logs`) |
| `File__RetainedFileCountLimit` | `60` |
| `Redaction__Enabled` | `true` |

**Los errores de la aplicación se registran solo en estos archivos**: no existe un canal de alertas externo (el bot de Telegram fue eliminado).

## Verificación rápida

1. Arrancar la aplicación y comprobar que existe `logs/controlpeso-<fecha>.ndjson`.
2. Provocar una operación con un dato sensible (por ejemplo un encabezado `Authorization`) y confirmar que aparece redactado.
3. Comprobar que las entradas de un mismo request comparten `@tr`/`traceId`.

Consultas y ejemplos: [LOGGING_QUERY_GUIDE.md](./LOGGING_QUERY_GUIDE.md). Reglas de uso: [LOGGING_BEST_PRACTICES.md](./LOGGING_BEST_PRACTICES.md). Arquitectura general: [ARCHITECTURE.md](./ARCHITECTURE.md).
