# Infraestructura - Control Peso Thiscloud

> Topología y operación de producción. Los procedimientos paso a paso están en [DEPLOYMENT.md](DEPLOYMENT.md); la arquitectura de código en [ARCHITECTURE.md](ARCHITECTURE.md).

## 1. Topología de producción

```
Usuario ──HTTPS──▶ Cloudflare (DNS, proxy, Web Analytics)
                       │
                       ▼
                  NPMplus (proxy inverso, TLS, WebSockets)
                       │ HTTP
                       ▼
        Proxmox CT 100 · Docker · /opt/controlpeso
        └─ controlpeso-web (puerto 8080 del contenedor → APP_PORT del host)
                       │ Npgsql (TCP 5432)
                       ▼
        Proxmox CT 205 · PostgreSQL 15 · base `controlpeso`
        └─ copias diarias con pg_dump (ver BACKUP_SYSTEM.md)
```

| Componente | Detalle |
|------------|---------|
| Dominio | `controlpeso.thiscloud.com.ar` |
| Aplicación | Imagen `controlpeso-web`, .NET 10, Blazor Server |
| Orquestación | `docker-compose.production.yml` en `/opt/controlpeso` (solo la aplicación; la base de datos es externa) |
| Base de datos | PostgreSQL 15 en un host separado, conexión por `CONTROLPESO_DB_CONNECTION` |
| Proxy | Cloudflare delante de NPMplus. NPMplus debe tener **WebSockets habilitados** (SignalR de Blazor Server) |
| Autenticación | Google OAuth 2.0; URI de redirección `https://controlpeso.thiscloud.com.ar/signin-google` |

## 2. Contenedor

La imagen se construye con el [`Dockerfile`](../Dockerfile) multi-stage (SDK 10.0 → `aspnet:10.0`):

- Se ejecuta como usuario **no root** `app` (UID 1654). El workflow de CI lo verifica en cada cambio.
- Escucha en `http://+:8080`; `ASPNETCORE_ENVIRONMENT=Production` por defecto.
- Incluye `curl` para el `HEALTHCHECK`, que consulta `/health` cada 30 s (con `X-Forwarded-Proto: https` para evitar la redirección a HTTPS).
- Los archivos estáticos `robots.txt` y `sitemap.xml` se eliminan de la imagen: los sirven Minimal APIs dinámicas.

### Puertos

| Entorno | Puerto del host | Puerto del contenedor | Acceso |
|---------|-----------------|-----------------------|--------|
| Local (`docker-compose.yml`) | 8080 | 8080 | <http://localhost:8080> |
| Producción (`docker-compose.production.yml`) | `APP_PORT` (por defecto 8080) | 8080 | NPMplus → `http://<host-docker>:APP_PORT` |

Para cambiar el puerto externo basta definir `APP_PORT` en el entorno del compose y actualizar el puerto de destino en NPMplus. No se necesita ninguna otra configuración de puertos en la aplicación.

### Volúmenes

| Volumen (producción) | Ruta en el contenedor | Contenido |
|----------------------|-----------------------|-----------|
| `controlpeso-logs` | `/app/logs` | Logs NDJSON (`controlpeso-YYYYMMDD.ndjson`, rotación de 10 MB, 60 archivos) |
| `controlpeso-keys` | `/app/keys` | Claves de Data Protection (si se pierden, se invalidan las sesiones y los tokens antiforgery) |
| `controlpeso-uploads` | `/app/wwwroot/uploads` | Avatares de usuarios |

En local el compose añade `controlpeso-pgdata` (datos de PostgreSQL) y llama `controlpeso-avatars` al volumen de avatares. Los volúmenes deben ser escribibles por el UID 1654.

### Variables relevantes

Valores no secretos ya definidos en el compose de producción: `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS`, `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`, `AllowedHosts=controlpeso.thiscloud.com.ar`, `DataProtection__Path=/app/keys` y la sección `ThisCloud__Loggings__*` (nivel `Warning`, salida a archivo, sin consola, redacción activa).

Secretos y valores del entorno (nunca en Git): ver [SECRETS_MANAGEMENT.md](SECRETS_MANAGEMENT.md).

Si el proxy no está en las redes por defecto (loopback, `172.16.0.0/12`, `192.168.178.0/24`), configurar `ForwardedHeaders:KnownNetworks` o `ForwardedHeaders:KnownProxies`; de lo contrario OAuth genera URIs `http://`.

## 3. Endpoints operativos

| Endpoint | Uso |
|----------|-----|
| `GET /health` | Liveness: no consulta la base de datos. Lo usan el `HEALTHCHECK` de Docker y los monitores de disponibilidad |
| `GET /health/ready` | Readiness: devuelve 503 si la base de datos no responde |
| `GET /robots.txt`, `GET /sitemap.xml` | SEO dinámico, ver [SEO_INFRASTRUCTURE.md](SEO_INFRASTRUCTURE.md) |

Ambos endpoints de salud son anónimos y están excluidos del rate limiting.

## 4. Base de datos y copias de seguridad

- Esquema por migraciones EF Core; se aplica con `scripts/migrate-postgres.sh` **antes** de levantar la nueva versión ([DATABASE.md](DATABASE.md)).
- Copias diarias con `pg_dump` en el host de la base de datos, 14 días de retención ([BACKUP_SYSTEM.md](BACKUP_SYSTEM.md)).

## 5. Logs y diagnóstico

Los errores se registran únicamente en los logs estructurados (Serilog, NDJSON) de `/app/logs`; no existen alertas externas. Comandos de consulta en [soporte.md](soporte.md) y [LOGGING_QUERY_GUIDE.md](LOGGING_QUERY_GUIDE.md).

## 6. Analítica

- **Cloudflare Web Analytics**: se activa desde Cloudflare, ver [CLOUDFLARE_ANALYTICS.md](CLOUDFLARE_ANALYTICS.md).
- **Google Analytics 4**: opcional; solo se carga (y la CSP solo lo permite) cuando `GoogleAnalytics:MeasurementId` tiene un id real.

## 7. Integración continua

`.github/workflows/ci.yml`: restore, build, tests, revisión de paquetes NuGet vulnerables, gitleaks sobre el diff del PR y prueba de humo de la imagen (usuario no root + `/health`). `.github/workflows/release.yml` crea un tag `vX.Y.Z` y un GitHub Release en cada push a `main`.

## 8. Scripts

| Script | Estado |
|--------|--------|
| `scripts/migrate-postgres.sh` | Vigente: genera y aplica el SQL idempotente de las migraciones |
| `scripts/setup-config.ps1` | Vigente: crea los `appsettings.*.json` locales desde las plantillas |
| `scripts/clearObjBinVs.ps1` | Vigente: limpia `bin/`, `obj/` y `.vs/` |
| `scripts/deploy-production.ps1`, `scripts/deploy-docker.ps1` | Anteriores a PostgreSQL y a la imagen no root: revisar antes de usarlos; el procedimiento de referencia está en [DEPLOYMENT.md](DEPLOYMENT.md) |
