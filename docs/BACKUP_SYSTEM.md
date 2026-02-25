# SQL Server Backup System Documentation

## Overview

Sistema automático de backups para SQL Server Express con:
- ✅ Backups diarios automáticos (2 AM)
- ✅ Rotación de 30 días (mantiene backups de último mes)
- ✅ Verificación de integridad (RESTORE VERIFYONLY)
- ✅ Compresión nativa de SQL Server
- ✅ Checksum para detectar corrupción
- ✅ Logs detallados de cada backup
- ✅ Scripts de backup on-demand y restore

## Architecture

```
┌─────────────────────────────────────────────────┐
│  controlpeso-sqlserver container                │
│                                                  │
│  ┌───────────────────────────────────────────┐  │
│  │  cron (daily at 2 AM)                     │  │
│  │    ↓                                       │  │
│  │  /opt/scripts/backup-sqlserver.sh         │  │
│  │    ↓                                       │  │
│  │  SQL Server BACKUP DATABASE with:         │  │
│  │  - COMPRESSION                              │  │
│  │  - CHECKSUM                                 │  │
│  │    ↓                                       │  │
│  │  RESTORE VERIFYONLY (integrity check)     │  │
│  │    ↓                                       │  │
│  │  Rotate old backups (delete > 30 days)    │  │
│  │    ↓                                       │  │
│  │  /var/opt/mssql/backups/                  │  │
│  │    - ControlPeso_YYYYMMDD_HHMMSS.bak      │  │
│  │    - backup.log                            │  │
│  │    - backup-cron.log                       │  │
│  └───────────────────────────────────────────┘  │
│                    ↓                             │
│            Bind Mount to Host                   │
└─────────────────────────────────────────────────┘
                     ↓
           /mnt/backups/controlpeso-sqlserver/
           (External volume for safety)
```

## Setup

### 1. Create Backup Directory on Host

Antes de desplegar, crear el directorio de backups en el servidor:

```bash
# SSH al servidor: ssh user@YOUR_SERVER_IP
sudo mkdir -p /mnt/backups/controlpeso-sqlserver
sudo chown -R 10001:0 /mnt/backups/controlpeso-sqlserver
sudo chmod -R 775 /mnt/backups/controlpeso-sqlserver
```

**Nota:** UID `10001` es el usuario `mssql` dentro del contenedor.

### 2. Deploy with Docker Compose

```bash
# Transfer files to server
scp -r . user@YOUR_SERVER_IP:/path/to/app/

# SSH to server
ssh user@YOUR_SERVER_IP

# Navigate to project
cd /path/to/app

# Build and start services
docker-compose -f docker-compose.production.yml up -d --build
```

El contenedor SQL Server se construirá con:
- `Dockerfile.sqlserver` (base image + cron + backup script)
- Cron job configurado (diario a las 2 AM)
- Script de backup en `/opt/scripts/backup-sqlserver.sh`

### 3. Verify Cron is Running

```bash
# Check cron status inside container
docker exec controlpeso-sqlserver ps aux | grep cron

# Expected output:
# root         123  0.0  0.0   8172  2816 ?        Ss   22:00   0:00 /usr/sbin/cron
```

### 4. Verify Backup Schedule

```bash
# List cron jobs
docker exec controlpeso-sqlserver crontab -l

# Expected output:
# 0 2 * * * /opt/scripts/backup-sqlserver.sh >> /var/opt/mssql/backups/backup-cron.log 2>&1
```

## Manual Operations

### On-Demand Backup

```bash
# Run backup immediately (outside cron schedule)
./scripts/backup-now.sh
```

### List Backups

```bash
# List all backups with sizes and dates
docker exec controlpeso-sqlserver ls -lh /var/opt/mssql/backups

# Example output:
# -rw-r----- 1 mssql root 1.5M Feb 25 02:00 ControlPeso_20260225_020000.bak
# -rw-r----- 1 mssql root 1.5M Feb 24 02:00 ControlPeso_20260224_020000.bak
# -rw-r----- 1 mssql root  45K Feb 25 02:00 backup.log
```

### View Backup Logs

```bash
# View last 50 lines of backup log
docker exec controlpeso-sqlserver tail -n 50 /var/opt/mssql/backups/backup.log

# View cron execution log
docker exec controlpeso-sqlserver tail -n 50 /var/opt/mssql/backups/backup-cron.log
```

### Restore from Backup

```bash
# List available backups
./scripts/restore-backup.sh

# Restore specific backup
./scripts/restore-backup.sh ControlPeso_20260225_020000.bak

# Follow prompts to confirm restore
```

**⚠️ WARNING:** Restore reemplaza la base de datos actual. Hacer backup manual antes si es necesario.

## Backup Details

### Filename Format

```
ControlPeso_YYYYMMDD_HHMMSS.bak
```

Ejemplos:
- `ControlPeso_20260225_020000.bak` → 25 de febrero 2026, 2:00 AM
- `ControlPeso_20260224_140530.bak` → 24 de febrero 2026, 2:05:30 PM (manual)

### Backup Features

**Compression:**
- Reduce tamaño de backup ~60-70%
- Típico: BD 100MB → Backup 30-40MB
- Comando SQL: `WITH COMPRESSION`

**Checksum:**
- Detecta corrupción de datos
- Valida integridad durante backup
- Comando SQL: `WITH CHECKSUM`

**Verification:**
- RESTORE VERIFYONLY después de cada backup
- Asegura que el backup es válido para restore
- NO restaura datos, solo verifica

### Retention Policy

- **Retención:** 30 días
- **Frecuencia:** Diaria (2 AM)
- **Resultado:** ~30 backups en disco
- **Espacio estimado:** 1-3 GB (depende del tamaño de BD)

### Rotation Logic

```bash
# Find backups older than 30 days and delete
find /var/opt/mssql/backups -name "ControlPeso_*.bak" -type f -mtime +30 -delete
```

## Monitoring

### Check Last Backup Date

```bash
# Get newest backup file
docker exec controlpeso-sqlserver ls -lt /var/opt/mssql/backups | grep "\.bak$" | head -n 1
```

### Check Backup Success

```bash
# Check last entry in backup log
docker exec controlpeso-sqlserver tail -n 20 /var/opt/mssql/backups/backup.log

# Look for:
# [TIMESTAMP] [INFO] Backup completed successfully
# [TIMESTAMP] [INFO] Backup verification passed
```

### Disk Space Monitoring

```bash
# Check backup directory size
docker exec controlpeso-sqlserver du -sh /var/opt/mssql/backups

# Check host mount point
df -h /mnt/backups/controlpeso-sqlserver
```

## Disaster Recovery

### Scenario 1: Restore from Last Night's Backup

```bash
# 1. List backups
./scripts/restore-backup.sh

# 2. Identify last backup (e.g., ControlPeso_20260225_020000.bak)
# 3. Restore
./scripts/restore-backup.sh ControlPeso_20260225_020000.bak

# 4. Restart web app
docker-compose -f docker-compose.production.yml restart controlpeso-web

# 5. Verify
docker exec controlpeso-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT COUNT(*) FROM ControlPeso.dbo.Users"
```

### Scenario 2: Database Corruption

```bash
# 1. Check SQL Server error logs
docker logs controlpeso-sqlserver

# 2. Try repair (if possible)
docker exec controlpeso-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "DBCC CHECKDB (ControlPeso) WITH PHYSICAL_ONLY"

# 3. If repair fails, restore from backup
./scripts/restore-backup.sh ControlPeso_YYYYMMDD_HHMMSS.bak
```

### Scenario 3: Complete Server Failure

1. **Provision new server**
2. **Install Docker + Docker Compose**
3. **Create backup directory:**
   ```bash
   sudo mkdir -p /mnt/backups/controlpeso-sqlserver
   ```
4. **Copy backups from old server:**
   ```bash
   scp -r oldserver:/mnt/backups/controlpeso-sqlserver/* /mnt/backups/controlpeso-sqlserver/
   ```
5. **Deploy application:**
   ```bash
   docker-compose -f docker-compose.production.yml up -d
   ```
6. **Restore latest backup:**
   ```bash
   ./scripts/restore-backup.sh ControlPeso_YYYYMMDD_HHMMSS.bak
   ```

## Backup Security

### Access Control

- Backups están en `/mnt/backups` (fuera del contenedor)
- Permisos restringidos: `chmod 770` (owner + group only)
- Bind mount read-write (contenedor necesita escribir)

### Encryption (Optional Future Enhancement)

Actualmente backups NO están encriptados. Para agregar encriptación:

```sql
-- Create master key
CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'StrongPassword123!';

-- Create certificate
CREATE CERTIFICATE BackupCert
WITH SUBJECT = 'ControlPeso Backup Certificate';

-- Backup with encryption
BACKUP DATABASE [ControlPeso]
TO DISK = '/var/opt/mssql/backups/ControlPeso_encrypted.bak'
WITH
    ENCRYPTION (ALGORITHM = AES_256, SERVER CERTIFICATE = BackupCert),
    COMPRESSION,
    CHECKSUM;
```

## Troubleshooting

### Backup Not Running Automatically

**Check cron service:**
```bash
docker exec controlpeso-sqlserver service cron status
```

**Check cron logs:**
```bash
docker exec controlpeso-sqlserver tail -f /var/opt/mssql/backups/backup-cron.log
```

**Restart cron:**
```bash
docker exec controlpeso-sqlserver service cron restart
```

### Backup Fails with Permission Denied

**Check directory permissions:**
```bash
docker exec controlpeso-sqlserver ls -ld /var/opt/mssql/backups
```

**Fix permissions (if needed):**
```bash
docker exec -u root controlpeso-sqlserver chown -R mssql:root /var/opt/mssql/backups
docker exec -u root controlpeso-sqlserver chmod -R 770 /var/opt/mssql/backups
```

### Disk Full Error

**Check disk usage:**
```bash
df -h /mnt/backups
docker exec controlpeso-sqlserver du -sh /var/opt/mssql/backups
```

**Manually delete old backups:**
```bash
docker exec controlpeso-sqlserver find /var/opt/mssql/backups -name "ControlPeso_*.bak" -mtime +7 -delete
```

**Reduce retention:**
Edit `scripts/backup-sqlserver.sh` and change `RETENTION_DAYS=30` to lower value.

## Performance Impact

- **Backup duration:** 1-5 minutos (depende del tamaño de BD)
- **CPU usage:** Pico durante compresión (~30-50%)
- **I/O impact:** Medio (escritura secuencial)
- **Downtime:** ❌ NONE (backups online, BD sigue operativa)

**Best practices:**
- Backups programados a las 2 AM (bajo tráfico)
- Compresión habilitada (reduce I/O)
- Verificación después de backup (no durante)

## Future Enhancements

1. **Backup encryption** (AES-256)
2. **Off-site backup replication** (rsync a servidor remoto)
3. **Backup monitoring alerts** (Telegram/Email en fallos)
4. **Incremental backups** (solo cambios, reduce tiempo)
5. **Backup to cloud storage** (Azure Blob, AWS S3)

## References

- SQL Server Backup docs: https://learn.microsoft.com/sql/relational-databases/backup-restore/backup-overview-sql-server
- Docker SQL Server: https://learn.microsoft.com/sql/linux/quickstart-install-connect-docker
- Cron schedule expression: https://crontab.guru/
