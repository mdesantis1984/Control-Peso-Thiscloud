#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Script de despliegue local automático para Control Peso Thiscloud con Docker Compose.

.DESCRIPTION
    Este script automatiza el proceso completo de despliegue LOCAL:
    - Verifica pre-requisitos (Docker, credenciales)
    - Build de la imagen Docker
    - Inicialización de servicios
    - Creación y configuración de base de datos
    - Verificación de salud de servicios
    - Tests de integración (Google OAuth, Telegram)

.PARAMETER Action
    Acción a realizar: build, start, stop, restart, logs, init-db, verify, full

.PARAMETER SkipBuild
    No hacer build de la imagen Docker (usar imagen existente)

.PARAMETER SkipHealthCheck
    No verificar health checks de servicios

.EXAMPLE
    .\Deploy-Docker-Local.ps1 -Action full
    Despliegue completo (build + start + init-db + verify)

.EXAMPLE
    .\Deploy-Docker-Local.ps1 -Action start
    Solo iniciar servicios (sin build)

.EXAMPLE
    .\Deploy-Docker-Local.ps1 -Action logs
    Ver logs en tiempo real

.NOTES
    Author: Marco De Santis
    Date: 2025-03-01
    Version: 3.0
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('build', 'start', 'stop', 'restart', 'logs', 'init-db', 'verify', 'full', 'clean')]
    [string]$Action = 'full',

    [Parameter(Mandatory = $false)]
    [switch]$SkipBuild,

    [Parameter(Mandatory = $false)]
    [switch]$SkipHealthCheck
)

# ============================================================================
# CONFIGURATION
# ============================================================================

$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

$script:ComposeFile = 'docker-compose.yml'
$script:OverrideFile = 'docker-compose.override.yml'
$script:InitDbScript = 'scripts/init-database.sql'
$script:ProjectRoot = $PSScriptRoot
$script:ContainerSqlServer = 'controlpeso-sqlserver'
$script:ContainerWeb = 'controlpeso-web'
$script:WebUrl = 'http://localhost:8080'

# ============================================================================
# FUNCTIONS
# ============================================================================

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "============================================================================" -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host "============================================================================" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-ErrorMsg {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Write-WarningMsg {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

function Write-InfoMsg {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Blue
}

function Test-Prerequisites {
    Write-Header "Verificando Pre-requisitos"

    # Verificar Docker
    try {
        $dockerVersion = docker --version
        Write-Success "Docker instalado: $dockerVersion"
    }
    catch {
        Write-ErrorMsg "Docker no está instalado o no está en el PATH"
        exit 1
    }

    # Verificar Docker Compose
    try {
        $composeVersion = docker compose version
        Write-Success "Docker Compose instalado: $composeVersion"
    }
    catch {
        Write-ErrorMsg "Docker Compose no está disponible"
        exit 1
    }

    # Verificar docker-compose.yml
    if (-not (Test-Path $script:ComposeFile)) {
        Write-ErrorMsg "Archivo $script:ComposeFile no encontrado"
        exit 1
    }
    Write-Success "Archivo $script:ComposeFile encontrado"

    # Verificar docker-compose.override.yml
    if (-not (Test-Path $script:OverrideFile)) {
        Write-WarningMsg "Archivo $script:OverrideFile no encontrado"
        Write-InfoMsg "Creando desde template..."
        
        if (Test-Path "$script:OverrideFile.production.example") {
            Copy-Item "$script:OverrideFile.production.example" $script:OverrideFile
            Write-Success "Archivo $script:OverrideFile creado desde template"
            Write-WarningMsg "⚠️  IMPORTANTE: Editar $script:OverrideFile con credenciales reales antes de continuar"
            
            $continue = Read-Host "¿Has configurado las credenciales? (s/n)"
            if ($continue -ne 's') {
                Write-InfoMsg "Deteniendo despliegue. Edita $script:OverrideFile y ejecuta el script nuevamente."
                exit 0
            }
        }
        else {
            Write-ErrorMsg "Template $script:OverrideFile.production.example no encontrado"
            exit 1
        }
    }
    else {
        Write-Success "Archivo $script:OverrideFile encontrado"
    }

    # Verificar script de inicialización de DB
    if (-not (Test-Path $script:InitDbScript)) {
        Write-ErrorMsg "Script de inicialización $script:InitDbScript no encontrado"
        exit 1
    }
    Write-Success "Script de inicialización $script:InitDbScript encontrado"

    Write-Success "Todos los pre-requisitos cumplidos"
}

function Invoke-DockerBuild {
    Write-Header "Build de Imagen Docker"

    if ($SkipBuild) {
        Write-InfoMsg "Saltando build (--SkipBuild especificado)"
        return
    }

    Write-InfoMsg "Iniciando build (esto puede tomar 2-5 minutos)..."
    
    try {
        docker compose build --no-cache
        Write-Success "Build completado exitosamente"
    }
    catch {
        Write-ErrorMsg "Build falló: $_"
        exit 1
    }
}

function Start-ServicesLocal {
    Write-Header "Iniciando Servicios"

    Write-InfoMsg "Iniciando contenedores en modo daemon..."
    
    try {
        docker compose up -d
        Write-Success "Servicios iniciados"
    }
    catch {
        Write-ErrorMsg "Error al iniciar servicios: $_"
        exit 1
    }

    if (-not $SkipHealthCheck) {
        Wait-ForHealthy
    }
}

function Wait-ForHealthy {
    Write-Header "Verificando Health Checks"

    Write-InfoMsg "Esperando a que SQL Server esté healthy..."
    
    $maxAttempts = 30
    $attempt = 0
    $healthy = $false

    while (-not $healthy -and $attempt -lt $maxAttempts) {
        $attempt++
        
        $sqlServerStatus = docker inspect --format='{{.State.Health.Status}}' $script:ContainerSqlServer 2>$null
        
        if ($sqlServerStatus -eq 'healthy') {
            $healthy = $true
            Write-Success "SQL Server está healthy"
        }
        else {
            Write-InfoMsg "Intento $attempt/$maxAttempts - Estado: $sqlServerStatus"
            Start-Sleep -Seconds 2
        }
    }

    if (-not $healthy) {
        Write-ErrorMsg "SQL Server no alcanzó estado healthy después de $maxAttempts intentos"
        Write-InfoMsg "Ejecuta: docker compose logs sqlserver"
        exit 1
    }

    Write-InfoMsg "Esperando a que la aplicación web esté healthy..."
    
    $attempt = 0
    $healthy = $false

    while (-not $healthy -and $attempt -lt $maxAttempts) {
        $attempt++
        
        $webStatus = docker inspect --format='{{.State.Health.Status}}' $script:ContainerWeb 2>$null
        
        if ($webStatus -eq 'healthy') {
            $healthy = $true
            Write-Success "Aplicación web está healthy"
        }
        else {
            Write-InfoMsg "Intento $attempt/$maxAttempts - Estado: $webStatus"
            Start-Sleep -Seconds 2
        }
    }

    if (-not $healthy) {
        Write-ErrorMsg "Aplicación web no alcanzó estado healthy después de $maxAttempts intentos"
        Write-InfoMsg "Ejecuta: docker compose logs controlpeso-web"
        exit 1
    }

    Write-Success "Todos los servicios están healthy"
}

function Initialize-DatabaseLocal {
    Write-Header "Inicializando Base de Datos"

    # Leer SA Password de docker-compose.override.yml
    $saPassword = $null
    $overrideContent = Get-Content $script:OverrideFile -Raw
    
    if ($overrideContent -match 'SA_PASSWORD=([^\s\n]+)') {
        $saPassword = $Matches[1]
    }

    if (-not $saPassword) {
        Write-ErrorMsg "No se pudo obtener SA_PASSWORD desde $script:OverrideFile"
        exit 1
    }

    Write-InfoMsg "Copiando script SQL al contenedor..."
    
    try {
        docker cp $script:InitDbScript "${script:ContainerSqlServer}:/tmp/init-database.sql"
        Write-Success "Script copiado"
    }
    catch {
        Write-ErrorMsg "Error al copiar script: $_"
        exit 1
    }

    Write-InfoMsg "Ejecutando script de inicialización..."
    
    try {
        $output = docker exec -it $script:ContainerSqlServer /opt/mssql-tools18/bin/sqlcmd `
            -S localhost `
            -U sa `
            -P $saPassword `
            -i /tmp/init-database.sql `
            -C

        Write-Host $output
        
        if ($output -match '✅.*complete') {
            Write-Success "Base de datos inicializada correctamente"
        }
        else {
            Write-WarningMsg "Script ejecutado, pero no se encontró mensaje de confirmación"
        }
    }
    catch {
        Write-ErrorMsg "Error al ejecutar script de inicialización: $_"
        exit 1
    }
}

function Test-DeploymentLocal {
    Write-Header "Verificando Despliegue"

    # Test 1: Health endpoint
    Write-InfoMsg "Verificando health endpoint..."
    try {
        $response = Invoke-WebRequest -Uri "$script:WebUrl/health" -UseBasicParsing -TimeoutSec 10
        
        if ($response.StatusCode -eq 200) {
            Write-Success "Health endpoint responde correctamente: $($response.Content)"
        }
        else {
            Write-WarningMsg "Health endpoint respondió con código: $($response.StatusCode)"
        }
    }
    catch {
        Write-ErrorMsg "Error al verificar health endpoint: $_"
    }

    # Test 2: Homepage
    Write-InfoMsg "Verificando homepage..."
    try {
        $response = Invoke-WebRequest -Uri $script:WebUrl -UseBasicParsing -TimeoutSec 10
        
        if ($response.StatusCode -eq 200) {
            Write-Success "Homepage responde correctamente"
        }
        else {
            Write-WarningMsg "Homepage respondió con código: $($response.StatusCode)"
        }
    }
    catch {
        Write-ErrorMsg "Error al verificar homepage: $_"
    }

    # Test 3: Verificar que no hay errores en logs recientes
    Write-InfoMsg "Verificando logs recientes..."
    try {
        $logs = docker compose logs --tail=50 $script:ContainerWeb
        
        $criticalErrors = $logs | Select-String -Pattern 'Critical|ERROR|Exception' -CaseSensitive
        
        if ($criticalErrors) {
            Write-WarningMsg "Se encontraron errores en logs recientes:"
            $criticalErrors | ForEach-Object { Write-Host $_.Line -ForegroundColor Yellow }
        }
        else {
            Write-Success "No se encontraron errores críticos en logs recientes"
        }
    }
    catch {
        Write-WarningMsg "No se pudieron verificar logs: $_"
    }

    # Summary
    Write-Header "Resumen del Despliegue"
    Write-InfoMsg "URL de la aplicación: $script:WebUrl"
    Write-InfoMsg "Telegram Diagnostics: $script:WebUrl/diagnostics/telegram"
    Write-InfoMsg "Ver logs: docker compose logs -f"
    Write-InfoMsg "Detener servicios: docker compose down"
}

function Show-LogsLocal {
    Write-Header "Logs en Tiempo Real"
    Write-InfoMsg "Presiona Ctrl+C para salir (los servicios seguirán corriendo)"
    Write-InfoMsg ""
    
    docker compose logs -f
}

function Stop-ServicesLocal {
    Write-Header "Deteniendo Servicios"
    
    docker compose down
    Write-Success "Servicios detenidos"
}

function Restart-ServicesLocal {
    Write-Header "Reiniciando Servicios"
    
    docker compose restart
    Write-Success "Servicios reiniciados"
    
    if (-not $SkipHealthCheck) {
        Wait-ForHealthy
    }
}

function Clean-EverythingLocal {
    Write-Header "Limpieza Completa"
    
    Write-WarningMsg "⚠️  ADVERTENCIA: Esto eliminará TODOS los datos (base de datos, logs, keys)"
    $confirm = Read-Host "¿Estás seguro? Escribe 'SI' para confirmar"
    
    if ($confirm -ne 'SI') {
        Write-InfoMsg "Limpieza cancelada"
        return
    }
    
    Write-InfoMsg "Deteniendo servicios y eliminando volúmenes..."
    docker compose down -v
    
    Write-InfoMsg "Eliminando imágenes..."
    docker rmi controlpeso-controlpeso-web:latest 2>$null
    
    Write-Success "Limpieza completa realizada"
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

Write-Host ""
Write-Host "🚀 Control Peso Thiscloud - Docker Local Deployment" -ForegroundColor Magenta
Write-Host "   Action: $Action" -ForegroundColor Magenta
Write-Host ""

switch ($Action) {
    'build' {
        Test-Prerequisites
        Invoke-DockerBuild
    }
    'start' {
        Test-Prerequisites
        Start-ServicesLocal
    }
    'stop' {
        Stop-ServicesLocal
    }
    'restart' {
        Restart-ServicesLocal
    }
    'logs' {
        Show-LogsLocal
    }
    'init-db' {
        Initialize-DatabaseLocal
    }
    'verify' {
        Test-DeploymentLocal
    }
    'clean' {
        Clean-EverythingLocal
    }
    'full' {
        Test-Prerequisites
        Invoke-DockerBuild
        Start-ServicesLocal
        Initialize-DatabaseLocal
        Test-DeploymentLocal
    }
}

Write-Host ""
Write-Success "Script completado exitosamente"
Write-Host ""
