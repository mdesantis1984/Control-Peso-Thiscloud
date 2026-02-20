# GUÍA RÁPIDA: CONFIGURAR TELEGRAM EN 5 MINUTOS

## 🚨 PROBLEMA ACTUAL
Las credenciales de Telegram están configuradas con valores de ejemplo:
```json
"BotToken": "YOUR_TELEGRAM_BOT_TOKEN_HERE",  ❌ Placeholder
"ChatId": "YOUR_TELEGRAM_CHAT_ID_HERE"       ❌ Placeholder
```

## ✅ SOLUCIÓN RÁPIDA

### OPCIÓN 1: Script Automático (RECOMENDADO)
```powershell
# Ejecutar desde la raíz del proyecto:
.\scripts\configure-telegram.ps1
```

El script te guiará paso a paso y:
- ✅ Te ayuda a crear el bot
- ✅ Obtiene el Chat ID automáticamente
- ✅ Actualiza appsettings.Development.json
- ✅ Envía mensaje de prueba

---

### OPCIÓN 2: Configuración Manual (5 pasos)

#### 1️⃣ Crear Bot (2 minutos)
```
1. Abrir Telegram → Buscar: @BotFather
2. Enviar: /newbot
3. Nombre: Control Peso Thiscloud Bot
4. Username: controlpeso_thiscloud_bot
5. COPIAR el TOKEN que te da (ejemplo: 1234567890:ABCdef...)
```

#### 2️⃣ Obtener Chat ID (2 minutos)
```
1. Buscar tu bot en Telegram (@controlpeso_thiscloud_bot)
2. Enviar: /start
3. Enviar: Hola
4. Abrir en navegador:
   https://api.telegram.org/bot<TU_TOKEN>/getUpdates
   (Reemplazar <TU_TOKEN> con el token del paso 1)
5. Buscar en el JSON: "chat":{"id": 123456789}
6. COPIAR ese número (tu Chat ID)
```

#### 3️⃣ Editar appsettings.Development.json (1 minuto)
```json
{
  "Telegram": {
    "Enabled": true,
    "BotToken": "PEGAR_TU_TOKEN_AQUÍ",      ← Paso 1
    "ChatId": "PEGAR_TU_CHATID_AQUÍ",       ← Paso 2
    "Environment": "Development"
  }
}
```

**Ejemplo con valores reales:**
```json
{
  "Telegram": {
    "Enabled": true,
    "BotToken": "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz",
    "ChatId": "123456789",
    "Environment": "Development"
  }
}
```

#### 4️⃣ Reiniciar Aplicación
```powershell
# Detener (Ctrl+C) y volver a iniciar:
dotnet run --project src\ControlPeso.Web
```

#### 5️⃣ Probar Notificaciones
```
1. Con la app corriendo, navegar a:
   http://localhost:7065/nonexistent
   
2. Deberías ver:
   ✅ Página de error amigable en el navegador
   ✅ Mensaje en Telegram con detalles del error
```

---

## 🔍 VERIFICAR CONFIGURACIÓN ACTUAL

```powershell
# Ver configuración actual:
Get-Content "src\ControlPeso.Web\appsettings.Development.json" | Select-String -Pattern "Telegram" -Context 5
```

**Si ves esto, NECESITAS configurar:**
```json
"BotToken": "YOUR_TELEGRAM_BOT_TOKEN_HERE",  ❌ Placeholder
"ChatId": "YOUR_TELEGRAM_CHAT_ID_HERE"       ❌ Placeholder
```

**Debe verse así después de configurar:**
```json
"BotToken": "1234567890:ABCdef...",  ✅ Token real
"ChatId": "123456789"                 ✅ Chat ID real
```

---

## ⚠️ ERRORES COMUNES

### Error: "Telegram BotToken is not configured"
**Causa:** BotToken sigue siendo "YOUR_TELEGRAM_BOT_TOKEN_HERE"
**Solución:** Seguir Paso 1-3 arriba

### Error: "Telegram ChatId is not configured"
**Causa:** ChatId sigue siendo "YOUR_TELEGRAM_CHAT_ID_HERE"
**Solución:** Seguir Paso 2-3 arriba

### Error: "Chat not found"
**Causa:** No enviaste /start al bot antes de obtener el Chat ID
**Solución:** 
1. Buscar tu bot en Telegram
2. Enviar /start
3. Enviar un mensaje cualquiera
4. Repetir Paso 2 (obtener Chat ID)

### Error: "Unauthorized"
**Causa:** BotToken incorrecto
**Solución:** Verificar que copiaste el token completo de BotFather

### No llegan mensajes
**Causa:** Aplicación no reiniciada después de configurar
**Solución:** Detener (Ctrl+C) y volver a ejecutar `dotnet run`

---

## 📊 VERIFICAR QUE FUNCIONA

### Logs esperados en consola:
```
[INF] Sending critical error notification to Telegram - TraceId: 0HN...
[INF] Critical error notification sent successfully to Telegram - TraceId: 0HN...
```

### Mensaje esperado en Telegram:
```
🚨 ERROR CRÍTICO - Control Peso Thiscloud

🔍 Trace ID: 0HN...
⏰ Timestamp: 2026-02-21 03:45:00 UTC
🌍 Ambiente: Development

📝 Mensaje:
Path: GET /nonexistent
User: Anonymous

❌ Excepción: NotFoundException
404 Not Found
```

---

## 🆘 NECESITAS AYUDA?

1. **Script automático no funciona:**
   - Seguir OPCIÓN 2 (Manual) arriba

2. **No sabes cómo crear bot:**
   - Ver video tutorial: https://core.telegram.org/bots#6-botfather

3. **Chat ID no aparece en JSON:**
   - Asegúrate de enviar /start al bot primero
   - Espera 10 segundos y refresca la URL getUpdates

4. **Errores en appsettings.json:**
   - Respetar comillas dobles en valores JSON
   - No dejar comas al final de la última propiedad

---

## 📚 DOCUMENTACIÓN COMPLETA
Ver: `docs/TELEGRAM_SETUP.md` para:
- Configuración de grupos
- Producción (Azure Key Vault, docker-compose)
- Troubleshooting avanzado
