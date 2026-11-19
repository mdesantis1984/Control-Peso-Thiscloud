# Deployment - Control Peso Thiscloud

> The application ships as a Docker image (.NET 10, non-root) and talks to an external **PostgreSQL 15** database. Production runs in a Docker container behind Cloudflare and the NPMplus reverse proxy. For the topology see [infraestructura.md](infraestructura.md); for backups see [BACKUP_SYSTEM.md](BACKUP_SYSTEM.md).

## Overview

| Item | Value |
|------|-------|
| Image | `controlpeso-web:<tag>` built locally from [`Dockerfile`](../Dockerfile) (multi-stage, `aspnet:10.0`) |
| Runtime user | `app` (UID 1654), never root |
| Listens on | `http://+:8080` inside the container |
| Writable paths | `/app/logs`, `/app/keys` (Data Protection), `/app/wwwroot/uploads` (avatars), `/app/data` |
| Health | `GET /health` (liveness, no database) and `GET /health/ready` (readiness, checks the database) |
| Compose files | `docker-compose.yml` (local, includes PostgreSQL) and `docker-compose.production.yml` (web only, external database) |

Both compose files use `image: controlpeso-web:latest`: **they never build**. Build the image first.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (build and tests)
- Docker with Compose v2
- PostgreSQL 15 reachable from the container (a role that can create tables and the `citext` extension)
- Google OAuth credentials (see [GOOGLE_OAUTH_SETUP.md](GOOGLE_OAUTH_SETUP.md))
- Domain behind a TLS-terminating proxy (production): `controlpeso.thiscloud.com.ar`

## Build the image

```bash
docker build -t controlpeso-web:latest -f Dockerfile .
```

## Versioning

The application version is never edited by hand: it comes from the git release tags.

- **Tags**: [`release.yml`](../.github/workflows/release.yml) creates the tag `vMAJOR.MINOR.PATCH` and the GitHub release when a change is merged to `main`; the bump follows Conventional Commits (`feat` = minor, `fix` = patch, a breaking change = major).
- **MinVer** (`Directory.Build.props`, tag prefix `v`) turns the nearest tag into the assembly version at build time. A commit exactly at `v9.0.0` builds as `9.0.0`; commits after it build as a SemVer pre-release such as `9.0.1-alpha.0.3`. The footer and sidebar show the informational version without the `+<commit>` metadata (`AppVersionInfo.Current`).
- **Docker**: `docker build` reads the tags from the `.git` folder of the build context (it is not copied into the final image), so build from a full clone: `git fetch --tags --unshallow` if needed. CI uses `fetch-depth: 0` for the same reason.
- **Without tag history** (shallow clone, source archive, git worktree) pass the version explicitly; it overrides MinVer:

```bash
docker build --build-arg APP_VERSION=9.0.0 -t controlpeso-web:9.0.0 -f Dockerfile .
dotnet build -p:MinVerVersionOverride=9.0.0
```

Without tags and without the override MinVer reports `0.0.0-alpha.0.N`.

## Local run (Docker Compose + PostgreSQL)

```bash
# 1. PostgreSQL password: environment variable or a local, gitignored .env file
export POSTGRES_PASSWORD='<password>'

# 2. Start the database only and wait until it is healthy
docker compose up -d postgres

# 3. Create the schema (the application never migrates at startup)
PGHOST=localhost PGDATABASE=controlpeso PGUSER=controlpeso PGPASSWORD="$POSTGRES_PASSWORD" \
  scripts/migrate-postgres.sh

# 4. Put the Google credentials in docker-compose.override.yml (gitignored), then:
docker compose up -d

# 5. Verify
docker compose ps
curl http://localhost:8080/health
curl http://localhost:8080/health/ready
```

`docker-compose.override.yml` (never committed) supplies the secrets that the tracked compose file deliberately leaves out:

```yaml
services:
  controlpeso-web:
    environment:
      - Authentication__Google__ClientId=<client-id>.apps.googleusercontent.com
      - Authentication__Google__ClientSecret=<client-secret>
      # Optional
      - GoogleAnalytics__MeasurementId=G-XXXXXXXXXX
      # Local plain-HTTP testing only
      - DISABLE_HTTPS_REDIRECTION=true
```

Open <http://localhost:8080/login> and sign in with Google. The redirect URI `http://localhost:8080/signin-google` must be registered in the OAuth client. See [SECRETS_MANAGEMENT.md](SECRETS_MANAGEMENT.md) for the full list of variables.

Local volumes: `controlpeso-pgdata`, `controlpeso-logs`, `controlpeso-keys`, `controlpeso-avatars`.

## Database migrations (PostgreSQL)

The application **never migrates the database at startup**. Schema changes are an explicit deploy step, run **before** starting the new container:

```bash
export PGHOST=<host> PGDATABASE=controlpeso PGUSER=<role> PGPASSWORD=<secret>   # secret from your vault, never committed
scripts/migrate-postgres.sh --dry-run   # generate and print the idempotent SQL only
scripts/migrate-postgres.sh             # generate and apply it with psql
```

- The script installs `dotnet-ef` (default 10.0.3, override with `EF_VERSION`) under `EF_TOOL_PATH` (default `${TMPDIR:-/tmp}/controlpeso-ef-tools`), runs `dotnet ef migrations script --idempotent` and applies the result with `psql` (`ON_ERROR_STOP=1`).
- It is idempotent: running it again on an up-to-date database changes nothing.
- The role needs permission to create tables and the `citext` extension.
- Details of the schema and how to add a migration: [DATABASE.md](DATABASE.md).

## Production deployment

Production runs `docker-compose.production.yml` from `/opt/controlpeso` on the Docker host (Proxmox CT 100). The database lives on a separate host (CT 205) and is reached through `CONTROLPESO_DB_CONNECTION`.

### Required configuration

Provide these through a gitignored `.env` next to the compose file (for `${...}` substitution) and a gitignored `docker-compose.override.yml` (for container environment variables). Never put values in a tracked file. With an explicit `-f docker-compose.production.yml`, Compose does **not** load the override automatically: pass `-f docker-compose.override.yml` as well.

| Variable | Purpose |
|----------|---------|
| `CONTROLPESO_DB_CONNECTION` | Npgsql connection string: `Host=<db-host>;Port=5432;Database=controlpeso;Username=<role>;Password=<secret>` |
| `Authentication__Google__ClientId` / `Authentication__Google__ClientSecret` | Google OAuth client (the tracked compose comments call them `GOOGLE_CLIENT_ID` / `GOOGLE_CLIENT_SECRET`; map them in the override file to the `Authentication__Google__*` keys the application reads) |
| `APP_PORT` | Optional host port mapped to container port 8080 (default `8080`) |
| `GoogleAnalytics__MeasurementId` | Optional; Google Analytics is only enabled when a real id is set |

The tracked file already sets `ASPNETCORE_ENVIRONMENT=Production`, `AllowedHosts=controlpeso.thiscloud.com.ar`, `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`, `DataProtection__Path=/app/keys`, logging to `/app/logs` and the three volumes (`controlpeso-logs`, `controlpeso-keys`, `controlpeso-uploads`).

### Release procedure

Test locally first; deploy only code that passed CI.

```bash
# 1. Build and test
dotnet build -c Release
dotnet test -c Release

# 2. Build and export the image
docker build -t controlpeso-web:<tag> -t controlpeso-web:latest -f Dockerfile .
docker save controlpeso-web:latest -o controlpeso-web_latest.tar

# 3. Transfer and load on the Docker host
scp controlpeso-web_latest.tar <user>@<docker-host>:/tmp/
ssh <user>@<docker-host> "docker load -i /tmp/controlpeso-web_latest.tar"

# 4. Apply schema changes first (from a machine that reaches the database)
scripts/migrate-postgres.sh

# 5. Restart the stack on the host
ssh <user>@<docker-host> "cd /opt/controlpeso && docker compose -f docker-compose.production.yml -f docker-compose.override.yml up -d"

# 6. Verify (wait about 20-60 s: the healthcheck start period is 120 s)
ssh <user>@<docker-host> "cd /opt/controlpeso && docker compose -f docker-compose.production.yml ps && docker logs controlpeso-web --tail=50"
curl https://controlpeso.thiscloud.com.ar/health
curl https://controlpeso.thiscloud.com.ar/health/ready
curl -I https://controlpeso.thiscloud.com.ar/robots.txt
```

Checklist after the deploy: container `healthy`, `/health` and `/health/ready` answer 200, logs without errors, `robots.txt` and `sitemap.xml` respond, Google login works.

> The PowerShell scripts under `scripts/` (`deploy-production.ps1`, `deploy-docker.ps1`) predate the PostgreSQL migration and the non-root image. Review them before use; the commands above are the reference procedure.

### Reverse proxy (Cloudflare + NPMplus)

TLS terminates at Cloudflare and NPMplus; the container only speaks HTTP on 8080.

- Proxy host `controlpeso.thiscloud.com.ar` → the Docker host on `APP_PORT`, scheme `http`.
- **WebSockets support must be enabled**: Blazor Server needs SignalR. Without it the page loads but is not interactive.
- Force SSL and HSTS at the proxy.
- The application trusts `X-Forwarded-*` only from the networks in `ForwardedHeaders:KnownNetworks` / `ForwardedHeaders:KnownProxies` (defaults: loopback, `172.16.0.0/12`, `192.168.178.0/24`). If the proxy sits in another network, set those keys, otherwise OAuth builds `http://` redirect URIs.
- Google OAuth redirect URI: `https://controlpeso.thiscloud.com.ar/signin-google`.

### Rollback

1. Re-tag and start the previous image (`docker tag controlpeso-web:<previous> controlpeso-web:latest`, then `docker compose -f docker-compose.production.yml -f docker-compose.override.yml up -d`).
2. Migrations are additive and idempotent; if a schema change must be undone, restore from the latest backup ([BACKUP_SYSTEM.md](BACKUP_SYSTEM.md)) or write a corrective migration.

## Logs

Structured NDJSON in `/app/logs/controlpeso-YYYYMMDD.ndjson` (volume `controlpeso-logs`), plus the console in local compose. Errors are recorded only in these logs; there are no external alerts.

```bash
docker logs -f controlpeso-web --tail=50
docker exec controlpeso-web sh -c 'ls -lh /app/logs'
```

Query examples: [LOGGING_QUERY_GUIDE.md](LOGGING_QUERY_GUIDE.md) and [soporte.md](soporte.md).

## Troubleshooting

| Symptom | Check |
|---------|-------|
| `502 Bad Gateway` from the proxy | Wrong forward port (must equal `APP_PORT`), container not healthy |
| Page loads but is not interactive, `WebSocket connection failed` | Enable WebSockets in the proxy host |
| `redirect_uri_mismatch` from Google | The exact URI (`.../signin-google`) is missing in the OAuth client; changes take a few minutes |
| Login loops or `http://` redirect URIs | `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` and the proxy network in `ForwardedHeaders:KnownNetworks` |
| `/health/ready` returns 503 | Database unreachable or bad `CONTROLPESO_DB_CONNECTION`; `/health` stays 200 |
| Relation/table does not exist | Schema not created: run `scripts/migrate-postgres.sh` |
| Users are signed out after every deploy | `/app/keys` is not a persistent volume (Data Protection keys are lost) |
| Compose refuses to start with `Set POSTGRES_PASSWORD` | Define `POSTGRES_PASSWORD` in the environment or a local `.env` |
| Avatar upload fails with permission errors | `/app/wwwroot/uploads` volume must be writable by UID 1654 |

## Security checklist

- [x] HTTPS enforced (proxy + `UseHttpsRedirection` + HSTS in Production)
- [x] Container runs as non-root (checked by the CI Docker smoke test)
- [x] Secrets only in environment variables or gitignored files
- [x] Security headers (CSP, `X-Frame-Options`, ...) from `SecurityHeadersMiddleware`
- [x] Rate limiting on the OAuth endpoints

Details: [SECURITY.md](SECURITY.md).
