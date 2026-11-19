# Gestión de configuración - Control Peso Thiscloud

La configuración se reparte entre un archivo base versionado, plantillas con marcadores de posición y archivos locales que **no** se versionan. Los secretos nunca entran al repositorio.

## Orden de precedencia

ASP.NET Core aplica, de menor a mayor prioridad:

1. `appsettings.json` (versionado, sin secretos)
2. `appsettings.{Environment}.json` (local, ignorado por Git)
3. User Secrets (solo en `Development`)
4. Variables de entorno (por ejemplo `ConnectionStrings__DefaultConnection`; el doble guion bajo equivale a `:`)

En Docker y en producción se usan variables de entorno (ver [SECRETS_MANAGEMENT.md](SECRETS_MANAGEMENT.md)).

## Archivos

Todos bajo `src/ControlPeso.Web/`:

| Archivo | ¿Versionado? | Contenido |
|---------|--------------|-----------|
| `appsettings.json` | Sí | Valores base sin secretos: cadena de conexión local con `CHANGE_ME_LOCAL`, `App:BaseUrl`, logging (`ThisCloud:Loggings`), `ForwardedHeaders`, `PhotoStorage`, claves de OAuth y Google Analytics en `null` |
| `appsettings.Development.json.template` | Sí | Plantilla de desarrollo (PostgreSQL local y credenciales de Google con marcadores) |
| `appsettings.Production.json.template` | Sí | Plantilla de producción con marcadores `CHANGE_ME` / `YOUR_*_HERE` |
| `appsettings.Development.json.example` | Sí | Ejemplo mínimo de las claves de OAuth |
| `appsettings.Development.json` | **No** (`.gitignore`) | Tu configuración local real |
| `appsettings.Production.json` | **No** (`.gitignore`) | Configuración real de producción, si se usa un archivo en lugar de variables de entorno |

## Primera configuración

```bash
# Desde la raíz del repositorio. Copia las plantillas si los archivos reales no existen
# (no sobrescribe archivos existentes)
pwsh scripts/setup-config.ps1
```

Sin PowerShell, copiar a mano `appsettings.Development.json.template` a `appsettings.Development.json`.

Luego reemplazar en `appsettings.Development.json` los marcadores por valores reales:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=controlpeso;Username=controlpeso;Password=<contraseña>"
  },
  "Authentication": {
    "Google": {
      "ClientId": "<client-id>.apps.googleusercontent.com",
      "ClientSecret": "<client-secret>"
    }
  }
}
```

La base de datos local se levanta con `docker compose up -d postgres` y el esquema se crea con `scripts/migrate-postgres.sh` (ver [DEPLOYMENT.md](DEPLOYMENT.md)). Cómo obtener las credenciales de Google: [GOOGLE_OAUTH_SETUP.md](GOOGLE_OAUTH_SETUP.md).

### Alternativa: User Secrets

```bash
cd src/ControlPeso.Web
dotnet user-secrets set "Authentication:Google:ClientId" "<client-id>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<client-secret>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=controlpeso;Username=controlpeso;Password=<contraseña>"
```

## Claves principales

| Clave | Uso |
|-------|-----|
| `ConnectionStrings:DefaultConnection` | Cadena de conexión Npgsql de PostgreSQL |
| `Authentication:Google:ClientId` / `ClientSecret` | Google OAuth. El proveedor solo se registra si `ClientId` no está vacío |
| `Authentication:LinkedIn:ClientId` / `ClientSecret` | Opcional y desactivado por defecto; la interfaz no ofrece este inicio de sesión |
| `GoogleAnalytics:MeasurementId` | Opcional. Solo se activa con un id real (`G-...`); los marcadores `YOUR_*` se ignoran |
| `App:BaseUrl` | URL pública de la aplicación |
| `DataProtection:Path` | Directorio de las claves de Data Protection (`/app/keys` en Docker) |
| `ForwardedHeaders:KnownNetworks` / `KnownProxies` | Proxies de confianza para `X-Forwarded-*` |
| `ThisCloud:Loggings:*` | Logging estructurado (archivo NDJSON, redacción, correlación) |
| `PhotoStorage:WebRootPath` / `AvatarsFolder` | Ubicación de los avatares (`uploads/avatars` bajo `wwwroot`) |
| `DISABLE_HTTPS_REDIRECTION` | `true` solo para pruebas locales en Docker sin TLS |

## Qué se puede commitear

- Sí: `appsettings.json`, `*.template`, `*.example`, `scripts/setup-config.ps1`.
- No: `appsettings.Development.json`, `appsettings.Production.json`, `docker-compose.override.yml`, archivos `.env` locales y cualquier archivo con valores reales.

Antes de cada commit, revisar `git diff --staged` en busca de contraseñas, `ClientSecret` o tokens. El workflow de CI ejecuta además gitleaks sobre el diff de cada pull request.

> **Imágenes Docker**: el `Dockerfile` copia todo el contexto de build y `.dockerignore` no excluye los `appsettings.*.json` locales. Construir las imágenes de producción desde un checkout limpio (o desde CI) para no incluir archivos locales con secretos.

## Cómo actualizar las plantillas

1. Agregar la nueva clave con un marcador (`YOUR_*_HERE` o `CHANGE_ME`) a la plantilla correspondiente y, si no es un secreto, un valor por defecto en `appsettings.json`.
2. Documentar la clave en este archivo.
3. Avisar al equipo: cada persona agrega el valor real en su archivo local (el script no sobrescribe archivos existentes).

## Problemas frecuentes

| Problema | Causa y solución |
|----------|------------------|
| `Connection string 'DefaultConnection' not found` | Falta la clave. Definirla en el archivo local o como variable de entorno |
| No aparece el botón de Google o el login falla | `Authentication:Google:ClientId` vacío: el proveedor no se registra |
| Se perdió el archivo local tras cambiar de rama | Los archivos ignorados no los toca Git; si se borraron, volver a ejecutar `scripts/setup-config.ps1` y completar los valores |
| La aplicación no conecta a la base de datos | Verificar que PostgreSQL esté levantado y que el esquema exista (`scripts/migrate-postgres.sh`) |

## Si se commitearon secretos por error

1. Rotar los secretos de inmediato ([CREDENTIAL_ROTATION_GUIDE.md](CREDENTIAL_ROTATION_GUIDE.md)).
2. Quitar el secreto del historial de Git (por ejemplo con `git filter-repo`) y coordinar con el propietario del repositorio antes de reescribir ramas compartidas.
3. Avisar a quienes tengan clones para que se vuelvan a sincronizar.
