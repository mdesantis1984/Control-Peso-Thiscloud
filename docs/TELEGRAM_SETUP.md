# Configuración de Notificaciones Telegram - Control Peso Thiscloud

Este documento explica cómo configurar las notificaciones automáticas de errores críticos vía Telegram.

---

## ¿Por qué Telegram?

Cuando ocurre un error crítico en la aplicación:
- ✅ **Usuario final**: Ve página amigable con mensaje profesional (no stack traces técnicos)
- ✅ **Administrador**: Recibe notificación inmediata en Telegram con detalles completos
- ✅ **Debugging**: Trace ID permite correlacionar error en logs

---

## Requisitos Previos

- Cuenta de Telegram (gratuita)
- Acceso al bot @BotFather en Telegram

---

## Paso 1: Crear Bot de Telegram

1. Abrir Telegram y buscar **@BotFather**
2. Iniciar conversación: `/start`
3. Crear nuevo bot: `/newbot`
4. Seguir instrucciones:
   - **Bot name**: `Control Peso Thiscloud Bot` (o el nombre que desees)
   - **Bot username**: `controlpeso_thiscloud_bot` (debe terminar en `_bot`)
5. BotFather responderá con tu **Bot Token**: `1234567890:ABCdefGHIjklMNOpqrsTUVwxyz`

⚠️ **IMPORTANTE**: Guarda el token en un lugar seguro. NO lo compartas públicamente.

---

## Paso 2: Obtener Chat ID

Necesitas el ID del chat donde el bot enviará notificaciones.

### Opción A: Chat Privado con el Bot

1. Buscar tu bot en Telegram (por username: `@controlpeso_thiscloud_bot`)
2. Iniciar conversación: `/start`
3. Enviar cualquier mensaje al bot (ej: "Hola")
4. Abrir en navegador: `https://api.telegram.org/bot<TU_BOT_TOKEN>/getUpdates`
   - Reemplazar `<TU_BOT_TOKEN>` con el token del Paso 1
5. Buscar en la respuesta JSON el campo `"chat":{"id": 123456789}`
6. Copiar ese número (puede ser positivo o negativo)

Ejemplo de respuesta:
```json
{
  "ok": true,
  "result": [
    {
      "update_id": 123456,
      "message": {
        "message_id": 1,
        "from": {"id": 123456789, "is_bot": false, "first_name": "Tu Nombre"},
        "chat": {"id": 123456789, "first_name": "Tu Nombre", "type": "private"},
        "date": 1234567890,
        "text": "Hola"
      }
    }
  ]
}
```

En este caso, tu Chat ID es: **123456789**

### Opción B: Grupo de Telegram (Recomendado para equipos)

1. Crear grupo en Telegram
2. Agregar tu bot al grupo como miembro
3. Enviar un mensaje en el grupo (ej: "@controlpeso_thiscloud_bot hola")
4. Obtener Chat ID con getUpdates (mismo proceso que Opción A)
5. Chat ID de grupos es **negativo**: `-987654321`

---

## Paso 3: Configurar en Desarrollo (User Secrets)

Ejecutar en terminal desde carpeta `src/ControlPeso.Web/`:

```powershell
# Habilitar notificaciones
dotnet user-secrets set "Telegram:Enabled" "true"

# Configurar Bot Token
dotnet user-secrets set "Telegram:BotToken" "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"

# Configurar Chat ID
dotnet user-secrets set "Telegram:ChatId" "123456789"

# Ambiente (Development/Production)
dotnet user-secrets set "Telegram:Environment" "Development"
```

⚠️ **Reemplazar valores** con tu token y chat ID reales.

---

## Paso 4: Configurar en Producción (Variables de Entorno)

### Docker / Docker Compose

Agregar en `docker-compose.yml`:

```yaml
services:
  controlpeso-web:
    environment:
      - Telegram__Enabled=true
      - Telegram__BotToken=CHANGE_ME
      - Telegram__ChatId=123456789
      - Telegram__Environment=Production
```

O usar archivo `.env` (NO commitear):

```env
TELEGRAM_ENABLED=true
TELEGRAM_BOT_TOKEN=CHANGE_ME
TELEGRAM_CHAT_ID=123456789
TELEGRAM_ENVIRONMENT=Production
```

Y en `docker-compose.yml`:

```yaml
services:
  controlpeso-web:
    env_file:
      - .env
```

### Azure App Service

Azure Portal → Tu App Service → Configuration → Application settings:

- `Telegram:Enabled` = `true`
- `Telegram:BotToken` = `1234567890:ABCdefGHIjklMNOpqrsTUVwxyz`
- `Telegram:ChatId` = `123456789`
- `Telegram:Environment` = `Production`

### Kubernetes

Crear Secret:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: telegram-config
type: Opaque
stringData:
  botToken: "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"
  chatId: "123456789"
```

Y en Deployment:

```yaml
env:
  - name: Telegram__Enabled
    value: "true"
  - name: Telegram__BotToken
    valueFrom:
      secretKeyRef:
        name: telegram-config
        key: botToken
  - name: Telegram__ChatId
    valueFrom:
      secretKeyRef:
        name: telegram-config
        key: chatId
  - name: Telegram__Environment
    value: "Production"
```

---

## Paso 5: Verificar Configuración

### Verificar User Secrets

```powershell
dotnet user-secrets list --project src/ControlPeso.Web/
```

Deberías ver:

```
Telegram:BotToken = CHANGE_ME
Telegram:ChatId = 123456789
Telegram:Enabled = true
Telegram:Environment = Development
```

### Test Manual (Opcional)

Crear archivo `TestTelegramNotification.http` (VS Code con REST Client extension):

```http
### Test Telegram Bot API
GET https://api.telegram.org/bot<TU_BOT_TOKEN>/getMe

### Send Test Message
POST https://api.telegram.org/bot<TU_BOT_TOKEN>/sendMessage
Content-Type: application/json

{
  "chat_id": "<TU_CHAT_ID>",
  "text": "🚨 <b>TEST</b>: Notificación de prueba desde Control Peso Thiscloud",
  "parse_mode": "HTML"
}
```

Si el POST retorna `"ok": true`, la configuración es correcta.

---

## Paso 6: Probar Notificaciones en la App

### Opción A: Trigger Error Real

1. Correr app: `dotnet run --project src/ControlPeso.Web/`
2. Navegar a URL inválida: `https://localhost:7065/ruta-que-no-existe`
3. O provocar error en código temporalmente

### Opción B: Endpoint de Test (Solo Development)

Agregar temporalmente en `Program.cs` (SOLO para testing, eliminar después):

```csharp
// SOLO PARA TEST - ELIMINAR EN PRODUCTION
if (app.Environment.IsDevelopment())
{
    app.MapGet("/test-error", () =>
    {
        throw new InvalidOperationException("Test error for Telegram notification");
    });
}
```

Visitar: `https://localhost:7065/test-error`

---

## Formato de Notificación

Cuando ocurre un error, recibirás en Telegram:

```
🚨 ERROR CRÍTICO - Control Peso Thiscloud

🔍 Trace ID: 0HNJGG9JDL9B0:00000019
⏰ Timestamp: 2026-02-20 14:20:35 UTC
🌍 Ambiente: Development

📝 Mensaje:
Path: GET /signin-google
User: Anonymous

❌ Excepción: AuthenticationFailureException
The remote login operation failed with error...

⚠️ Inner Exception:
SQLite Error 1: 'no such column: u.LinkedInId'.

━━━━━━━━━━━━━━━━━━━━
🔗 Ver Logs Completos (enlace a dashboard)
```

---

## Deshabilitar Notificaciones

### Temporalmente (Development)

```powershell
dotnet user-secrets set "Telegram:Enabled" "false"
```

### Permanentemente

En `appsettings.json`:

```json
"Telegram": {
  "Enabled": false,
  ...
}
```

Cuando `Enabled = false`, el servicio loguea pero NO envía mensajes a Telegram.

---

## Troubleshooting

### Error: "Telegram notifications are disabled"

✅ **Solución**: Verificar que `Telegram:Enabled = true` en configuración.

### Error: "BotToken is not configured"

✅ **Solución**: Verificar user secrets o variables de entorno.

### Error: "ChatId is not configured"

✅ **Solución**: Verificar user secrets o variables de entorno.

### Error: "Telegram API returned 401 Unauthorized"

✅ **Solución**: Bot Token inválido. Verificar token en @BotFather.

### Error: "Telegram API returned 400 Bad Request - chat not found"

✅ **Solución**: 
1. Chat ID incorrecto
2. Si es grupo: Verificar que bot está agregado como miembro
3. Si es chat privado: Enviar mensaje al bot primero

### Error: "Failed to send Telegram notification"

✅ **Solución**: Ver logs completos en `logs/controlpeso-*.ndjson` para detalles.

---

## Seguridad

### ✅ Buenas Prácticas

- **NUNCA** commitear tokens en repositorio Git
- Usar User Secrets (dev) o Secrets Manager (prod)
- Rotar tokens periódicamente
- Restringir acceso al chat de notificaciones

### ❌ Evitar

- Token en `appsettings.json` o `appsettings.Development.json` (visible en repo)
- Token en variables de entorno sin cifrar (servidores compartidos)
- Compartir token en canales inseguros (email, Slack público, etc.)

---

## Límites de Telegram Bot API

- **Rate Limit**: 30 mensajes/segundo por bot
- **Mensajes largos**: Máximo 4096 caracteres (TelegramNotificationService trunca automáticamente)
- **Grupos**: Máximo 200,000 miembros

Para esta app, los límites NO son problema (errores críticos = pocos mensajes por hora).

---

## Alternativas Futuras

Si necesitas más canales de notificación:

- **Email**: Implementar `EmailNotificationService : INotificationService`
- **Slack**: Implementar `SlackNotificationService : INotificationService`
- **Microsoft Teams**: Implementar `TeamsNotificationService : INotificationService`
- **SMS**: Implementar `SmsNotificationService : INotificationService` (Twilio)

La interfaz `INotificationService` permite agregar múltiples proveedores sin cambiar el middleware.

---

## Referencias

- Telegram Bot API Docs: https://core.telegram.org/bots/api
- @BotFather: https://t.me/botfather
- GetUpdates Endpoint: https://core.telegram.org/bots/api#getupdates
- SendMessage Endpoint: https://core.telegram.org/bots/api#sendmessage

---

**Última actualización**: 2026-02-20  
**Autor**: AI Assistant (Claude Sonnet 4.5)  
**Versión**: 1.0
