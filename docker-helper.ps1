# =============================================================================
# Control Peso Thiscloud - Docker Helper Script (PowerShell)
# =============================================================================

$ErrorActionPreference = "Stop"

# Functions
function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Check if Docker is running
function Test-Docker {
    try {
        docker info | Out-Null
    }
    catch {
        Write-Error "Docker is not running. Please start Docker Desktop."
        exit 1
    }
}

# Check if .env file exists
function Test-EnvFile {
    if (-not (Test-Path ".env")) {
        Write-Warning ".env file not found. Creating from .env.example..."
        Copy-Item ".env.example" ".env"
        Write-Info "Please edit .env with your OAuth credentials:"
        Write-Info "  - GOOGLE_CLIENT_ID"
        Write-Info "  - GOOGLE_CLIENT_SECRET"
        Write-Info "  - LINKEDIN_CLIENT_ID"
        Write-Info "  - LINKEDIN_CLIENT_SECRET"
        exit 1
    }
}

# Main menu
function Show-Menu {
    Write-Host ""
    Write-Host "========================================"
    Write-Host " Control Peso Thiscloud - Docker Helper"
    Write-Host "========================================"
    Write-Host "1. Build & Start (first time)"
    Write-Host "2. Start services"
    Write-Host "3. Stop services"
    Write-Host "4. Restart services"
    Write-Host "5. View logs"
    Write-Host "6. View real-time logs"
    Write-Host "7. Status"
    Write-Host "8. Backup database"
    Write-Host "9. Restore database"
    Write-Host "10. Clean all (⚠️ DELETES DATA)"
    Write-Host "11. Shell access"
    Write-Host "0. Exit"
    Write-Host "========================================"
    $option = Read-Host "Select option"
    return $option
}

# Build and start
function Invoke-BuildStart {
    Write-Info "Building image and starting services..."
    docker-compose up -d --build
    Write-Success "Services started!"
    Write-Info "Access application at: http://localhost:8080"
}

# Start services
function Start-Services {
    Write-Info "Starting services..."
    docker-compose up -d
    Write-Success "Services started!"
}

# Stop services
function Stop-Services {
    Write-Info "Stopping services..."
    docker-compose down
    Write-Success "Services stopped!"
}

# Restart services
function Restart-Services {
    Write-Info "Restarting services..."
    docker-compose restart
    Write-Success "Services restarted!"
}

# View logs
function Show-Logs {
    Write-Info "Showing last 100 lines of logs..."
    docker-compose logs --tail=100 controlpeso-web
}

# View real-time logs
function Show-LogsFollow {
    Write-Info "Following logs (Ctrl+C to exit)..."
    docker-compose logs -f controlpeso-web
}

# Status
function Show-Status {
    Write-Info "Service status:"
    docker-compose ps
    Write-Host ""
    Write-Info "Volume status:"
    docker volume ls | Select-String "controlpeso"
}

# Backup database
function Backup-Database {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $backupFile = "backup-controlpeso-$timestamp.db"
    Write-Info "Creating database backup: $backupFile"
    docker cp controlpeso-web:/app/data/controlpeso.db "./$backupFile"
    Write-Success "Backup created: $backupFile"
}

# Restore database
function Restore-Database {
    $backupFile = Read-Host "Enter backup file path"
    
    if (-not (Test-Path $backupFile)) {
        Write-Error "Backup file not found: $backupFile"
        return
    }
    
    Write-Warning "This will overwrite the current database!"
    $confirm = Read-Host "Are you sure? (yes/no)"
    
    if ($confirm -ne "yes") {
        Write-Info "Restore cancelled."
        return
    }
    
    Write-Info "Restoring database from: $backupFile"
    docker cp $backupFile controlpeso-web:/app/data/controlpeso.db
    Write-Success "Database restored! Restarting services..."
    docker-compose restart
}

# Clean all
function Remove-All {
    Write-Warning "⚠️  This will DELETE ALL DATA (containers, images, volumes)!"
    $confirm = Read-Host "Are you sure? (yes/no)"
    
    if ($confirm -ne "yes") {
        Write-Info "Clean cancelled."
        return
    }
    
    Write-Info "Stopping services..."
    docker-compose down -v
    
    Write-Info "Removing image..."
    try {
        docker rmi controlpeso-controlpeso-web 2>$null
    }
    catch {
        # Ignore if image doesn't exist
    }
    
    Write-Success "All cleaned!"
}

# Shell access
function Open-Shell {
    Write-Info "Opening shell in container (type 'exit' to leave)..."
    docker exec -it controlpeso-web /bin/bash
}

# Main script
function Main {
    Test-Docker
    Test-EnvFile
    
    while ($true) {
        $option = Show-Menu
        
        switch ($option) {
            "1" { Invoke-BuildStart }
            "2" { Start-Services }
            "3" { Stop-Services }
            "4" { Restart-Services }
            "5" { Show-Logs }
            "6" { Show-LogsFollow }
            "7" { Show-Status }
            "8" { Backup-Database }
            "9" { Restore-Database }
            "10" { Remove-All }
            "11" { Open-Shell }
            "0" {
                Write-Info "Goodbye!"
                exit 0
            }
            default {
                Write-Error "Invalid option: $option"
            }
        }
        
        Write-Host ""
        Read-Host "Press Enter to continue"
    }
}

# Run main
Main
