# Configuración de Google OAuth 2.0

## Pasos para Obtener ClientId y ClientSecret

### 1. Ir a Google Cloud Console
👉 **https://console.cloud.google.com**

### 2. Crear o Seleccionar Proyecto
- Si no tenés proyecto: **"New Project"** → Nombre: "Control Peso Thiscloud"
- Si ya existe: Seleccioná el proyecto en el dropdown superior

### 3. Habilitar Google+ API
1. Ir a **"APIs & Services"** → **"Library"**
2. Buscar: **"Google+ API"** o **"People API"**
3. Click en **"Enable"**

### 4. Configurar OAuth Consent Screen
1. Ir a **"APIs & Services"** → **"OAuth consent screen"**
2. Seleccionar **"External"** (para testing) o **"Internal"** (si tenés Google Workspace)
3. Click **"Create"**

**Configuración básica**:
- **App name**: Control Peso Thiscloud
- **User support email**: tu-email@gmail.com
- **Developer contact**: tu-email@gmail.com
- Click **"Save and Continue"**

**Scopes** (permisos):
- Click **"Add or Remove Scopes"**
- Seleccionar:
  - ✅ `email`
  - ✅ `profile`
  - ✅ `openid`
- Click **"Update"** → **"Save and Continue"**

**Test users** (solo si seleccionaste "External"):
- Click **"Add Users"**
- Agregar tu email de Gmail para testing
- Click **"Save and Continue"**

### 5. Crear Credenciales OAuth 2.0
1. Ir a **"APIs & Services"** → **"Credentials"**
2. Click **"+ CREATE CREDENTIALS"** → **"OAuth client ID"**
3. **Application type**: **Web application**
4. **Name**: Control Peso Thiscloud Web

**Authorized JavaScript origins** (URLs desde donde se puede llamar):
```
http://localhost:5000
https://localhost:5001
```

**Authorized redirect URIs** (callback después del login):
```
http://localhost:5000/signin-google
https://localhost:5001/signin-google
```

5. Click **"Create"**

### 6. Copiar Credenciales
Después de crear, verás un popup con:
- ✅ **Client ID**: `123456789-abc.apps.googleusercontent.com`
- ✅ **Client Secret**: `GOCSPX-abc123xyz...`

**IMPORTANTE**: Copiar ambos valores ahora (también podés verlos después en "Credentials")

### 7. Pegar en appsettings.Development.json

Editá el archivo: `src/ControlPeso.Web/appsettings.Development.json`

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "PEGAR-TU-CLIENT-ID-AQUI.apps.googleusercontent.com",
      "ClientSecret": "PEGAR-TU-CLIENT-SECRET-AQUI"
    }
  }
}
```

### 8. Reiniciar la Aplicación
```bash
# Detener la app (Ctrl+C)
# Volver a ejecutar
dotnet run --project src/ControlPeso.Web
```

### 9. Probar Login
1. Abrir: **http://localhost:5000/login**
2. Click en **"Iniciar sesión con Google"**
3. Deberías ver el popup de Google para autorizar
4. Después del login → redirige a `/dashboard`

---

## Troubleshooting

### ❌ Error: "invalid_client" o "redirect_uri_mismatch"

**CAUSA RAÍZ**: Las URIs de redirección en Google Cloud Console NO coinciden con las URLs de tu aplicación.

**🔍 CÓMO VERIFICAR TU PROBLEMA**:

Tu aplicación corre en los puertos especificados en `src/ControlPeso.Web/Properties/launchSettings.json`:
- **HTTPS**: `https://localhost:7065`
- **HTTP**: `http://localhost:5212`

**✅ SOLUCIÓN PASO A PASO**:

1. **Ir a Google Cloud Console**:
   - 👉 https://console.cloud.google.com/apis/credentials
   - Seleccionar tu proyecto

2. **Encontrar tu OAuth 2.0 Client ID**:
   - Buscar el Client ID que estás usando: `180510012560-6a1l32rfl33pdk7q7aehbe8o06urbl0h.apps.googleusercontent.com`
   - Click en el ícono ✏️ (editar)

3. **Actualizar Authorized redirect URIs**:
   - Agregar EXACTAMENTE estas URIs (copiar/pegar):
     ```
     https://localhost:7065/signin-google
     http://localhost:5212/signin-google
     ```
   - **IMPORTANTE**: Verificar que NO haya espacios antes/después
   - Click **"SAVE"**

4. **Esperar propagación de cambios**:
   - Google tarda **1-2 minutos** en propagar los cambios
   - ☕ Esperar antes de probar

5. **Reiniciar la aplicación**:
   - En Visual Studio: Detener (Shift+F5) y volver a iniciar (F5)
   - O en terminal: `Ctrl+C` y `dotnet run --project src/ControlPeso.Web`

6. **Probar login nuevamente**:
   - Ir a: `https://localhost:7065/login`
   - Click en "Iniciar sesión con Google"
   - Debería funcionar ahora ✅

**⚠️ NOTAS IMPORTANTES**:
- Las URIs deben coincidir EXACTAMENTE (puerto + ruta)
- Si cambiás el puerto en `launchSettings.json`, debés actualizar Google Cloud Console
- Los cambios en Google pueden tardar hasta 5 minutos en aplicarse

### Error: "Access blocked: This app's request is invalid"
**Causa**: OAuth consent screen no configurado o incompleto

**Solución**:
1. Completar **OAuth consent screen** con todos los campos
2. Agregar tu email en **Test users**
3. Guardar y esperar 5 minutos

### Error: "This app isn't verified"
**Causa**: Normal en modo desarrollo con "External"

**Solución**:
- Click en **"Advanced"** → **"Go to Control Peso (unsafe)"**
- Esto solo aparece en testing, usuarios de prueba pueden acceder

### Error: ClientId/ClientSecret incorrectos
**Síntomas**: App no inicia login, error en logs

**Solución**:
1. Verificar que copiaste ClientId completo (termina en `.apps.googleusercontent.com`)
2. Verificar que copiaste ClientSecret completo (empieza con `GOCSPX-`)
3. Sin espacios extras, sin comillas extras en JSON

---

## Producción

Para deployment a producción:

1. **Cambiar OAuth consent screen a "In Production"** (requiere verificación de Google si es "External")

2. **Agregar dominio de producción** en Authorized URIs:
```
https://controlpeso.thiscloud.com
https://controlpeso.thiscloud.com/signin-google
```

3. **Usar variables de entorno** en vez de appsettings:
```bash
export Authentication__Google__ClientId="..."
export Authentication__Google__ClientSecret="..."
```

4. **O usar User Secrets / Azure Key Vault** para secretos

---

## Referencias
- 📚 **Google OAuth 2.0 Docs**: https://developers.google.com/identity/protocols/oauth2
- 📚 **ASP.NET Core Google Auth**: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins
