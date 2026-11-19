# Backup System - Control Peso Thiscloud

> Backups cover the **PostgreSQL** database only. The application container is stateless apart from logs, Data Protection keys and avatars (Docker volumes, see [infraestructura.md](infraestructura.md)).

## Overview

| Item | Value |
|------|-------|
| Where it runs | The PostgreSQL host (Proxmox CT 205), not the application container |
| Tool | `pg_dump` |
| Script | `/usr/local/bin/controlpeso-pg-backup` (lives on the database host; it is not part of this repository) |
| Schedule | Daily at 03:17 (cron on the database host) |
| Destination | `/var/backups/controlpeso` |
| Retention | 14 days |

The backup script is operational configuration of the database host. If it changes, update this document.

## Verify the backups

On the database host:

```bash
# Cron entry
crontab -l | grep controlpeso-pg-backup

# Latest files: there should be one per day for the last 14 days
ls -lh /var/backups/controlpeso | tail

# A fresh file (younger than ~26 h) and a non-trivial size are the minimum checks
find /var/backups/controlpeso -type f -mtime -1 -ls
```

## Run a backup on demand

```bash
sudo /usr/local/bin/controlpeso-pg-backup
```

## Restore

Always restore into a **new, empty database first** and check it before touching production.

1. Identify the dump format: a plain SQL dump is readable text (`head -c 200 <file>`), a custom-format dump starts with `PGDMP` and is listed with `pg_restore --list <file>`.
2. Create a scratch database and restore into it:

```bash
export PGHOST=<host> PGUSER=<role> PGPASSWORD=<secret>

createdb controlpeso_restore_check

# Custom or directory format (the file may also be compressed: decompress it first)
pg_restore --no-owner --dbname=controlpeso_restore_check <backup-file>

# Plain SQL format
psql --set ON_ERROR_STOP=1 --dbname=controlpeso_restore_check --file <backup-file>
```

3. Compare row counts of `"Users"` and `"WeightLogs"` against the expectation, and start a local container against the scratch database (`ConnectionStrings__DefaultConnection`) to check that `/health/ready` answers 200.
4. To recover production: stop the web container, restore into the real database (drop and recreate it, or restore into a fresh one and repoint `CONTROLPESO_DB_CONNECTION`), then start the container. Run `scripts/migrate-postgres.sh` afterwards if the backup predates a later migration; it is idempotent.
5. Drop the scratch database: `dropdb controlpeso_restore_check`.

## Recommendations

- **Test a restore regularly** (at least once per quarter): an untested backup is not a backup.
- Keep a copy off the database host (another machine or storage), since `/var/backups/controlpeso` sits on the same host as the data.
- Take a manual backup right before applying a schema migration in production.
- Treat backups as sensitive data: they contain user emails and weights. Restrict permissions on `/var/backups/controlpeso` and encrypt any off-site copy.
- Monitor the age of the newest file (alert if older than 26 hours).

## What is not backed up

| Data | Where | Note |
|------|-------|------|
| Avatars | volume `controlpeso-uploads` on the Docker host | Back up this volume if avatars matter; users can upload them again |
| Data Protection keys | volume `controlpeso-keys` | If lost, sessions are invalidated and users sign in again; no data loss |
| Logs | volume `controlpeso-logs` | Diagnostics only |
| Secrets | Environment of the Docker host | Keep a copy in a password manager ([SECRETS_MANAGEMENT.md](SECRETS_MANAGEMENT.md)) |
