# 🔍 DIAGNÓSTICO: TELEGRAM NO FUNCIONA - SOLUCIÓN COMPLETA

## ❌ PROBLEMA IDENTIFICADO

Las notificaciones de Telegram **NO funcionan** porque las credenciales están configuradas con valores de ejemplo (placeholders):

```json
// ❌ CONFIGURACIÓN ACTUAL (appsettings.Development.json)
"Telegram": {
  "Enabled": true,
  "BotToken": "YOUR_TELEGRAM_BOT_TOKEN_HERE",  // ← PLACEHOLDER
  "ChatId": "YOUR_TELEGRAM_CHAT_ID_HERE"        // ← PLACEHOLDER
}
```

**Evidencia de los logs:**
- `TelegramNotificationService.cs` línea 44-47: Detecta BotToken no configurado
- `TelegramNotificationService.cs` línea 49-52: Detecta ChatId no configurado
- Loguea: `"Telegram BotToken is not configured - cannot send notification"`
- Resultado: **NO se envían mensajes**

---

## ✅ SOLUCIÓN COMPLETA

### 🚀 OPCIÓN 1: PANEL DE DIAGNÓSTICO (MÁS FÁCIL)

1. **Iniciar aplicación**:
   ```powershell
   dotnet run --project src\ControlPeso.Web
   ```

2. **Navegar a página de diagnóstico**:
   ```
   http://localhost:7065/diagnostics/telegram
   ```
   (También accesible desde menú: **Administración** → **Diagnóstico Telegram**)

3. **Seguir instrucciones en pantalla**:
   - La página muestra el estado actual de la configuración
   - Indica qué valores faltan
   - Proporciona guía paso a paso
   - Permite probar con botón **"Enviar Mensaje de Prueba"**

4. **Después de configurar, hacer clic en "Enviar Mensaje de Prueba"**:
   - ✅ Si funciona: Recibirás mensaje en Telegram
   - ❌ Si falla: Muestra error específico con causa

---

### 🛠️ OPCIÓN 2: SCRIPT AUTOMÁTICO

```powershell
# Ejecutar desde la raíz del proyecto:
.\scripts\configure-telegram.ps1
```

El script:
1. Te guía para crear bot con @BotFather
2. Obtiene el Chat ID automáticamente
3. Actualiza `appsettings.Development.json`
4. **Envía mensaje de prueba para confirmar**
5. Te muestra si funciona o no

---

### 📝 OPCIÓN 3: MANUAL (5 PASOS)

#### **Paso 1: Crear Bot (2 minutos)**
```
1. Telegram → Buscar: @BotFather
2. Enviar: /newbot
3. Nombre: Control Peso Thiscloud Bot
4. Username: controlpeso_thiscloud_bot
5. COPIAR el token (ejemplo: 1234567890:ABCdef...)
```

#### **Paso 2: Obtener Chat ID (2 minutos)**
```
1. Buscar tu bot (@controlpeso_thiscloud_bot)
2. Enviar: /start
3. Enviar: Hola
4. Abrir: https://api.telegram.org/bot<TU_TOKEN>/getUpdates
5. Buscar: "chat":{"id": 123456789}
6. COPIAR el número
```

#### **Paso 3: Actualizar Configuración (1 minuto)**

Editar `src\ControlPeso.Web\appsettings.Development.json`:

```json
{
  "Telegram": {
    "Enabled": true,
    "BotToken": "1234567890:ABCdef...",  // ← Pegar token del Paso 1
    "ChatId": "123456789",                // ← Pegar Chat ID del Paso 2
    "Environment": "Development"
  }
}
```

#### **Paso 4: Reiniciar**
```powershell
# Detener (Ctrl+C) y volver a iniciar:
dotnet run --project src\ControlPeso.Web
```

#### **Paso 5: Probar**
```
http://localhost:7065/nonexistent
```

Deberías ver:
- ✅ Página de error amigable (navegador)
- ✅ Mensaje en Telegram con detalles

---

## 🔍 VERIFICAR CONFIGURACIÓN ACTUAL

```powershell
# Ver appsettings actuales:
Get-Content "src\ControlPeso.Web\appsettings.Development.json" | Select-String -Pattern "Telegram" -Context 5

# Resultado esperado ANTES de configurar:
# "BotToken": "YOUR_TELEGRAM_BOT_TOKEN_HERE",  ❌

# Resultado esperado DESPUÉS de configurar:
# "BotToken": "1234567890:ABCdef...",  ✅
```

---

## 📊 FLUJO DEL SISTEMA

```
Error en aplicación
    ↓
GlobalExceptionMiddleware (src/ControlPeso.Web/Middleware/GlobalExceptionMiddleware.cs)
    ↓ Captura excepción
    ↓ Loguea error con TraceId
    ↓ Llama a INotificationService.SendCriticalErrorAsync(...)
    ↓
TelegramNotificationService (src/ControlPeso.Web/Services/TelegramNotificationService.cs)
    ↓ Verifica _options.Enabled ✅
    ↓ Verifica _options.BotToken ❌ "YOUR_TELEGRAM_BOT_TOKEN_HERE"
    ↓ Loguea: "Telegram BotToken is not configured"
    ↓ return; (NO ENVÍA NADA)
```

**Después de configurar correctamente:**

```
Error en aplicación
    ↓
GlobalExceptionMiddleware
    ↓ Captura excepción
    ↓
TelegramNotificationService
    ↓ Verifica Enabled ✅
    ↓ Verifica BotToken ✅ (real)
    ↓ Verifica ChatId ✅ (real)
    ↓ BuildErrorMessage(...)
    ↓ SendTelegramMessageAsync(...)
    ↓ POST https://api.telegram.org/bot<TOKEN>/sendMessage
    ↓ ✅ Mensaje enviado
    ↓ Usuario recibe notificación en Telegram 🎉
```

---

## 🐛 ERRORES COMUNES

### 1. "Telegram BotToken is not configured"
**Causa**: BotToken sigue siendo `"YOUR_TELEGRAM_BOT_TOKEN_HERE"`
**Solución**: Seguir Paso 1-3 Manual arriba

### 2. "Telegram ChatId is not configured"
**Causa**: ChatId sigue siendo `"YOUR_TELEGRAM_CHAT_ID_HERE"`
**Solución**: Seguir Paso 2-3 Manual arriba

### 3. "Chat not found"
**Causa**: No enviaste `/start` al bot antes de obtener Chat ID
**Solución**:
```
1. Telegram → Buscar tu bot
2. Enviar: /start
3. Enviar: Hola
4. Repetir obtención de Chat ID (Paso 2)
```

### 4. "Unauthorized"
**Causa**: BotToken incorrecto o incompleto
**Solución**:
- Verificar que copiaste el token COMPLETO de @BotFather
- Incluye el número inicial Y el texto después de `:` (ejemplo: `1234567890:ABCdef...`)

### 5. No llegan mensajes después de configurar
**Causa**: Aplicación no reiniciada
**Solución**:
```powershell
# Detener aplicación (Ctrl+C)
dotnet run --project src\ControlPeso.Web
```

### 6. Mensaje de prueba funciona pero errores reales NO
**Causa**: Error ocurre ANTES de que response.HasStarted sea false
**Solución**: Verificar logs para ver si GlobalExceptionMiddleware captura la excepción:
```powershell
# Buscar en consola:
# [ERR] Unhandled exception occurred - Path: ...
```

---

## ✅ CONFIRMAR QUE FUNCIONA

### Logs esperados en consola:
```
[INF] Sending critical error notification to Telegram - TraceId: 0HN...
[INF] Critical error notification sent successfully to Telegram - TraceId: 0HN...
```

### Mensaje esperado en Telegram:
```
🚨 ERROR CRÍTICO - Control Peso Thiscloud

🔍 Trace ID: 0HN1234567890
⏰ Timestamp: 2026-02-21 04:15:32 UTC
🌍 Ambiente: Development

📝 Mensaje:
Path: GET /nonexistent
User: Anonymous

❌ Excepción: NotFoundException
404 Not Found

━━━━━━━━━━━━━━━━━━━━
🔗 Ver Logs Completos
```

---

## 🎯 RESUMEN EJECUTIVO

| Item | Estado | Acción Requerida |
|------|--------|------------------|
| **GlobalExceptionHandler** | ✅ HABILITADO | Ninguna (ya corregido) |
| **Telegram Services** | ✅ REGISTRADOS | Ninguna (ya configurado) |
| **BotToken** | ❌ PLACEHOLDER | **CONFIGURAR (ver arriba)** |
| **ChatId** | ❌ PLACEHOLDER | **CONFIGURAR (ver arriba)** |

**Próximo paso**: Elegir OPCIÓN 1, 2 o 3 y configurar credenciales.

---

## 📚 DOCUMENTACIÓN ADICIONAL

- **Guía rápida**: `docs/TELEGRAM_QUICKSTART.md`
- **Guía completa**: `docs/TELEGRAM_SETUP.md`
- **Panel diagnóstico**: http://localhost:7065/diagnostics/telegram
- **Script automático**: `scripts/configure-telegram.ps1`

---

## 🆘 NECESITAS AYUDA?

1. **Usar panel de diagnóstico**: http://localhost:7065/diagnostics/telegram
   - Muestra qué falta configurar
   - Permite probar envío
   - Da feedback inmediato

2. **Ejecutar script**: `.\scripts\configure-telegram.ps1`
   - Proceso guiado paso a paso
   - Prueba automática al final

3. **Revisar logs**: La aplicación loguea TODO lo que pasa con Telegram
   - `[INF]` si funciona
   - `[WRN]` si está deshabilitado
   - `[ERR]` si hay error de configuración

---

**Estado actual**: GlobalExceptionHandler ✅ habilitado | Telegram ❌ sin credenciales
**Bloqueo**: Credenciales de Telegram (BotToken + ChatId)
**Tiempo estimado**: 5-10 minutos para configurar
