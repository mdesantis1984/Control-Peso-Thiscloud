# Secrets Management - Control Peso Thiscloud

Secrets never go into Git. Tracked files only contain placeholders (`CHANGE_ME`, `YOUR_*_HERE`); real values live in the environment of the machine that runs the application. Non-secret configuration is described in [CONFIG_MANAGEMENT.md](CONFIG_MANAGEMENT.md).

## What counts as a secret

| Secret | Variable | Required |
|--------|----------|----------|
| PostgreSQL password (local compose) | `POSTGRES_PASSWORD` | Local Docker |
| Npgsql connection string, includes the password (production) | `CONTROLPESO_DB_CONNECTION` | Production |
| Google OAuth client id | `Authentication__Google__ClientId` | Yes |
| Google OAuth client secret | `Authentication__Google__ClientSecret` | Yes |
| LinkedIn OAuth client id / secret | `Authentication__LinkedIn__ClientId` / `ClientSecret` | No (disabled in the UI) |
| Google Analytics measurement id | `GoogleAnalytics__MeasurementId` | No (not a secret, but environment specific) |

## Model

```
Git (public)                           Docker host (/opt/controlpeso)
├─ docker-compose.yml                  ├─ docker-compose.production.yml   (copied from Git)
├─ docker-compose.production.yml       ├─ .env                            (CONTROLPESO_DB_CONNECTION, APP_PORT)
├─ appsettings*.json.template          └─ docker-compose.override.yml     (OAuth secrets, mode 600)
└─ .gitignore: .env*, docker-compose.override.yml,
   appsettings.Development.json, appsettings.Production.json, .secrets.local
```

The tracked compose files deliberately omit every secret. Compose reads `.env` for `${...}` substitution and only merges the override file when it is passed explicitly in production.

## Production setup

```bash
cd /opt/controlpeso

# 1. .env (database connection string, optional APP_PORT)
cat > .env <<'EOF'
CONTROLPESO_DB_CONNECTION=Host=<db-host>;Port=5432;Database=controlpeso;Username=<role>;Password=<secret>
EOF

# 2. docker-compose.override.yml (OAuth secrets)
cat > docker-compose.override.yml <<'EOF'
services:
  controlpeso-web:
    environment:
      - Authentication__Google__ClientId=<client-id>.apps.googleusercontent.com
      - Authentication__Google__ClientSecret=<client-secret>
EOF

# 3. Owner-only permissions
chmod 600 .env docker-compose.override.yml

# 4. Both files must be ignored by Git (run in a checkout)
git check-ignore .env docker-compose.override.yml

# 5. Start, passing the override explicitly (with -f it is not loaded automatically)
docker compose -f docker-compose.production.yml -f docker-compose.override.yml up -d

# 6. Verify that the variables reached the container (prints names only, not values)
docker exec controlpeso-web env | grep -c '^Authentication__Google__'
```

A `$` inside a value must be written `$$` in `.env` and compose files.

## Local development

Pick one:

- `docker-compose.override.yml` (gitignored) next to `docker-compose.yml`, plus `POSTGRES_PASSWORD` in the environment or in a local `.env`. See [DEPLOYMENT.md](DEPLOYMENT.md).
- `appsettings.Development.json` created from the template (`pwsh scripts/setup-config.ps1`) or `dotnet user-secrets`. See [CONFIG_MANAGEMENT.md](CONFIG_MANAGEMENT.md).
- `.secrets.local` for the PowerShell deploy scripts (created from `.secrets.local.example`, gitignored).

Use different credentials per environment (a separate Google OAuth client for local and production).

## Rotation

The step-by-step procedure for every credential is in [CREDENTIAL_ROTATION_GUIDE.md](CREDENTIAL_ROTATION_GUIDE.md). Short version for the web container:

```bash
# After editing .env or docker-compose.override.yml
docker compose -f docker-compose.production.yml -f docker-compose.override.yml up -d --force-recreate controlpeso-web
curl -I https://controlpeso.thiscloud.com.ar/login
```

## Best practices

- Strong, unique passwords; rotate at least every 90 days and whenever someone with access leaves.
- Restrict the PostgreSQL port to the Docker host (firewall); SSH with keys only.
- Share secrets only through a password manager, never through chat, email or screenshots.
- Keep a record of every secret in a password manager so the host can be rebuilt.
- CI runs gitleaks on every pull request diff, and `.gitignore` covers the local files, but check `git diff --staged` before each commit anyway.

## Troubleshooting

| Symptom | Check |
|---------|-------|
| Google login fails or the provider is missing | `docker exec controlpeso-web env \| grep -c '^Authentication__Google__'` must print 2; recreate the container after editing the override |
| `/health/ready` returns 503 | `CONTROLPESO_DB_CONNECTION` (host, role, password) and network access to the database |
| `Set POSTGRES_PASSWORD` when running local compose | Export `POSTGRES_PASSWORD` or create a local `.env` |
| Password containing `$` is cut | Escape as `$$` |

## Accidental commit of a secret

1. **Revoke or rotate the secret immediately** ([CREDENTIAL_ROTATION_GUIDE.md](CREDENTIAL_ROTATION_GUIDE.md)). Treat it as compromised even if the commit is removed.
2. Remove it from the history (for example with `git filter-repo`), coordinating with the repository owner because shared branches get rewritten, and ask everyone to re-clone.
3. Invalidating sessions requires rotating the Data Protection keys (stop the container, empty the `controlpeso-keys` volume, start again); all users must sign in again.

## References

- [Docker Compose: merge files](https://docs.docker.com/compose/multiple-compose-files/merge/)
- [Docker Compose: environment variables](https://docs.docker.com/compose/how-tos/environment-variables/)
- [12-Factor App: Config](https://12factor.net/config)
- [GOOGLE_OAUTH_SETUP.md](GOOGLE_OAUTH_SETUP.md), [BACKUP_SYSTEM.md](BACKUP_SYSTEM.md)
