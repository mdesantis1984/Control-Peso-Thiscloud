# Script para corregir tests después de re-scaffold SQL Server Express
# Convierte tipos antiguos (string, double, int) a tipos nativos (Guid, DateTime, DateOnly, decimal, bool)

function Fix-TestFile {
    param([string]$filePath)
    
    if (!(Test-Path $filePath)) { 
        Write-Host "⚠️ File not found: $filePath"
        return 
    }
    
    Write-Host "🔧 Processing: $filePath"
    
    $content = Get-Content $filePath -Raw
    $originalContent = $content
    
    # 1. Guids: ToString() removal
    $content = $content -replace 'Guid\.NewGuid\(\)\.ToString\(\)', 'Guid.NewGuid()'
    $content = $content -replace 'Id = "([0-9a-f-]{36})"', 'Id = Guid.Parse("$1")'
    $content = $content -replace 'UserId = "([0-9a-f-]{36})"', 'UserId = Guid.Parse("$1")'
    
    # 2. DateTime: ToString() removal
    $content = $content -replace 'MemberSince = DateTime\.UtcNow\.ToString\("O"\)', 'MemberSince = DateTime.UtcNow'
    $content = $content -replace 'CreatedAt = DateTime\.UtcNow\.ToString\("O"\)', 'CreatedAt = DateTime.UtcNow'
    $content = $content -replace 'UpdatedAt = DateTime\.UtcNow\.ToString\("O"\)', 'UpdatedAt = DateTime.UtcNow'
    $content = $content -replace 'MemberSince = "(\d{4}-\d{2}-\d{2}T[^"]+)"', 'MemberSince = DateTime.Parse("$1")'
    $content = $content -replace 'CreatedAt = "(\d{4}-\d{2}-\d{2}T[^"]+)"', 'CreatedAt = DateTime.Parse("$1")'
    $content = $content -replace 'UpdatedAt = "(\d{4}-\d{2}-\d{2}T[^"]+)"', 'UpdatedAt = DateTime.Parse("$1")'
    
    # 3. DateOnly: new DateTime → new DateOnly
    $content = $content -replace 'DateOfBirth = new DateTime\((\d+), (\d+), (\d+)\)\.ToString\("yyyy-MM-dd"\)', 'DateOfBirth = new DateOnly($1, $2, $3)'
    $content = $content -replace 'DateOfBirth = "(\d{4})-(\d{2})-(\d{2})"', 'DateOfBirth = new DateOnly($1, $2, $3)'
    $content = $content -replace 'Date = "(\d{4})-(\d{2})-(\d{2})"', 'Date = new DateOnly($1, $2, $3)'
    
    # 4. TimeOnly
    $content = $content -replace 'Time = "(\d{2}):(\d{2})"', 'Time = new TimeOnly($1, $2)'
    
    # 5. Decimals: add 'm' suffix
    $content = $content -replace '(Height|Weight|GoalWeight|StartingWeight) = (\d+\.\d+)([,\s])', '$1 = $2m$3'
    $content = $content -replace 'Weight = (\d+\.\d+)([,\s])', 'Weight = $1m$2'
    
    # 6. Booleans: 0/1 → false/true
    $content = $content -replace 'DarkMode = 1', 'DarkMode = true'
    $content = $content -replace 'DarkMode = 0', 'DarkMode = false'
    $content = $content -replace 'NotificationsEnabled = 1', 'NotificationsEnabled = true'
    $content = $content -replace 'NotificationsEnabled = 0', 'NotificationsEnabled = false'
    $content = $content -replace 'IsRead = 1', 'IsRead = true'
    $content = $content -replace 'IsRead = 0', 'IsRead = false'
    
    # 7. Type enum → string (solo en entity initialization)
    $content = $content -replace 'Type = \(int\)NotificationSeverity\.(\w+)', 'Type = "$1"'
    
    # 8. ReadAt eliminado
    $content = $content -replace ',\s*ReadAt = null', ''
    $content = $content -replace 'ReadAt = null,', ''
    $content = $content -replace '\.ReadAt = [^;]+;', ' // ReadAt removed'
    
    # 9. Parse innecesarios (entity ya es tipo correcto)
    $content = $content -replace 'Guid\.Parse\(entity\.(Id|UserId|EntityId)\)', 'entity.$1'
    $content = $content -replace 'DateTime\.Parse\(entity\.(MemberSince|CreatedAt|UpdatedAt)\)', 'entity.$1'
    $content = $content -replace 'DateOnly\.Parse\(entity\.Date\)', 'entity.Date'
    $content = $content -replace 'DateOnly\.Parse\(entity\.DateOfBirth\)', 'entity.DateOfBirth'
    $content = $content -replace 'TimeOnly\.Parse\(entity\.Time\)', 'entity.Time'
    
    # 10. Comparaciones con .ToString()
    $content = $content -replace '\.Id == userId\.ToString\(\)', '.Id == userId'
    $content = $content -replace '\.UserId == userId\.ToString\(\)', '.UserId == userId'
    $content = $content -replace '\.Id == notificationId\.ToString\(\)', '.Id == notificationId'
    
    # 11. Guid.Parse cuando ya es Guid
    $content = $content -replace 'Guid\.Parse\((existingUser|existingLog|newUser|user)\.Id\)', '$1.Id'
    
    # 12. Assert.Equal con decimals
    $content = $content -replace 'Assert\.Equal\((\d+\.\d+), ([^,]+), precision', 'Assert.Equal($1m, $2, precision'
    
    # Solo escribir si hubo cambios
    if ($content -ne $originalContent) {
        $content | Set-Content $filePath -NoNewline
        Write-Host "   ✅ Changes applied"
    } else {
        Write-Host "   ⏭️ No changes needed"
    }
}

# Obtener todos los archivos .cs de tests (excepto generated)
$testFiles = Get-ChildItem -Path tests -Recurse -Filter *.cs | 
    Where-Object { 
        $_.Name -notlike '*AssemblyInfo*' -and 
        $_.Name -notlike '*.g.cs' -and
        $_.Name -notlike '*.Designer.cs'
    }

Write-Host "`n🚀 Fixing $($testFiles.Count) test files...`n"

foreach ($file in $testFiles) {
    Fix-TestFile -filePath $file.FullName
}

Write-Host "`n✅ ALL TEST FILES PROCESSED`n"
Write-Host "Running build to verify...`n"

# Build para verificar
$buildOutput = dotnet build 2>&1 | Out-String
$errorCount = ($buildOutput | Select-String "error CS" | Measure-Object).Count

if ($errorCount -eq 0) {
    Write-Host "🎉 BUILD SUCCESSFUL - 0 ERRORS!" -ForegroundColor Green
} else {
    Write-Host "⚠️ BUILD HAS $errorCount ERRORS - Manual fixes may be needed" -ForegroundColor Yellow
}
