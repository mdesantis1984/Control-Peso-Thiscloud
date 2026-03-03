# Setup Configuration Files - Control Peso Thiscloud
# This script copies .template files to actual configuration files if they don't exist
# Run this script after cloning the repository or when switching branches

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Config Setup - Control Peso Thiscloud    " -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$webProjectPath = "src/ControlPeso.Web"
$setupComplete = $true

# Configuration files to setup
$configFiles = @(
    @{
        Template = "$webProjectPath/appsettings.Development.json.template"
        Target   = "$webProjectPath/appsettings.Development.json"
        Name     = "Development Configuration"
    },
    @{
        Template = "$webProjectPath/appsettings.Production.json.template"
        Target   = "$webProjectPath/appsettings.Production.json"
        Name     = "Production Configuration"
    }
)

foreach ($config in $configFiles) {
    Write-Host "Checking $($config.Name)..." -ForegroundColor Yellow
    
    if (-Not (Test-Path $config.Template)) {
        Write-Host "  ❌ ERROR: Template file not found: $($config.Template)" -ForegroundColor Red
        $setupComplete = $false
        continue
    }
    
    if (Test-Path $config.Target) {
        Write-Host "  ✅ Already exists: $($config.Target)" -ForegroundColor Green
        Write-Host "     (Skipping - not overwriting existing file)" -ForegroundColor Gray
    }
    else {
        try {
            Copy-Item -Path $config.Template -Destination $config.Target -ErrorAction Stop
            Write-Host "  ✅ Created: $($config.Target)" -ForegroundColor Green
            Write-Host "     ⚠️  IMPORTANT: Edit this file and replace placeholder values!" -ForegroundColor Yellow
            $setupComplete = $false
        }
        catch {
            Write-Host "  ❌ ERROR: Failed to copy template: $($_.Exception.Message)" -ForegroundColor Red
            $setupComplete = $false
        }
    }
    
    Write-Host ""
}

# Summary
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "              SETUP SUMMARY                 " -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

if ($setupComplete) {
    Write-Host "✅ All configuration files are ready!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Verify configuration values are correct" -ForegroundColor White
    Write-Host "  2. Run: dotnet build" -ForegroundColor White
    Write-Host "  3. Run: dotnet run --project src/ControlPeso.Web" -ForegroundColor White
}
else {
    Write-Host "⚠️  Configuration setup incomplete!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Action required:" -ForegroundColor Cyan
    Write-Host "  1. Edit the newly created configuration files" -ForegroundColor White
    Write-Host "  2. Replace ALL placeholder values (YOUR_*_HERE)" -ForegroundColor White
    Write-Host "  3. Save the files" -ForegroundColor White
    Write-Host "  4. Re-run this script to verify" -ForegroundColor White
    Write-Host ""
    Write-Host "Configuration files to edit:" -ForegroundColor Yellow
    foreach ($config in $configFiles) {
        if (Test-Path $config.Target) {
            $content = Get-Content $config.Target -Raw
            if ($content -match "YOUR_.*_HERE") {
                Write-Host "  • $($config.Target)" -ForegroundColor Red
            }
        }
    }
}

Write-Host ""
Write-Host "Note: Configuration files are in .gitignore and will NOT be committed." -ForegroundColor Gray
Write-Host "      Templates (.template files) ARE committed for version control." -ForegroundColor Gray
Write-Host ""

if (-Not $setupComplete) {
    exit 1
}
