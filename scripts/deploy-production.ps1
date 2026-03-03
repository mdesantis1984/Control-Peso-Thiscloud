<#
.SYNOPSIS
    Deployment seguro a producción de Control Peso Thiscloud

.DESCRIPTION
    Script que valida y despliega la aplicación a producción siguiendo workflow seguro:
    1. Test local con .secrets.local
    2. Build Release
    3. Docker image
    4. Transfer a servidor producción
    5. Deploy remoto
    6. Verificación post-deploy

.PARAMETER Version
    Versión semántica a deployar (ej: "7.0.1")

.PARAMETER ServerHost
    IP/hostname del servidor producción (default: 10.0.0.100)

.PARAMETER SkipLocalTest
    Omitir test local (NO RECOMENDADO, solo para hotfixes)

.EXAMPLE
    .\deploy-production.ps1 -Version "7.0.1"
    
.NOTES
    Author: Control Peso Thiscloud Team
    Requires: Docker, scp, ssh, PowerShell 7+
#>

param(
    [Parameter(Mandatory=$true, HelpMessage="Versión semántica (ej: 7.0.1)")]
    [ValidatePattern("^\d+\.\d+\.\d+$")]
    [string]$Version,

    [Parameter(Mandatory=$false)]
    [string]$ServerHost = "10.0.0.100",

    [Parameter(Mandatory=$false)]
    [switch]$SkipLocalTest
)

# ============================================
# CONFIGURACIÓN
# ============================================
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$WebProject = Join-Path $ProjectRoot "src\ControlPeso.Web\ControlPeso.Web.csproj"
$Dockerfile = Join-Path $ProjectRoot "Dockerfile"
$DockerImageName = "controlpeso-web"
$DockerImageTag = "latest"
$TarFileName = "controlpeso-web_$Version.tar"
$RemoteUser = "root"
$RemoteDeployPath = "/opt/controlpeso"

# ============================================
# FUNCIONES
# ============================================

function Write-Step {
    param([string]$Message, [string]$Emoji = "📋")
    Write-Host "`n$Emoji $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Failure {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Test-CommandExists {
    param([string]$Command)
    $null -ne (Get-Command $Command -ErrorAction SilentlyContinue)
}

function Invoke-SafeCommand {
    param(
        [string]$Command,
        [string]$ErrorMessage
    )
    
    Write-Verbose "Ejecutando: $Command"
    Invoke-Expression $Command
    
    if ($LASTEXITCODE -ne 0) {
        Write-Failure $ErrorMessage
        exit 1
    }
}

# ============================================
# VALIDACIONES PRE-REQUISITOS
# ============================================

Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Yellow
Write-Host "  🚀 DEPLOYMENT PRODUCCIÓN - Control Peso Thiscloud" -ForegroundColor Yellow
Write-Host "     Versión: $Version" -ForegroundColor Yellow
Write-Host "     Servidor: $ServerHost" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Yellow

Write-Step "Validando pre-requisitos..." "🔍"

# Verificar comandos requeridos
$requiredCommands = @("git", "dotnet", "docker", "scp", "ssh")
foreach ($cmd in $requiredCommands) {
    if (-not (Test-CommandExists $cmd)) {
        Write-Failure "Comando '$cmd' no encontrado. Instalar antes de continuar."
        exit 1
    }
}
Write-Success "Todos los comandos requeridos disponibles"

# Verificar rama Git
Set-Location $ProjectRoot
$currentBranch = git branch --show-current
if ($currentBranch -notin @("main", "develop")) {
    Write-Failure "Deploy solo desde 'main' o 'develop'. Rama actual: $currentBranch"
    exit 1
}
Write-Success "Rama Git válida: $currentBranch"

# Verificar .secrets.local existe
$secretsFile = Join-Path $ProjectRoot ".secrets.local"
if (-not (Test-Path $secretsFile)) {
    Write-Failure "Archivo .secrets.local no encontrado. Crear desde .secrets.local.example"
    exit 1
}
Write-Success "Archivo .secrets.local encontrado"

# Verificar .secrets.local NO está en staging
$gitStatus = git status --short
if ($gitStatus -match "\.secrets\.local$") {
    Write-Failure "Archivo .secrets.local está en staging area. NUNCA commitear secretos!"
    exit 1
}
Write-Success "Archivo .secrets.local NO está en Git (correcto)"

# ============================================
# PASO 1: TEST LOCAL
# ============================================

if (-not $SkipLocalTest) {
    Write-Step "Testing local con .secrets.local..." "🧪"
    
    # Down containers existentes
    Write-Host "  Deteniendo containers existentes..."
    docker compose -f docker-compose.yml -f .secrets.local down 2>&1 | Out-Null
    
    # Up con .secrets.local
    Write-Host "  Iniciando containers con .secrets.local..."
    docker compose -f docker-compose.yml -f .secrets.local up -d
    
    if ($LASTEXITCODE -ne 0) {
        Write-Failure "Error iniciando containers locales"
        exit 1
    }
    
    # Esperar startup (30s)
    Write-Host "  Esperando startup (30 segundos)..."
    Start-Sleep -Seconds 30
    
    # Verificar /health
    Write-Host "  Verificando /health endpoint..."
    try {
        $healthResponse = Invoke-RestMethod -Uri "http://localhost:8080/health" -TimeoutSec 10
        if ($healthResponse.status -ne "healthy") {
            Write-Failure "Health check local falló: status = $($healthResponse.status)"
            docker compose down
            exit 1
        }
        Write-Success "Health check local OK"
    }
    catch {
        Write-Failure "Health check local falló: $_"
        docker compose logs --tail=50
        docker compose down
        exit 1
    }
    
    # Down containers
    Write-Host "  Deteniendo containers locales..."
    docker compose -f docker-compose.yml -f .secrets.local down 2>&1 | Out-Null
    
    Write-Success "Test local completado exitosamente"
}
else {
    Write-Host "⚠️  ADVERTENCIA: Test local omitido (SkipLocalTest=true)" -ForegroundColor Yellow
}

# ============================================
# PASO 2: BUILD RELEASE
# ============================================

Write-Step "Building aplicación Release..." "🔨"

Invoke-SafeCommand `
    -Command "dotnet build '$WebProject' -c Release" `
    -ErrorMessage "Build Release falló"

Write-Success "Build Release completado"

# ============================================
# PASO 3: DOCKER IMAGE
# ============================================

Write-Step "Construyendo imagen Docker..." "🐳"

Invoke-SafeCommand `
    -Command "docker build -t ${DockerImageName}:$Version -t ${DockerImageName}:${DockerImageTag} -f '$Dockerfile' '$ProjectRoot'" `
    -ErrorMessage "Docker build falló"

Write-Success "Imagen Docker creada: ${DockerImageName}:$Version"

# ============================================
# PASO 4: EXPORT + TRANSFER
# ============================================

Write-Step "Exportando y transfiriendo imagen..." "📦"

# Export a TAR
Write-Host "  Exportando imagen a TAR..."
$tarPath = Join-Path $ProjectRoot $TarFileName
Invoke-SafeCommand `
    -Command "docker save ${DockerImageName}:${DockerImageTag} -o '$tarPath'" `
    -ErrorMessage "Docker save falló"

# Transfer vía SCP
Write-Host "  Transfiriendo a $ServerHost:/tmp/ (puede tomar varios minutos)..."
Invoke-SafeCommand `
    -Command "scp '$tarPath' ${RemoteUser}@${ServerHost}:/tmp/" `
    -ErrorMessage "SCP transfer falló"

Write-Success "Imagen transferida a servidor producción"

# Limpiar TAR local
Remove-Item $tarPath -Force
Write-Verbose "TAR local eliminado: $tarPath"

# ============================================
# PASO 5: DEPLOY REMOTO
# ============================================

Write-Step "Desplegando en producción..." "🚀"

$deployScript = @"
cd $RemoteDeployPath && \
docker load -i /tmp/$TarFileName && \
docker compose -f docker-compose.yml -f docker-compose.production.yml -f .secrets.local down && \
docker compose -f docker-compose.yml -f docker-compose.production.yml -f .secrets.local up -d && \
rm -f /tmp/$TarFileName
"@

Invoke-SafeCommand `
    -Command "ssh ${RemoteUser}@${ServerHost} '$deployScript'" `
    -ErrorMessage "Deploy remoto falló"

Write-Success "Containers desplegados en producción"

# ============================================
# PASO 6: VERIFICACIÓN POST-DEPLOY
# ============================================

Write-Step "Verificando deployment..." "✅"

# Esperar startup (30s)
Write-Host "  Esperando startup producción (30 segundos)..."
Start-Sleep -Seconds 30

# Verificar containers HEALTHY
Write-Host "  Verificando estado containers..."
$containerStatus = ssh ${RemoteUser}@${ServerHost} "docker ps --filter 'name=controlpeso' --format 'table {{.Names}}\t{{.Status}}'"
Write-Host $containerStatus

if ($containerStatus -notmatch "healthy") {
    Write-Failure "Containers NO están healthy. Revisar logs remotos."
    exit 1
}
Write-Success "Containers HEALTHY"

# Verificar /health endpoint
Write-Host "  Verificando /health endpoint producción..."
try {
    $prodHealthResponse = Invoke-RestMethod -Uri "https://controlpeso.thiscloud.com.ar/health" -TimeoutSec 10
    if ($prodHealthResponse.status -ne "healthy") {
        Write-Failure "Health check producción falló: status = $($prodHealthResponse.status)"
        exit 1
    }
    Write-Success "Health check producción OK"
}
catch {
    Write-Failure "Health check producción falló: $_"
    exit 1
}

# Verificar robots.txt
Write-Host "  Verificando robots.txt..."
try {
    $robotsResponse = Invoke-WebRequest -Uri "https://controlpeso.thiscloud.com.ar/robots.txt" -TimeoutSec 10
    if ($robotsResponse.StatusCode -ne 200) {
        Write-Host "⚠️  robots.txt no responde 200" -ForegroundColor Yellow
    }
    else {
        Write-Success "robots.txt OK"
    }
}
catch {
    Write-Host "⚠️  robots.txt no accesible: $_" -ForegroundColor Yellow
}

# ============================================
# FINALIZACIÓN
# ============================================

Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  ✅✅✅ DEPLOYMENT COMPLETADO EXITOSAMENTE" -ForegroundColor Green
Write-Host "     Versión: $Version" -ForegroundColor Green
Write-Host "     URL: https://controlpeso.thiscloud.com.ar" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Green

Write-Host "📋 Próximos pasos:" -ForegroundColor Cyan
Write-Host "  1. Verificar aplicación funcionando en producción"
Write-Host "  2. Monitorear logs: ssh root@$ServerHost 'docker logs -f controlpeso-web'"
Write-Host "  3. Si OK → Git tag: git tag v$Version && git push origin v$Version"
Write-Host "  4. Actualizar Release Notes en GitHub"

exit 0
