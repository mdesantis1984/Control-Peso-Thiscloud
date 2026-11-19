# Soporte y resolución de problemas - Control Peso Thiscloud

Guía operativa para diagnosticar la aplicación en producción y en desarrollo. Los detalles de despliegue están en [DEPLOYMENT.md](DEPLOYMENT.md), la topología en [infraestructura.md](infraestructura.md) y las buenas prácticas de logging en [LOGGING_BEST_PRACTICES.md](LOGGING_BEST_PRACTICES.md).

## Contenido

1. [Diagnóstico rápido](#1-diagnóstico-rápido)
2. [Logs](#2-logs)
3. [Errores y alertas](#3-errores-y-alertas)
4. [Blazor: notas técnicas](#4-blazor-notas-técnicas)
5. [Comandos de referencia](#5-comandos-de-referencia)
6. [Scripts del repositorio](#6-scripts-del-repositorio)

---

## 1. Diagnóstico rápido

```bash
# ¿La aplicación responde? (liveness: no consulta la base de datos)
curl -i https://controlpeso.thiscloud.com.ar/health

# ¿Puede hablar con PostgreSQL? (readiness: 503 si la base no responde)
curl -i https://controlpeso.thiscloud.com.ar/health/ready

# Estado del contenedor y últimos logs (en el host Docker, /opt/controlpeso)
docker compose -f docker-compose.production.yml ps
docker logs controlpeso-web --tail=100
```

| Síntoma | Causa probable y qué revisar |
|---------|------------------------------|
| `/health` 200 y `/health/ready` 503 | Base de datos inaccesible o `CONTROLPESO_DB_CONNECTION` incorrecta; revisar el host de PostgreSQL y la red |
| Error de "relation ... does not exist" | El esquema no está creado: ejecutar `scripts/migrate-postgres.sh` (la aplicación no migra al iniciar) |
| 502 desde el proxy | Contenedor caído o puerto de NPMplus distinto de `APP_PORT` |
| La página carga pero no responde a clics | WebSockets deshabilitados en NPMplus (Blazor Server usa SignalR) |
| `redirect_uri_mismatch` al iniciar sesión | URI no registrada en Google Cloud ([GOOGLE_OAUTH_SETUP.md](GOOGLE_OAUTH_SETUP.md)) o cabeceras `X-Forwarded-*` no aceptadas ([DEPLOYMENT.md](DEPLOYMENT.md)) |
| Los usuarios pierden la sesión en cada despliegue | `/app/keys` no es un volumen persistente |
| Error 429 al iniciar sesión | Rate limiting del login (5 solicitudes por minuto por IP) |
| El avatar no se guarda | El volumen `/app/wwwroot/uploads` no es escribible por el UID 1654 |
| El contenedor reinicia al arrancar | `docker logs controlpeso-web`; revisar configuración y permisos de `/app/logs` y `/app/keys` |
| Un usuario no ve el panel de administración | Su `"Users"."Role"` debe ser 1 (Administrator); ver [DATABASE.md](DATABASE.md) |

---

## 2. Logs

- **Producción**: `/app/logs/controlpeso-YYYYMMDD.ndjson` dentro del contenedor (volumen `controlpeso-logs`).
- **Desarrollo con `dotnet run`**: `src/ControlPeso.Web/logs/controlpeso-YYYYMMDD.ndjson`.
- **Docker local**: además de los archivos, la consola (`docker logs controlpeso-web`).
- **Formato**: NDJSON compacto (una línea JSON por evento) con redacción de secretos y `X-Correlation-Id`.
- En producción el nivel mínimo es `Warning` y no hay salida por consola: para investigar un incidente hay que mirar los archivos.

Campos principales de cada línea: `@t` (fecha UTC), `@mt` (plantilla del mensaje), `@l` (nivel; ausente si es `Information`), `@x` (excepción), `@tr` (trace id), `RequestPath`, `UserId`, `SourceContext`.

```bash
# Listar los archivos y elegir el del día
docker exec controlpeso-web sh -c 'ls -lh /app/logs'

# Errores y warnings del día
docker exec controlpeso-web sh -c 'cat /app/logs/controlpeso-$(date +%Y%m%d).ndjson' \
  | jq -c 'select(."@l" == "Error" or ."@l" == "Warning" or ."@l" == "Fatal")'

# Un request completo (el trace id aparece en /error?traceId=... y en cada línea del request)
docker exec controlpeso-web sh -c 'cat /app/logs/*.ndjson' | jq -c 'select(."@tr" == "<trace-id>")'

# Un usuario concreto
docker exec controlpeso-web sh -c 'cat /app/logs/*.ndjson' | jq -c 'select(.UserId == "<user-id>")'
```

Más consultas (rendimiento, rangos de tiempo, Seq): [LOGGING_QUERY_GUIDE.md](LOGGING_QUERY_GUIDE.md). La integración con `ThisCloud.Framework.Loggings` está en [THISCLOUD_FRAMEWORK_INTEGRATION.md](THISCLOUD_FRAMEWORK_INTEGRATION.md).

---

## 3. Errores y alertas

Los errores se registran únicamente en los logs estructurados; no existen alertas externas (el bot de Telegram se eliminó). Ante una excepción no controlada, `GlobalExceptionMiddleware` la registra y redirige al usuario a `/error?traceId=...`; el usuario puede informar ese identificador y con él se encuentra el detalle en los logs (`@tr`).

Para enterarse de caídas conviene un monitor externo que consulte `/health` y `/health/ready`.

---

## 4. Blazor: notas técnicas

### 4.1. Prerendering y JS interop

Detalle ampliado en [technical/BLAZOR_PRERENDERING_JS_INTEROP.md](technical/BLAZOR_PRERENDERING_JS_INTEROP.md).

Blazor Server prerenderiza los componentes antes de abrir el circuito SignalR; durante el prerender no hay JavaScript ni `IJSRuntime` operativo. Síntoma: `InvalidOperationException: JavaScript interop calls cannot be issued during server-side prerendering`.

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        // Aquí ya es seguro usar JS interop
        var value = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "key");
        StateHasChanged();
    }
}
```

Para leer o escribir en el navegador no se llama a `localStorage` directamente: se inyecta `IStorageService`, cuya implementación por defecto es `LocalStorageService` (ver `src/ControlPeso.Web/Services/Storage/README.md`).

### 4.2. Estado compartido entre componentes

Detalle ampliado en [technical/UNIT_SYSTEM_GLOBAL_STATE_MANAGEMENT.md](technical/UNIT_SYSTEM_GLOBAL_STATE_MANAGEMENT.md).

`UserStateService` (servicio con ámbito de circuito) publica eventos que los componentes suscriben: `UserProfileUpdated`, `UserThemeUpdated` y `UserUnitSystemUpdated`. Cuando el perfil cambia el sistema de unidades o el tema, Dashboard, Historial y Tendencias se actualizan sin recargar la página.

- Suscribirse en `OnInitialized` y **desuscribirse en `Dispose`** para evitar fugas de memoria y llamadas a componentes ya descartados.
- Llamar a `await InvokeAsync(StateHasChanged)` desde el manejador del evento, porque puede ejecutarse fuera del contexto de sincronización del componente.
- El tema se guarda en la base de datos para usuarios autenticados y en `localStorage` para anónimos (`ThemeService`).

### 4.3. Concurrencia y base de datos

Blazor Server ejecuta varios componentes a la vez dentro de un circuito, por lo que un `DbContext` compartido no es seguro. Los servicios crean contextos de corta vida desde `IDbContextFactory` y usan `AsNoTracking()` en las lecturas.

---

## 5. Comandos de referencia

### 5.1. Docker

```bash
# Construir la imagen y exportarla para el host de producción
docker build -t controlpeso-web:latest -f Dockerfile .
docker save controlpeso-web:latest -o controlpeso-web_latest.tar

# Local
docker compose up -d postgres        # solo la base de datos
docker compose up -d                 # base de datos y aplicación
docker compose down                  # detener (los volúmenes se conservan)

# Estado, logs y salud
docker compose ps
docker logs -f controlpeso-web --tail=50
docker inspect --format '{{json .State.Health}}' controlpeso-web

# Shell dentro del contenedor (la imagen es aspnet:10.0, tiene sh)
docker exec -it controlpeso-web sh
```

Limpieza: usar `docker image prune` y `docker container prune` solo después de confirmar qué se elimina; `docker volume prune` y `docker system prune --volumes` pueden borrar datos de PostgreSQL y las claves de Data Protection, por lo que no deben ejecutarse en producción.

### 5.2. Git (Git Flow simplificado)

```bash
# Rama de trabajo desde develop
git switch develop && git pull
git switch -c feature/<nombre>

# Trabajo y commits (Conventional Commits)
git add <archivos> && git commit -m "feat(scope): descripción"

# Subir la rama y abrir un pull request hacia develop
git push -u origin feature/<nombre>

# Resolver conflictos con develop
git fetch origin && git merge origin/develop
```

El workflow `release.yml` crea el tag `vX.Y.Z` en cada push a `main`; los tags no se crean a mano.

### 5.3. Base de datos y migraciones

```bash
# Aplicar el esquema (genera el SQL idempotente de las migraciones y lo ejecuta con psql)
export PGHOST=<host> PGDATABASE=controlpeso PGUSER=<rol> PGPASSWORD=<secreto>
scripts/migrate-postgres.sh --dry-run
scripts/migrate-postgres.sh

# Crear una migración nueva (el esquema se define en código; ver DATABASE.md)
dotnet ef migrations add <Nombre> --project src/ControlPeso.Infrastructure
```

Ver [DATABASE.md](DATABASE.md) para el procedimiento completo y [BACKUP_SYSTEM.md](BACKUP_SYSTEM.md) para copias y restauración.

---

## 6. Scripts del repositorio

| Script | Para qué sirve | Estado |
|--------|----------------|--------|
| `scripts/migrate-postgres.sh` | Genera y aplica las migraciones en PostgreSQL | Vigente |
| `scripts/setup-config.ps1` | Crea `appsettings.Development.json` y `appsettings.Production.json` desde las plantillas sin sobrescribir | Vigente |
| `scripts/clearObjBinVs.ps1` | Elimina `bin/`, `obj/` y `.vs/` ante un build corrupto | Vigente |
| `scripts/deploy-production.ps1`, `scripts/deploy-docker.ps1`, `scripts/Deploy-Docker-Local.ps1` | Despliegue con PowerShell | Anteriores a PostgreSQL y a la imagen no root; revisar antes de usar. Referencia actual: [DEPLOYMENT.md](DEPLOYMENT.md) |
| `scripts/init-database.sql`, `scripts/fix-database.sql`, `scripts/init-sqlserver.sql`, `scripts/migrate-to-sqlserver.ps1` | Heredados de SQL Server/SQLite | Obsoletos, no usar |

---

## Referencias

- [infraestructura.md](infraestructura.md), [DEPLOYMENT.md](DEPLOYMENT.md), [SECURITY.md](SECURITY.md)
- [documentacionfuncional.md](documentacionfuncional.md)
- [.github/copilot-instructions.md](../.github/copilot-instructions.md)
