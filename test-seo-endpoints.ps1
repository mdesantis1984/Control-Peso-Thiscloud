# SEO Endpoints Testing Script
# Tests sitemap.xml and robots.txt dynamic generation
# Run: pwsh test-seo-endpoints.ps1

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   SEO ENDPOINTS TESTING - Control Peso    " -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$BaseUrl = "https://localhost:7143"
$ProductionUrl = "https://controlpeso.thiscloud.com.ar"

# Determine which URL to test
$TestUrl = $BaseUrl
if ($args.Count -gt 0 -and $args[0] -eq "production") {
    $TestUrl = $ProductionUrl
    Write-Host "Testing PRODUCTION environment: $TestUrl" -ForegroundColor Yellow
} else {
    Write-Host "Testing DEVELOPMENT environment: $TestUrl" -ForegroundColor Green
    Write-Host "(Use 'pwsh test-seo-endpoints.ps1 production' to test production)" -ForegroundColor Gray
}

Write-Host ""

# Function to test endpoint
function Test-SeoEndpoint {
    param(
        [string]$Url,
        [string]$Name,
        [string]$ExpectedContentType
    )
    
    Write-Host "Testing $Name..." -ForegroundColor Yellow
    Write-Host "  URL: $Url" -ForegroundColor Gray
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method GET -SkipCertificateCheck -ErrorAction Stop
        
        # Status Code
        if ($response.StatusCode -eq 200) {
            Write-Host "  ✅ Status: $($response.StatusCode) OK" -ForegroundColor Green
        } else {
            Write-Host "  ❌ Status: $($response.StatusCode) (expected 200)" -ForegroundColor Red
            return $false
        }
        
        # Content-Type
        $contentType = $response.Headers['Content-Type']
        if ($contentType -like "*$ExpectedContentType*") {
            Write-Host "  ✅ Content-Type: $contentType" -ForegroundColor Green
        } else {
            Write-Host "  ❌ Content-Type: $contentType (expected $ExpectedContentType)" -ForegroundColor Red
            return $false
        }
        
        # Cache headers
        $cacheControl = $response.Headers['Cache-Control']
        if ($cacheControl) {
            Write-Host "  ✅ Cache-Control: $cacheControl" -ForegroundColor Green
        } else {
            Write-Host "  ⚠️  Cache-Control: Not set" -ForegroundColor Yellow
        }
        
        # Content Length
        $contentLength = $response.Content.Length
        Write-Host "  ✅ Content Length: $contentLength bytes" -ForegroundColor Green
        
        # Preview content
        $preview = $response.Content.Substring(0, [Math]::Min(200, $contentLength))
        Write-Host "  📄 Preview:" -ForegroundColor Cyan
        Write-Host "     $($preview.Replace("`r`n", " "))" -ForegroundColor Gray
        
        Write-Host ""
        return $true
        
    } catch {
        Write-Host "  ❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        return $false
    }
}

# Function to validate sitemap XML structure
function Test-SitemapStructure {
    param([string]$Url)
    
    Write-Host "Validating Sitemap XML structure..." -ForegroundColor Yellow
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method GET -SkipCertificateCheck -ErrorAction Stop
        [xml]$xml = $response.Content
        
        # Check root element
        if ($xml.DocumentElement.LocalName -eq "urlset") {
            Write-Host "  ✅ Root element: <urlset>" -ForegroundColor Green
        } else {
            Write-Host "  ❌ Root element: $($xml.DocumentElement.LocalName) (expected <urlset>)" -ForegroundColor Red
            return $false
        }
        
        # Count URLs
        $urls = $xml.urlset.url
        $urlCount = $urls.Count
        Write-Host "  ✅ URL count: $urlCount" -ForegroundColor Green
        
        # Validate each URL
        $expectedUrls = @(
            "/",
            "/login",
            "/privacy",
            "/privacidad",
            "/terms",
            "/terminos",
            "/changelog",
            "/historial"
        )
        
        foreach ($expectedPath in $expectedUrls) {
            $found = $urls | Where-Object { $_.loc -like "*$expectedPath" }
            if ($found) {
                $priority = $found.priority
                $changefreq = $found.changefreq
                $lastmod = $found.lastmod
                Write-Host "  ✅ Found: $expectedPath (priority: $priority, changefreq: $changefreq, lastmod: $lastmod)" -ForegroundColor Green
            } else {
                Write-Host "  ❌ Missing: $expectedPath" -ForegroundColor Red
            }
        }
        
        Write-Host ""
        return $true
        
    } catch {
        Write-Host "  ❌ XML Parse Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        return $false
    }
}

# Function to validate robots.txt structure
function Test-RobotsStructure {
    param([string]$Url)
    
    Write-Host "Validating Robots.txt structure..." -ForegroundColor Yellow
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method GET -SkipCertificateCheck -ErrorAction Stop
        $content = $response.Content
        
        # Check for AI crawler support
        $aiCrawlers = @(
            "GPTBot",
            "ChatGPT-User",
            "Claude-Web",
            "Google-Extended",
            "FacebookBot",
            "CCBot",
            "PerplexityBot",
            "Applebot-Extended",
            "cohere-ai"
        )
        
        $foundCount = 0
        foreach ($crawler in $aiCrawlers) {
            if ($content -like "*User-agent: $crawler*") {
                Write-Host "  ✅ AI Crawler: $crawler" -ForegroundColor Green
                $foundCount++
            } else {
                Write-Host "  ⚠️  AI Crawler: $crawler (not found)" -ForegroundColor Yellow
            }
        }
        
        if ($foundCount -eq 0) {
            Write-Host "  ❌ No AI crawlers found in robots.txt!" -ForegroundColor Red
            return $false
        }
        
        # Check for sitemap reference
        if ($content -like "*Sitemap:*sitemap.xml*") {
            Write-Host "  ✅ Sitemap reference found" -ForegroundColor Green
        } else {
            Write-Host "  ❌ Sitemap reference missing" -ForegroundColor Red
            return $false
        }
        
        # Check for blocked paths
        $blockedPaths = @("/dashboard", "/profile", "/admin", "/api/")
        foreach ($path in $blockedPaths) {
            if ($content -like "*Disallow: $path*") {
                Write-Host "  ✅ Blocked path: $path" -ForegroundColor Green
            } else {
                Write-Host "  ⚠️  Blocked path: $path (not explicitly listed)" -ForegroundColor Yellow
            }
        }
        
        Write-Host ""
        return $true
        
    } catch {
        Write-Host "  ❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        return $false
    }
}

# Run Tests
$results = @()

# Test 1: Sitemap.xml endpoint
$results += Test-SeoEndpoint `
    -Url "$TestUrl/sitemap.xml" `
    -Name "Sitemap.xml" `
    -ExpectedContentType "application/xml"

# Test 2: Robots.txt endpoint
$results += Test-SeoEndpoint `
    -Url "$TestUrl/robots.txt" `
    -Name "Robots.txt" `
    -ExpectedContentType "text/plain"

# Test 3: Sitemap structure validation
$results += Test-SitemapStructure -Url "$TestUrl/sitemap.xml"

# Test 4: Robots.txt structure validation
$results += Test-RobotsStructure -Url "$TestUrl/robots.txt"

# Summary
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "              TEST SUMMARY                  " -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$passedCount = ($results | Where-Object { $_ -eq $true }).Count
$totalCount = $results.Count
$failedCount = $totalCount - $passedCount

if ($passedCount -eq $totalCount) {
    Write-Host "✅ ALL TESTS PASSED ($passedCount/$totalCount)" -ForegroundColor Green
    exit 0
} else {
    Write-Host "❌ SOME TESTS FAILED ($failedCount failed, $passedCount passed, $totalCount total)" -ForegroundColor Red
    exit 1
}
