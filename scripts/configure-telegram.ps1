# Script para configurar credenciales de Telegram en Control Peso Thiscloud
# Ejecutar desde la raíz del proyecto: .\scripts\configure-telegram.ps1

Write-Host "╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   CONFIGURACIÓN DE TELEGRAM - CONTROL PESO THISCLOUD             ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Paso 1: Instrucciones para crear el bot
Write-Host "📱 PASO 1: CREAR BOT DE TELEGRAM" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host ""
Write-Host "1. Abrir Telegram y buscar: " -NoNewline
Write-Host "@BotFather" -ForegroundColor Green
Write-Host "2. Enviar comando: " -NoNewline
Write-Host "/newbot" -ForegroundColor Green
Write-Host "3. Seguir instrucciones:" -ForegroundColor White
Write-Host "   - Nombre: " -NoNewline -ForegroundColor Gray
Write-Host "Control Peso Thiscloud Bot" -ForegroundColor Cyan
Write-Host "   - Username: " -NoNewline -ForegroundColor Gray
Write-Host "controlpeso_thiscloud_bot" -ForegroundColor Cyan
Write-Host ""
Write-Host "4. Copiar el TOKEN que te da BotFather" -ForegroundColor White
Write-Host "   (Ejemplo: 1234567890:ABCdefGHIjklMNOpqrsTUVwxyz)" -ForegroundColor DarkGray
Write-Host ""

# Solicitar Bot Token
$botToken = Read-Host "Pegar tu BOT TOKEN aquí"

if ([string]::IsNullOrWhiteSpace($botToken)) {
    Write-Host "❌ ERROR: Bot Token no puede estar vacío" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Bot Token recibido" -ForegroundColor Green
Write-Host ""

# Paso 2: Instrucciones para obtener Chat ID
Write-Host "🆔 PASO 2: OBTENER CHAT ID" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host ""
Write-Host "1. Buscar tu bot en Telegram (por username)" -ForegroundColor White
Write-Host "2. Enviar: " -NoNewline
Write-Host "/start" -ForegroundColor Green
Write-Host "3. Enviar cualquier mensaje (ej: 'Hola')" -ForegroundColor White
Write-Host ""
Write-Host "4. Abrir en navegador:" -ForegroundColor White
$getUpdatesUrl = "https://api.telegram.org/bot$botToken/getUpdates"
Write-Host "   $getUpdatesUrl" -ForegroundColor Cyan
Write-Host ""
Write-Host "5. Buscar en el JSON: " -NoNewline -ForegroundColor White
Write-Host '"chat":{"id": 123456789}' -ForegroundColor Yellow
Write-Host "6. Copiar ese número (tu Chat ID)" -ForegroundColor White
Write-Host ""

# Abrir navegador automáticamente
Write-Host "🌐 Abriendo navegador para obtener Chat ID..." -ForegroundColor Magenta
Start-Process $getUpdatesUrl

Write-Host ""
Write-Host "Presiona ENTER después de enviar mensajes al bot..." -ForegroundColor Yellow
Read-Host

# Solicitar Chat ID
$chatId = Read-Host "Pegar tu CHAT ID aquí"

if ([string]::IsNullOrWhiteSpace($chatId)) {
    Write-Host "❌ ERROR: Chat ID no puede estar vacío" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Chat ID recibido" -ForegroundColor Green
Write-Host ""

# Paso 3: Actualizar appsettings.Development.json
Write-Host "💾 PASO 3: ACTUALIZAR CONFIGURACIÓN" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host ""

$appsettingsPath = "src\ControlPeso.Web\appsettings.Development.json"

if (-not (Test-Path $appsettingsPath)) {
    Write-Host "❌ ERROR: No se encontró $appsettingsPath" -ForegroundColor Red
    exit 1
}

# Leer archivo
$json = Get-Content $appsettingsPath -Raw | ConvertFrom-Json

# Actualizar configuración de Telegram
$json.Telegram.BotToken = $botToken
$json.Telegram.ChatId = $chatId
$json.Telegram.Enabled = $true

# Guardar archivo
$json | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath -Encoding UTF8

Write-Host "✅ Configuración actualizada en: $appsettingsPath" -ForegroundColor Green
Write-Host ""

# Paso 4: Verificar configuración
Write-Host "🔍 PASO 4: VERIFICAR CONFIGURACIÓN" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Configuración actual:" -ForegroundColor White
Write-Host "  Enabled:  " -NoNewline -ForegroundColor Gray
Write-Host "true" -ForegroundColor Green
Write-Host "  BotToken: " -NoNewline -ForegroundColor Gray
Write-Host "$($botToken.Substring(0, 15))..." -ForegroundColor Cyan
Write-Host "  ChatId:   " -NoNewline -ForegroundColor Gray
Write-Host "$chatId" -ForegroundColor Cyan
Write-Host ""

# Paso 5: Enviar mensaje de prueba
Write-Host "📤 PASO 5: ENVIAR MENSAJE DE PRUEBA" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Enviando mensaje de prueba a Telegram..." -ForegroundColor Magenta

$testMessage = @"
✅ <b>TELEGRAM CONFIGURADO CORRECTAMENTE</b>

🎯 <b>Control Peso Thiscloud</b>
⏰ $(Get-Date -Format "yyyy-MM-dd HH:mm:ss") UTC
🌍 <b>Ambiente:</b> Development

📝 Las notificaciones de errores críticos están activas.

━━━━━━━━━━━━━━━━━━━━
🚀 Configuración completada exitosamente
"@

$payload = @{
    chat_id = $chatId
    text = $testMessage
    parse_mode = "HTML"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "https://api.telegram.org/bot$botToken/sendMessage" `
                                   -Method Post `
                                   -Body $payload `
                                   -ContentType "application/json"
    
    Write-Host "✅ ¡Mensaje de prueba enviado exitosamente!" -ForegroundColor Green
    Write-Host "   Revisa tu Telegram para verificar que llegó el mensaje." -ForegroundColor Cyan
    Write-Host ""
} catch {
    Write-Host "❌ ERROR al enviar mensaje de prueba:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "⚠️  Posibles causas:" -ForegroundColor Yellow
    Write-Host "   - Bot Token incorrecto" -ForegroundColor Gray
    Write-Host "   - Chat ID incorrecto" -ForegroundColor Gray
    Write-Host "   - No enviaste /start al bot antes de obtener el Chat ID" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🔄 Vuelve a ejecutar el script con las credenciales correctas" -ForegroundColor Yellow
    exit 1
}

# Resumen final
Write-Host "╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║                   ✅ CONFIGURACIÓN COMPLETA                       ║" -ForegroundColor Green
Write-Host "╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "🎉 Próximos pasos:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. " -NoNewline -ForegroundColor Gray
Write-Host "Reiniciar la aplicación:" -ForegroundColor White
Write-Host "   dotnet run --project src\ControlPeso.Web" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. " -NoNewline -ForegroundColor Gray
Write-Host "Probar notificaciones navegando a:" -ForegroundColor White
Write-Host "   http://localhost:5000/nonexistent" -ForegroundColor Cyan
Write-Host ""
Write-Host "3. " -NoNewline -ForegroundColor Gray
Write-Host "Deberías recibir un mensaje en Telegram con:" -ForegroundColor White
Write-Host "   🚨 ERROR CRÍTICO - Control Peso Thiscloud" -ForegroundColor Red
Write-Host "   🔍 Trace ID" -ForegroundColor Gray
Write-Host "   📝 Detalles del error" -ForegroundColor Gray
Write-Host ""
Write-Host "📚 Documentación completa: docs\TELEGRAM_SETUP.md" -ForegroundColor Magenta
Write-Host ""
