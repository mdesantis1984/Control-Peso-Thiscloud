# Production Secrets Management Guide

## Overview

Control Peso Thiscloud usa **docker-compose.override.yml** para gestión de secretos en producción.

## Security Model

```
┌─────────────────────────────────────────────────────────┐
│  Git Repository (PUBLIC)                                │
│  ✅ docker-compose.production.yml (base config)         │
│  ✅ docker-compose.override.yml.production.example      │
│  ❌ docker-compose.override.yml (NEVER commit)          │
└─────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────┐
│  Production Server                                      │
│  📁 docker-compose.production.yml (pulled from Git)     │
│  📁 docker-compose.override.yml (MANUAL creation)       │
│  🔒 Contains REAL secrets (SQL password, OAuth)         │
└─────────────────────────────────────────────────────────┘
                           ↓
                 Docker Compose merges automatically
```

## Setup on Production Server

### Step 1: Copy Template

```bash
# SSH to production server
ssh user@10.0.0.100

# Navigate to project directory
cd /opt/controlpeso

# Copy template
cp docker-compose.override.yml.production.example docker-compose.override.yml
```

### Step 2: Edit with Real Secrets

```bash
# Edit file (use nano, vim, or vi)
nano docker-compose.override.yml
```

**Required secrets:**

```yaml
services:
  controlpeso-sqlserver:
    environment:
      # Strong password: 8+ chars, mixed case, digits, special chars
      - MSSQL_SA_PASSWORD=YourRealProductionPassword@2026!
  
  controlpeso-web:
    environment:
      # Google OAuth (from console.cloud.google.com)
      - Authentication__Google__ClientId=123456789-abc.apps.googleusercontent.com
      - Authentication__Google__ClientSecret=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45
```

### Step 3: Verify File is Gitignored

```bash
# Should output: docker-compose.override.yml
git check-ignore docker-compose.override.yml

# If not ignored, add to .gitignore immediately!
echo "docker-compose.override.yml" >> .gitignore
```

### Step 4: Set Secure Permissions

```bash
# Only owner can read/write
chmod 600 docker-compose.override.yml

# Verify permissions
ls -l docker-compose.override.yml
# Expected: -rw------- 1 user user 1234 Feb 25 10:00 docker-compose.override.yml
```

### Step 5: Deploy

```bash
# Docker Compose automatically merges files
docker-compose -f docker-compose.production.yml up -d

# Verify secrets are loaded
docker exec controlpeso-web env | grep "Authentication__Google__ClientId"
# Should output: Authentication__Google__ClientId=123456789-abc.apps.googleusercontent.com
```

## Secrets Checklist

### Required (MUST configure)

- [x] `MSSQL_SA_PASSWORD` - SQL Server SA password
- [x] `Authentication__Google__ClientId` - Google OAuth Client ID
- [x] `Authentication__Google__ClientSecret` - Google OAuth Client Secret

### Optional (can be configured later)

- [ ] `Authentication__LinkedIn__ClientId` - LinkedIn OAuth (if using)
- [ ] `Authentication__LinkedIn__ClientSecret` - LinkedIn OAuth (if using)
- [ ] `GoogleAnalytics__MeasurementId` - Google Analytics 4 (not in this deployment)
- [ ] `Telegram__BotToken` - Telegram notifications
- [ ] `Telegram__ChatId` - Telegram chat ID
- [ ] `CloudflareAnalytics__Token` - Cloudflare Analytics (optional)

## Secret Rotation

### Change SQL Server Password

```bash
# 1. Stop services
docker-compose -f docker-compose.production.yml down

# 2. Edit docker-compose.override.yml with new password
nano docker-compose.override.yml

# 3. Remove old SQL Server volume (WARNING: data loss if no backup!)
docker volume rm controlpeso-sqlserver-data

# 4. Start services (will create new DB with new password)
docker-compose -f docker-compose.production.yml up -d

# 5. Restore from backup
./scripts/restore-backup.sh ControlPeso_YYYYMMDD_HHMMSS.bak
```

### Change Google OAuth Credentials

```bash
# 1. Generate new ClientId + ClientSecret in Google Cloud Console
# 2. Edit docker-compose.override.yml
nano docker-compose.override.yml

# 3. Restart only web container (no downtime for DB)
docker-compose -f docker-compose.production.yml restart controlpeso-web

# 4. Verify users can login
curl -I https://controlpeso.thiscloud.com.ar/login
```

## Security Best Practices

### DO

✅ Use strong passwords (8+ chars, mixed case, digits, special chars)
✅ Rotate secrets every 90 days
✅ Limit access to server (SSH key auth, no password)
✅ Use firewall to block SQL Server port (1433) from external access
✅ Keep backups encrypted (future enhancement)
✅ Use different passwords for dev/staging/production
✅ Store backup of secrets in password manager (1Password, LastPass, Bitwarden)

### DON'T

❌ Commit docker-compose.override.yml to Git
❌ Share secrets via email, Slack, WhatsApp
❌ Use default passwords (YourStrong@Passw0rd)
❌ Reuse passwords across environments
❌ Store secrets in plain text files on desktop
❌ Take screenshots of secrets
❌ Use weak passwords (password123, admin)

## Troubleshooting

### Secret Not Loaded

**Symptom:** Google OAuth fails with "ClientId not configured"

**Solution:**
```bash
# Check environment variables inside container
docker exec controlpeso-web env | grep Authentication

# If empty, verify docker-compose.override.yml exists
ls -la docker-compose.override.yml

# Recreate container to reload environment
docker-compose -f docker-compose.production.yml up -d --force-recreate
```

### SQL Server Authentication Failed

**Symptom:** Web app can't connect to SQL Server

**Solution:**
```bash
# 1. Check SQL Server logs
docker logs controlpeso-sqlserver

# 2. Verify password matches in both services
docker exec controlpeso-sqlserver env | grep MSSQL_SA_PASSWORD
docker exec controlpeso-web env | grep ConnectionStrings__DefaultConnection

# 3. Test SQL Server connection manually
docker exec controlpeso-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1"
```

### Accidental Commit of Secrets

**If you accidentally committed secrets to Git:**

1. **Immediately revoke compromised secrets** (Google OAuth, SQL password, etc.)
2. **Remove from Git history:**
   ```bash
   git filter-branch --force --index-filter \
     'git rm --cached --ignore-unmatch docker-compose.override.yml' \
     --prune-empty --tag-name-filter cat -- --all
   
   git push origin --force --all
   git push origin --force --tags
   ```
3. **Generate new secrets** and deploy
4. **Force users to re-login** (invalidate session cookies)

## Backup of Secrets

Store secrets backup in secure location:

### Option 1: Password Manager (Recommended)

- Use 1Password, LastPass, or Bitwarden
- Create secure note: "Control Peso Production Secrets"
- Store each secret as key-value pair
- Share with authorized team members only

### Option 2: Encrypted File

```bash
# Encrypt secrets file
gpg --symmetric --cipher-algo AES256 docker-compose.override.yml
# Creates: docker-compose.override.yml.gpg

# Store .gpg file in secure backup location
# Decrypt when needed:
gpg --decrypt docker-compose.override.yml.gpg > docker-compose.override.yml
```

### Option 3: Azure Key Vault / AWS Secrets Manager (Future)

For enterprise deployments, consider cloud secret management:
- Azure Key Vault
- AWS Secrets Manager
- HashiCorp Vault

## Environment-Specific Secrets

| Environment | SQL Password | OAuth ClientId | OAuth ClientSecret |
|-------------|--------------|----------------|---------------------|
| Development | `DevPassword123!` | Dev ClientId | Dev Secret |
| Staging | `StagingPassword456!` | Staging ClientId | Staging Secret |
| Production | `ProductionPassword789!` | **PRODUCTION ClientId** | **PRODUCTION Secret** |

**CRITICAL:** Use different secrets for each environment!

## References

- Docker Compose override docs: https://docs.docker.com/compose/multiple-compose-files/merge/
- Google OAuth setup: `docs/GOOGLE_OAUTH_PRODUCTION.md`
- Backup system: `docs/BACKUP_SYSTEM.md`
- 12-Factor App (Config): https://12factor.net/config
