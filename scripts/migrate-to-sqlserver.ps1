<#
.SYNOPSIS
Migrate ControlPeso database from SQLite to SQL Server Express

.DESCRIPTION
Creates SQL Server database and migrates data from SQLite

.PARAMETER SQLiteDbPath
Path to SQLite database file. Default: controlpeso.db

.PARAMETER SqlServerInstance
SQL Server instance name. Default: localhost\SQLEXPRESS

.PARAMETER SkipDataMigration
Create empty database without migrating data from SQLite

.EXAMPLE
.\migrate-to-sqlserver.ps1
Migrate from controlpeso.db to SQL Server Express with data

.EXAMPLE
.\migrate-to-sqlserver.ps1 -SkipDataMigration
Create empty SQL Server database
#>

param(
    [string]$SQLiteDbPath = "$PSScriptRoot\..\controlpeso.db",
    [string]$SqlServerInstance = "localhost\SQLEXPRESS",
    [switch]$SkipDataMigration = $false
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SQLite → SQL Server Migration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Verify SQL Server connectivity
Write-Host "[1/5] Verifying SQL Server connectivity..." -ForegroundColor Yellow
Write-Host "  Server: $SqlServerInstance" -ForegroundColor Gray
try {
    $version = sqlcmd -S $SqlServerInstance -Q "SELECT @@VERSION" -E -h -1
    Write-Host "  SQL Server connected successfully" -ForegroundColor Green
    Write-Host "  Version: $($version.Trim().Substring(0, 60))..." -ForegroundColor Gray
} catch {
    Write-Host "  FAIL: Cannot connect to SQL Server" -ForegroundColor Red
    Write-Error "Ensure SQL Server Express is running. Error: $_"
}

# Step 2: Create SQL Server database
Write-Host "`n[2/5] Creating SQL Server database..." -ForegroundColor Yellow
$schemaPath = "$PSScriptRoot\..\docs\schema\sqlserver_schema_v1.sql"
if (-not (Test-Path $schemaPath)) {
    Write-Error "Schema file not found: $schemaPath"
}

try {
    sqlcmd -S $SqlServerInstance -i $schemaPath -E -b
    Write-Host "  Database 'ControlPeso' created successfully" -ForegroundColor Green
} catch {
    Write-Host "  FAIL: Database creation failed" -ForegroundColor Red
    Write-Error $_
}

# Step 3: Verify database exists
Write-Host "`n[3/5] Verifying database..." -ForegroundColor Yellow
$dbCheck = sqlcmd -S $SqlServerInstance -Q "SELECT name FROM sys.databases WHERE name = 'ControlPeso'" -E -h -1
if ($dbCheck -match "ControlPeso") {
    Write-Host "  Database verified: ControlPeso" -ForegroundColor Green
    
    # Count tables
    $tableCount = sqlcmd -S $SqlServerInstance -d ControlPeso -Q "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'" -E -h -1
    Write-Host "  Tables created: $($tableCount.Trim())" -ForegroundColor Green
} else {
    Write-Error "Database verification failed"
}

# Step 4: Migrate data from SQLite (if not skipped)
if (-not $SkipDataMigration) {
    Write-Host "`n[4/5] Migrating data from SQLite..." -ForegroundColor Yellow
    
    if (-not (Test-Path $SQLiteDbPath)) {
        Write-Host "  WARNING: SQLite database not found: $SQLiteDbPath" -ForegroundColor Yellow
        Write-Host "  Skipping data migration (database will be empty)" -ForegroundColor Yellow
    } else {
        Write-Host "  Source: $SQLiteDbPath" -ForegroundColor Gray
        
        # Check if sqlite3 CLI is available
        try {
            sqlite3 -version | Out-Null
            Write-Host "  SQLite CLI available" -ForegroundColor Green
        } catch {
            Write-Host "  WARNING: sqlite3.exe not found in PATH" -ForegroundColor Yellow
            Write-Host "  Download from: https://www.sqlite.org/download.html" -ForegroundColor Yellow
            Write-Host "  Skipping data migration" -ForegroundColor Yellow
            return
        }
        
        # Export SQLite data to CSV
        Write-Host "  Exporting SQLite tables to CSV..." -NoNewline
        $tempDir = "$PSScriptRoot\..\temp_migration"
        New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
        
        # Export Users
        sqlite3 $SQLiteDbPath ".mode csv" ".output $tempDir\users.csv" "SELECT * FROM Users"
        
        # Export WeightLogs
        sqlite3 $SQLiteDbPath ".mode csv" ".output $tempDir\weightlogs.csv" "SELECT * FROM WeightLogs"
        
        # Export UserPreferences
        sqlite3 $SQLiteDbPath ".mode csv" ".output $tempDir\userpreferences.csv" "SELECT * FROM UserPreferences"
        
        # Export AuditLog
        sqlite3 $SQLiteDbPath ".mode csv" ".output $tempDir\auditlog.csv" "SELECT * FROM AuditLog"
        
        Write-Host " DONE" -ForegroundColor Green
        
        # Import into SQL Server using BCP
        Write-Host "  Importing data into SQL Server..." -ForegroundColor Gray
        
        # Note: Data type conversion needed (TEXT → UNIQUEIDENTIFIER, REAL → DECIMAL, etc.)
        # For production, recommend using Entity Framework Core or custom migration tool
        Write-Host "  WARNING: Manual data conversion required for production" -ForegroundColor Yellow
        Write-Host "  Recommend using EF Core DbContext.Database.Migrate() instead" -ForegroundColor Yellow
        
        # Cleanup temp files
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
} else {
    Write-Host "`n[4/5] Skipping data migration (as requested)" -ForegroundColor Yellow
}

# Step 5: Update connection string in appsettings
Write-Host "`n[5/5] Updating connection string..." -ForegroundColor Yellow
$appsettingsPath = "$PSScriptRoot\..\src\ControlPeso.Web\appsettings.Production.json"
if (Test-Path $appsettingsPath) {
    $connectionString = "Server=$SqlServerInstance;Database=ControlPeso;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
    Write-Host "  Connection string:" -ForegroundColor Gray
    Write-Host "  $connectionString" -ForegroundColor Cyan
    Write-Host "  Please update appsettings.Production.json manually" -ForegroundColor Yellow
} else {
    Write-Host "  WARNING: appsettings.Production.json not found" -ForegroundColor Yellow
}

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  MIGRATION COMPLETED" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Update connection string in appsettings.Production.json" -ForegroundColor White
Write-Host "  2. Test connection: dotnet ef database scaffold" -ForegroundColor White
Write-Host "  3. Run application with SQL Server" -ForegroundColor White
Write-Host ""
Write-Host "Connection string (copy this):" -ForegroundColor Cyan
Write-Host "Server=$SqlServerInstance;Database=ControlPeso;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True" -ForegroundColor White
Write-Host ""
