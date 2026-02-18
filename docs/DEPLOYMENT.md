# Deployment Documentation - Control Peso Thiscloud

## Overview

This document covers deployment to **Azure App Service** (recommended) or **IIS** (on-premises), with CI/CD via GitHub Actions.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Azure subscription OR Windows Server with IIS
- Domain registered: `controlpeso.thiscloud.com.ar`
- SSL certificate (Let's Encrypt or commercial)
- Git repository: GitHub

## Environment Configuration

### Development
```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=controlpeso.db"
  },
  "Authentication": {
    "Google": {
      "ClientId": "[User Secrets]",
      "ClientSecret": "[User Secrets]"
    }
  }
}
```

### Production
```json
// appsettings.Production.json (DO NOT commit secrets)
{
  "ConnectionStrings": {
    "DefaultConnection": "[Azure App Settings / Environment Variables]"
  },
  "Authentication": {
    "Google": {
      "ClientId": "[Azure App Settings]",
      "ClientSecret": "[Azure App Settings]"
    }
  },
  "GoogleAnalytics": {
    "MeasurementId": "[Azure App Settings]"
  }
}
```

## Azure App Service Deployment

### 1. Create App Service

```bash
# Azure CLI
az login
az group create --name rg-controlpeso --location brazilsouth
az appservice plan create --name asp-controlpeso --resource-group rg-controlpeso --sku B1 --is-linux
az webapp create --name controlpeso --resource-group rg-controlpeso --plan asp-controlpeso --runtime "DOTNETCORE:10.0"
```

### 2. Configure App Settings

```bash
# Connection String (SQL Server)
az webapp config connection-string set --name controlpeso --resource-group rg-controlpeso \
  --connection-string-type SQLServer \
  --settings DefaultConnection="Server=tcp:controlpeso.database.windows.net,1433;Database=ControlPeso;User Id=admin;Password=***;Encrypt=true;"

# Authentication Secrets
az webapp config appsettings set --name controlpeso --resource-group rg-controlpeso \
  --settings \
  Authentication__Google__ClientId="YOUR_CLIENT_ID" \
  Authentication__Google__ClientSecret="YOUR_CLIENT_SECRET" \
  GoogleAnalytics__MeasurementId="G-XXXXXXXXX"
```

### 3. Deploy Code

**Option A: Azure CLI**
```bash
cd src/ControlPeso.Web
dotnet publish -c Release -o ./publish
az webapp deployment source config-zip --resource-group rg-controlpeso --name controlpeso --src publish.zip
```

**Option B: Visual Studio**
1. Right-click `ControlPeso.Web` → Publish
2. Select Azure → App Service (Linux)
3. Sign in to Azure
4. Select `controlpeso` app
5. Publish

### 4. Configure Custom Domain

```bash
# Add custom domain
az webapp config hostname add --webapp-name controlpeso --resource-group rg-controlpeso --hostname controlpeso.thiscloud.com.ar

# Enable HTTPS (App Service Managed Certificate)
az webapp config ssl bind --certificate-thumbprint auto --ssl-type SNI --name controlpeso --resource-group rg-controlpeso --hostname controlpeso.thiscloud.com.ar
```

**DNS Configuration** (at domain registrar):
```
Type: CNAME
Name: www
Value: controlpeso.azurewebsites.net

Type: A
Name: @
Value: [App Service IP from Azure Portal]
```

### 5. Enable Application Insights (Optional)

```bash
az monitor app-insights component create --app controlpeso-insights --location brazilsouth --resource-group rg-controlpeso

# Get Instrumentation Key
INSTRUMENTATION_KEY=$(az monitor app-insights component show --app controlpeso-insights --resource-group rg-controlpeso --query instrumentationKey -o tsv)

# Configure in App Service
az webapp config appsettings set --name controlpeso --resource-group rg-controlpeso \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$INSTRUMENTATION_KEY"
```

## IIS Deployment (On-Premises)

### 1. Prerequisites

- Windows Server 2019+ with IIS 10+
- .NET 10 Hosting Bundle: [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server 2019+ (or SQLite for testing)

### 2. Publish Application

```bash
cd src/ControlPeso.Web
dotnet publish -c Release -o C:\inetpub\wwwroot\controlpeso
```

### 3. Create IIS Site

1. Open IIS Manager
2. Right-click **Sites** → Add Website
3. Site name: `controlpeso`
4. Physical path: `C:\inetpub\wwwroot\controlpeso`
5. Binding: HTTPS, port 443, hostname `controlpeso.thiscloud.com.ar`
6. SSL certificate: Import from Let's Encrypt or commercial CA

### 4. Configure Application Pool

1. Select Application Pool `controlpeso`
2. **.NET CLR Version**: No Managed Code
3. **Managed Pipeline Mode**: Integrated
4. **Identity**: ApplicationPoolIdentity (or custom service account)
5. **Start Mode**: AlwaysRunning (optional, for faster cold starts)

### 5. Environment Variables

Set at System level or in `web.config`:

```xml
<configuration>
  <system.webServer>
    <aspNetCore processPath="dotnet" arguments=".\ControlPeso.Web.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        <environmentVariable name="ConnectionStrings__DefaultConnection" value="Server=localhost;Database=ControlPeso;Trusted_Connection=True;" />
        <environmentVariable name="Authentication__Google__ClientId" value="YOUR_CLIENT_ID" />
        <environmentVariable name="Authentication__Google__ClientSecret" value="YOUR_CLIENT_SECRET" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

### 6. Let's Encrypt SSL (Free)

**Using win-acme**:

```powershell
# Download win-acme
wget https://github.com/win-acme/win-acme/releases/latest/download/win-acme.v2.2.4.1561.x64.pluggable.zip -OutFile wacs.zip
Expand-Archive wacs.zip -DestinationPath C:\wacs
cd C:\wacs

# Run interactive setup
.\wacs.exe

# Select:
# 1. Create certificate
# 2. Manual input
# 3. controlpeso.thiscloud.com.ar
# 4. IIS binding (automatically install)
# 5. Task Scheduler (auto-renewal every 60 days)
```

## CI/CD with GitHub Actions

### Workflow File

`.github/workflows/deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [main]
  workflow_dispatch:

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET 10
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '10.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --configuration Release --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal
    
    - name: Publish
      run: dotnet publish src/ControlPeso.Web/ControlPeso.Web.csproj -c Release -o ./publish
    
    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'controlpeso'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

### Setup GitHub Secrets

1. Go to GitHub repo → Settings → Secrets → Actions
2. Add `AZURE_WEBAPP_PUBLISH_PROFILE`:
   - In Azure Portal → App Service → Get Publish Profile
   - Copy XML content
   - Paste as secret value

## Database Migration (SQLite → SQL Server)

### 1. Export SQLite Data

```bash
# Dump schema + data
sqlite3 controlpeso.db .dump > controlpeso_backup.sql
```

### 2. Convert SQL Syntax

SQLite → SQL Server differences:

| SQLite | SQL Server |
|--------|------------|
| `TEXT` | `NVARCHAR(MAX)` or `NVARCHAR(n)` |
| `INTEGER` | `INT` |
| `REAL` | `FLOAT` or `DECIMAL(p,s)` |
| `AUTOINCREMENT` | `IDENTITY(1,1)` |
| `datetime('now')` | `GETUTCDATE()` |

**Convert script** (manual or tool like [SQLite to SQL Server Converter](https://www.rebasedata.com/convert-sqlite-to-sql-server-online)):

```sql
-- Example conversion
CREATE TABLE Users (
    Id NVARCHAR(450) NOT NULL PRIMARY KEY,
    GoogleId NVARCHAR(200) NOT NULL UNIQUE,
    Name NVARCHAR(200) NOT NULL,
    Email NVARCHAR(320) NOT NULL UNIQUE,
    Role INT NOT NULL DEFAULT 0 CHECK (Role IN (0, 1)),
    -- ...
);
```

### 3. Apply to SQL Server

```bash
# Using sqlcmd
sqlcmd -S tcp:controlpeso.database.windows.net,1433 -d ControlPeso -U admin -P "***" -i controlpeso_backup.sql

# Using SQL Server Management Studio (SSMS)
# 1. Open SSMS
# 2. Connect to SQL Server
# 3. File → Open → controlpeso_backup.sql
# 4. Execute
```

### 4. Update Connection String

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=tcp:controlpeso.database.windows.net,1433;Database=ControlPeso;User Id=admin;Password=***;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;"
}
```

### 5. Test Connection

```bash
cd src/ControlPeso.Web
dotnet ef database drop --force  # Drop SQLite DB
dotnet run  # Should connect to SQL Server
```

## Monitoring & Logging

### Application Insights (Azure)

**Automatic Telemetry**:
- Request rates, response times
- Dependency tracking (SQL queries, HTTP calls)
- Exceptions (unhandled + logged)
- Custom events (weight log created, user signup)

**Query Logs**:
```kql
// Recent exceptions
exceptions
| where timestamp > ago(1h)
| order by timestamp desc

// Slowest requests
requests
| where timestamp > ago(24h)
| summarize avg(duration), max(duration) by name
| order by max_duration desc
```

### Serilog File Logs (All Environments)

**Location**: `logs/controlpeso-YYYYMMDD.ndjson`

**Query with jq**:
```bash
# Errors in last hour
cat logs/controlpeso-$(date +%Y%m%d).ndjson | jq 'select(.Level == "Error")'

# Correlation ID tracking
cat logs/controlpeso-*.ndjson | jq 'select(.CorrelationId == "abc123")'
```

## Health Checks

**Add to `Program.cs`**:
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ControlPesoDbContext>();

app.MapHealthChecks("/health");
```

**Test**:
```bash
curl https://controlpeso.thiscloud.com.ar/health
# Response: Healthy
```

## Backup Strategy

### Azure SQL Database
- **Automated backups**: Enabled by default (7-35 days retention)
- **Point-in-time restore**: Restore to any point within retention period
- **Long-term retention**: Configure for 10 years (optional)

### SQLite (Development)
```bash
# Daily backup (cron job or Task Scheduler)
cp controlpeso.db backups/controlpeso_$(date +%Y%m%d).db

# Restore
cp backups/controlpeso_20260218.db controlpeso.db
```

## Rollback Procedure

### Azure App Service
1. Go to Azure Portal → App Service → Deployment Center
2. View deployment history
3. Click previous deployment → Redeploy

### IIS
1. Stop IIS site
2. Replace files with previous publish
3. Start IIS site

### Database
1. Restore from backup (point-in-time or manual)
2. Run migration scripts if schema changed

## Performance Tuning

### Azure App Service
- **Scale Up**: Increase tier (B1 → S1 for more CPU/RAM)
- **Scale Out**: Add instances for load balancing
- **Always On**: Prevent cold starts
- **ARR Affinity**: Disable if using external session store

### IIS
- **HTTP/2**: Enable in IIS 10+
- **Compression**: Enable Brotli or Gzip
- **Caching**: Configure `Cache-Control` headers
- **CDN**: Use Azure CDN or Cloudflare for static assets

## Security Checklist

- [x] HTTPS enforced (redirect HTTP → HTTPS)
- [x] SSL certificate installed (Let's Encrypt or commercial)
- [x] Secrets in environment variables (NOT in `appsettings.json`)
- [x] Firewall: Allow only ports 80/443
- [x] Database: Restrict access to App Service IP range
- [x] HSTS header enabled (`Strict-Transport-Security`)
- [x] Security headers (CSP, X-Frame-Options, etc.)
- [ ] **TODO**: DDoS protection (Azure DDoS Standard or Cloudflare)
- [ ] **TODO**: WAF (Web Application Firewall) for advanced protection

## Troubleshooting

### 500 Internal Server Error
1. Check Application Insights or IIS logs (`C:\inetpub\logs`)
2. Enable detailed errors:
   ```json
   "DetailedErrors": "true",
   "ASPNETCORE_ENVIRONMENT": "Development"
   ```
3. Check connection string correctness
4. Verify .NET 10 Hosting Bundle installed (IIS)

### Database Connection Timeout
1. Check firewall allows App Service IP
2. Verify connection string credentials
3. Test with `sqlcmd` or SSMS from same network

### OAuth Login Not Working
1. Verify OAuth redirect URI in Google/LinkedIn app settings:
   - `https://controlpeso.thiscloud.com.ar/signin-google`
   - `https://controlpeso.thiscloud.com.ar/signin-linkedin`
2. Check `ClientId` and `ClientSecret` in App Settings
3. Ensure HTTPS (OAuth requires secure callback)

### Slow Performance
1. Enable Application Insights to identify bottlenecks
2. Check database query performance (missing indexes)
3. Enable output caching for static pages
4. Use Azure CDN for MudBlazor assets

## References

- [Azure App Service Docs](https://docs.microsoft.com/en-us/azure/app-service/)
- [IIS Configuration Reference](https://docs.microsoft.com/en-us/iis/configuration/)
- [Let's Encrypt win-acme](https://www.win-acme.com/)
- [GitHub Actions for Azure](https://github.com/marketplace/actions/azure-webapp)
- [Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
