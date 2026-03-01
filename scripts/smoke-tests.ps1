<#
.SYNOPSIS
Smoke tests for Control Peso Thiscloud production deployment

.DESCRIPTION
Runs basic smoke tests to verify critical functionality after deployment

.PARAMETER BaseUrl
Base URL of the application. Default: https://controlpeso.thiscloud.com.ar

.EXAMPLE
.\smoke-tests.ps1
Run all smoke tests against production

.EXAMPLE
.\smoke-tests.ps1 -BaseUrl "http://localhost:8080"
Run smoke tests against local Docker instance
#>

param(
    [string]$BaseUrl = "https://controlpeso.thiscloud.com.ar"
)

$ErrorActionPreference = "Continue"
$TestResults = @()

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [int]$ExpectedStatus = 200,
        [string]$ExpectedContent = $null
    )
    
    Write-Host "`nTesting: $Name" -ForegroundColor Cyan
    Write-Host "  URL: $Url" -ForegroundColor Gray
    
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 30 -MaximumRedirection 0 -ErrorAction Stop
        
        if ($response.StatusCode -eq $ExpectedStatus) {
            Write-Host "  Status: $($response.StatusCode) ✓" -ForegroundColor Green
            
            if ($ExpectedContent -and $response.Content -notlike "*$ExpectedContent*") {
                Write-Host "  Content: MISSING '$ExpectedContent' ✗" -ForegroundColor Red
                return $false
            } elseif ($ExpectedContent) {
                Write-Host "  Content: Contains '$ExpectedContent' ✓" -ForegroundColor Green
            }
            
            return $true
        } else {
            Write-Host "  Status: $($response.StatusCode) (expected $ExpectedStatus) ✗" -ForegroundColor Red
            return $false
        }
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.Value__
        if ($statusCode -eq $ExpectedStatus) {
            Write-Host "  Status: $statusCode ✓" -ForegroundColor Green
            return $true
        } else {
            Write-Host "  Status: $statusCode (expected $ExpectedStatus) ✗" -ForegroundColor Red
            Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
            return $false
        }
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SMOKE TESTS - Control Peso Thiscloud" -ForegroundColor Cyan
Write-Host "  Target: $BaseUrl" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Test 1: Health endpoint
$TestResults += @{
    Name = "Health Check"
    Result = Test-Endpoint -Name "Health Check" -Url "$BaseUrl/health" -ExpectedContent "healthy"
}

# Test 2: Home page (public)
$TestResults += @{
    Name = "Home Page"
    Result = Test-Endpoint -Name "Home Page (Public)" -Url "$BaseUrl/" -ExpectedContent "Control Peso Thiscloud"
}

# Test 3: Login page (should redirect to Google OAuth)
$TestResults += @{
    Name = "Login Page"
    Result = Test-Endpoint -Name "Login Page" -Url "$BaseUrl/login" -ExpectedStatus 200
}

# Test 4: Privacy page
$TestResults += @{
    Name = "Privacy Policy"
    Result = Test-Endpoint -Name "Privacy Policy" -Url "$BaseUrl/privacy" -ExpectedContent "Privacy"
}

# Test 5: Terms page
$TestResults += @{
    Name = "Terms & Conditions"
    Result = Test-Endpoint -Name "Terms & Conditions" -Url "$BaseUrl/terms" -ExpectedContent "Terms"
}

# Test 6: Licenses page
$TestResults += @{
    Name = "Third-Party Licenses"
    Result = Test-Endpoint -Name "Third-Party Licenses" -Url "$BaseUrl/licenses" -ExpectedContent "License"
}

# Test 7: Changelog page
$TestResults += @{
    Name = "Changelog"
    Result = Test-Endpoint -Name "Changelog" -Url "$BaseUrl/changelog" -ExpectedContent "Version"
}

# Test 8: Dashboard (requires auth - should redirect)
$TestResults += @{
    Name = "Dashboard (Auth Required)"
    Result = Test-Endpoint -Name "Dashboard (Auth Required)" -Url "$BaseUrl/dashboard" -ExpectedStatus 302
}

# Test 9: Profile (requires auth - should redirect)
$TestResults += @{
    Name = "Profile (Auth Required)"
    Result = Test-Endpoint -Name "Profile (Auth Required)" -Url "$BaseUrl/profile" -ExpectedStatus 302
}

# Test 10: Admin (requires admin role - should redirect)
$TestResults += @{
    Name = "Admin Panel (Admin Required)"
    Result = Test-Endpoint -Name "Admin Panel (Admin Required)" -Url "$BaseUrl/admin" -ExpectedStatus 302
}

# Test 11: Static files (logo)
$TestResults += @{
    Name = "Static Files (Logo)"
    Result = Test-Endpoint -Name "Static Files (Logo)" -Url "$BaseUrl/images/og-image.png" -ExpectedStatus 200
}

# Test 12: robots.txt
$TestResults += @{
    Name = "robots.txt"
    Result = Test-Endpoint -Name "robots.txt" -Url "$BaseUrl/robots.txt" -ExpectedContent "Sitemap"
}

# Test 13: sitemap.xml
$TestResults += @{
    Name = "sitemap.xml"
    Result = Test-Endpoint -Name "sitemap.xml" -Url "$BaseUrl/sitemap.xml" -ExpectedContent "<urlset"
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  TEST SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$passed = ($TestResults | Where-Object { $_.Result -eq $true } | Measure-Object).Count
$failed = ($TestResults | Where-Object { $_.Result -eq $false } | Measure-Object).Count
$total = $TestResults.Count

Write-Host "`nTotal Tests: $total" -ForegroundColor White
Write-Host "Passed: $passed" -ForegroundColor Green
Write-Host "Failed: $failed" -ForegroundColor Red
Write-Host "Success Rate: $([math]::Round(($passed / $total) * 100, 2))%" -ForegroundColor Cyan

if ($failed -eq 0) {
    Write-Host "`n✓ ALL TESTS PASSED" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n✗ SOME TESTS FAILED" -ForegroundColor Red
    Write-Host "`nFailed tests:" -ForegroundColor Red
    $TestResults | Where-Object { $_.Result -eq $false } | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor Red
    }
    exit 1
}
