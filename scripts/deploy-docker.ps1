<#
.SYNOPSIS
Deploy Control Peso Thiscloud to Production Server (10.0.0.100) using Docker

.DESCRIPTION
Automated Docker deployment script:
- Builds Docker image locally
- Runs tests (optional)
- Pushes image to target server
- Deploys with docker-compose on production server
- Runs health checks

.PARAMETER ServerIP
Target production server IP. Default: 10.0.0.100

.PARAMETER ServerUser
SSH username for server. Default: administrator

.PARAMETER SkipTests
Skip running tests before deployment

.PARAMETER SkipBuild
Skip building Docker image (use existing)

.EXAMPLE
.\deploy-docker.ps1
Standard deployment with tests and build

.EXAMPLE
.\deploy-docker.ps1 -SkipTests -ServerUser "admin"
Quick deployment without tests

.NOTES
Requires:
- Docker Desktop installed locally
- SSH access to production server
- Docker installed on production server
- docker-compose on production server
#>

param(
    [string]$ServerIP = "10.0.0.100",
    [string]$ServerUser = "administrator",
    [int]$ServerSSHPort = 22,
    [switch]$SkipTests = $false,
    [switch]$SkipBuild = $false,
    [string]$ImageName = "controlpeso-web",
    [string]$ImageTag = "latest"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Configuration
$ProjectRoot = "$PSScriptRoot\.."
$DockerComposeFile = "$ProjectRoot\docker-compose.yml"
$DockerComposeOverride = "$ProjectRoot\docker-compose.override.yml"
$RemotePath = "/opt/controlpeso"
$HealthCheckUrl = "http://$ServerIP`:8080/health"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Control Peso Thiscloud - Docker Deploy" -ForegroundColor Cyan
Write-Host "  Target: $ServerIP" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Pre-flight checks
Write-Host "[1/8] Pre-flight checks..." -ForegroundColor Yellow

Write-Host "  - Checking Docker..." -NoNewline
try {
    docker --version | Out-Null
    Write-Host " OK" -ForegroundColor Green
} catch {
    Write-Host " FAIL" -ForegroundColor Red
    Write-Error "Docker not found. Install Docker Desktop: https://www.docker.com/products/docker-desktop"
}

Write-Host "  - Checking docker-compose..." -NoNewline
try {
    docker-compose --version | Out-Null
    Write-Host " OK" -ForegroundColor Green
} catch {
    Write-Host " FAIL" -ForegroundColor Red
    Write-Error "docker-compose not found"
}

Write-Host "  - Checking server connectivity..." -NoNewline
$pingResult = Test-Connection -ComputerName $ServerIP -Count 1 -Quiet -ErrorAction SilentlyContinue
if (-not $pingResult) {
    Write-Host " FAIL" -ForegroundColor Red
    Write-Error "Cannot reach server $ServerIP"
}
Write-Host " OK" -ForegroundColor Green

Write-Host "  - Checking SSH access..." -NoNewline
try {
    $sshTest = ssh -o BatchMode=yes -o ConnectTimeout=5 -p $ServerSSHPort "$ServerUser@$ServerIP" "echo OK" 2>&1
    if ($sshTest -like "*OK*") {
        Write-Host " OK" -ForegroundColor Green
    } else {
        Write-Host " WARN (password required)" -ForegroundColor Yellow
    }
} catch {
    Write-Host " FAIL" -ForegroundColor Red
    Write-Host "  Configure SSH key: ssh-copy-id $ServerUser@$ServerIP" -ForegroundColor Yellow
}

# Step 2: Run tests (optional)
if (-not $SkipTests) {
    Write-Host "`n[2/8] Running tests..." -ForegroundColor Yellow
    Push-Location $ProjectRoot
    dotnet test --configuration Release --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Tests FAILED. Aborting deployment." -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Write-Host "  Tests PASSED" -ForegroundColor Green
    Pop-Location
} else {
    Write-Host "`n[2/8] Skipping tests (as requested)" -ForegroundColor Yellow
}

# Step 3: Build Docker image
if (-not $SkipBuild) {
    Write-Host "`n[3/8] Building Docker image..." -ForegroundColor Yellow
    Push-Location $ProjectRoot
    docker build -t ${ImageName}:${ImageTag} -f Dockerfile .
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Build FAILED" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Write-Host "  Image built: ${ImageName}:${ImageTag}" -ForegroundColor Green
    Pop-Location
} else {
    Write-Host "`n[3/8] Skipping build (using existing image)" -ForegroundColor Yellow
}

# Step 4: Save and transfer Docker image
Write-Host "`n[4/8] Transferring Docker image to server..." -ForegroundColor Yellow
$imageTarFile = "$env:TEMP\${ImageName}_${ImageTag}.tar"

Write-Host "  Saving image to tar..." -NoNewline
docker save -o $imageTarFile ${ImageName}:${ImageTag}
if ($LASTEXITCODE -ne 0) {
    Write-Host " FAIL" -ForegroundColor Red
    exit 1
}
$imageSize = [math]::Round((Get-Item $imageTarFile).Length / 1MB, 2)
Write-Host " OK ($imageSize MB)" -ForegroundColor Green

Write-Host "  Transferring to server..." -NoNewline
scp -P $ServerSSHPort $imageTarFile "${ServerUser}@${ServerIP}:/tmp/"
if ($LASTEXITCODE -ne 0) {
    Write-Host " FAIL" -ForegroundColor Red
    Remove-Item $imageTarFile -Force
    exit 1
}
Write-Host " OK" -ForegroundColor Green

# Cleanup local tar file
Remove-Item $imageTarFile -Force

# Step 5: Load image on server
Write-Host "`n[5/8] Loading Docker image on server..." -ForegroundColor Yellow
ssh -p $ServerSSHPort "${ServerUser}@${ServerIP}" "docker load -i /tmp/${ImageName}_${ImageTag}.tar && rm /tmp/${ImageName}_${ImageTag}.tar"
if ($LASTEXITCODE -ne 0) {
    Write-Host "  FAIL" -ForegroundColor Red
    exit 1
}
Write-Host "  Image loaded on server" -ForegroundColor Green

# Step 6: Copy docker-compose files
Write-Host "`n[6/8] Copying docker-compose configuration..." -ForegroundColor Yellow
ssh -p $ServerSSHPort "${ServerUser}@${ServerIP}" "mkdir -p $RemotePath"

Write-Host "  Copying docker-compose.yml..." -NoNewline
scp -P $ServerSSHPort $DockerComposeFile "${ServerUser}@${ServerIP}:${RemotePath}/"
Write-Host " OK" -ForegroundColor Green

if (Test-Path $DockerComposeOverride) {
    Write-Host "  Copying docker-compose.override.yml..." -NoNewline
    scp -P $ServerSSHPort $DockerComposeOverride "${ServerUser}@${ServerIP}:${RemotePath}/"
    Write-Host " OK" -ForegroundColor Green
} else {
    Write-Host "  WARNING: docker-compose.override.yml not found (secrets needed!)" -ForegroundColor Yellow
    Write-Host "  Create it on server: $RemotePath/docker-compose.override.yml" -ForegroundColor Yellow
}

# Step 7: Deploy on server
Write-Host "`n[7/8] Deploying application..." -ForegroundColor Yellow
$deployCmd = @"
cd $RemotePath && \
docker-compose down && \
docker-compose up -d && \
docker-compose ps
"@

ssh -p $ServerSSHPort "${ServerUser}@${ServerIP}" $deployCmd
if ($LASTEXITCODE -ne 0) {
    Write-Host "  Deployment FAILED" -ForegroundColor Red
    exit 1
}
Write-Host "  Deployment successful" -ForegroundColor Green

# Step 8: Health check
Write-Host "`n[8/8] Running health checks..." -ForegroundColor Yellow
Write-Host "  Waiting 15 seconds for app warmup..." -NoNewline
Start-Sleep -Seconds 15
Write-Host " DONE" -ForegroundColor Green

Write-Host "  Testing health endpoint: $HealthCheckUrl" -NoNewline
try {
    $response = Invoke-WebRequest -Uri $HealthCheckUrl -UseBasicParsing -TimeoutSec 30
    if ($response.StatusCode -eq 200) {
        Write-Host " OK" -ForegroundColor Green
        $healthData = $response.Content | ConvertFrom-Json
        Write-Host "  Status: $($healthData.status)" -ForegroundColor Green
        Write-Host "  Version: $($healthData.version)" -ForegroundColor Green
    } else {
        Write-Host " FAIL (HTTP $($response.StatusCode))" -ForegroundColor Red
    }
} catch {
    Write-Host " FAIL" -ForegroundColor Red
    Write-Host "  Error: $_" -ForegroundColor Red
    Write-Host "`n  Manual check required on server" -ForegroundColor Yellow
    Write-Host "  SSH: ssh $ServerUser@$ServerIP" -ForegroundColor Yellow
    Write-Host "  Logs: docker-compose -f $RemotePath/docker-compose.yml logs" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  DEPLOYMENT SUCCESSFUL!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Application URL: http://$ServerIP`:8080" -ForegroundColor Cyan
Write-Host "Health Check: $HealthCheckUrl" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Configure reverse proxy (Nginx/Traefik) for HTTPS" -ForegroundColor White
Write-Host "  2. Run smoke tests: .\scripts\smoke-tests.ps1 -BaseUrl http://$ServerIP`:8080" -ForegroundColor White
Write-Host "  3. Monitor logs: ssh $ServerUser@$ServerIP 'docker-compose -f $RemotePath/docker-compose.yml logs -f'" -ForegroundColor White
Write-Host ""
Write-Host "Rollback command:" -ForegroundColor Yellow
Write-Host "  ssh $ServerUser@$ServerIP 'docker-compose -f $RemotePath/docker-compose.yml down && docker-compose up -d'" -ForegroundColor White
Write-Host ""
