# Google OAuth 2.0 Configuration for Production

## ⚠️ CRITICAL: Production Redirect URI

Para que Google OAuth funcione en producción, debes configurar la **redirect URI autorizada** en Google Cloud Console:

```
https://controlpeso.thiscloud.com.ar/signin-google
```

## Setup Steps

### 1. Google Cloud Console Configuration

1. Ir a: https://console.cloud.google.com/apis/credentials
2. Seleccionar proyecto existente o crear uno nuevo
3. Ir a **Credentials** → **Create Credentials** → **OAuth 2.0 Client ID**
4. Application type: **Web application**
5. Name: `ControlPeso Production`

#### Authorized JavaScript origins:
```
https://controlpeso.thiscloud.com.ar
```

#### Authorized redirect URIs:
```
https://controlpeso.thiscloud.com.ar/signin-google
```

**⚠️ IMPORTANTE:** La redirect URI debe ser EXACTAMENTE `https://controlpeso.thiscloud.com.ar/signin-google` (sin trailing slash, https obligatorio)

6. Click **Create**
7. Copiar **Client ID** y **Client secret**

### 2. Configure Secrets in Production

Editar `docker-compose.override.yml` en el servidor:

```yaml
services:
  controlpeso-web:
    environment:
      - Authentication__Google__ClientId=YOUR_CLIENT_ID_HERE.apps.googleusercontent.com
      - Authentication__Google__ClientSecret=GOCSPX-YOUR_CLIENT_SECRET_HERE
```

### 3. OAuth Consent Screen

1. Ir a **OAuth consent screen** en Google Cloud Console
2. User Type: **External** (si es público) o **Internal** (solo usuarios de tu organización)
3. Completar información de la app:
   - App name: `Control Peso Thiscloud`
   - User support email: tu email
   - Developer contact: tu email
4. Scopes requeridos:
   - `userinfo.email` (obligatorio)
   - `userinfo.profile` (obligatorio)
5. Test users (si está en modo Testing):
   - Agregar emails de usuarios que puedan hacer login
   - En producción, publicar la app (modo Production)

### 4. Verificación de Dominio (Opcional pero recomendado)

Para evitar warnings de "App no verificada":

1. Ir a **Domain verification** en Google Cloud Console
2. Agregar dominio: `controlpeso.thiscloud.com.ar`
3. Seguir pasos de verificación (DNS TXT record o archivo HTML)

## Forwarded Headers Configuration

La app está configurada para funcionar detrás de NPM Plus reverse proxy:

```csharp
// En Program.cs - ya configurado
builder.Services.AddForwardedHeadersConfiguration(builder.Environment);

// En pipeline
app.UseForwardedHeaders();
```

Esto asegura que:
- OAuth redirect URIs funcionen correctamente (https)
- Cookies tengan el scheme correcto
- HSTS funcione correctamente
- Logs registren la IP real del cliente

## NPM Plus Configuration

En NPM Plus (Nginx Proxy Manager):

1. **Proxy Host**:
   - Domain Names: `controlpeso.thiscloud.com.ar`
   - Scheme: `http`
   - Forward Hostname/IP: `10.0.0.100`
   - Forward Port: `8182`

2. **SSL**:
   - SSL Certificate: Let's Encrypt
   - Force SSL: ✅ ON
   - HTTP/2 Support: ✅ ON
   - HSTS Enabled: ✅ ON
   - HSTS Subdomains: ✅ ON

3. **Custom Nginx Configuration** (Advanced tab):
```nginx
# Forwarded headers for ASP.NET Core
proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
proxy_set_header X-Forwarded-Proto $scheme;
proxy_set_header X-Forwarded-Host $host;
proxy_set_header X-Real-IP $remote_addr;

# WebSocket support (Blazor SignalR)
proxy_http_version 1.1;
proxy_set_header Upgrade $http_upgrade;
proxy_set_header Connection "upgrade";

# Timeouts (adjust if needed for long operations)
proxy_connect_timeout 60s;
proxy_send_timeout 60s;
proxy_read_timeout 60s;
```

## Testing OAuth in Production

1. Navegar a: https://controlpeso.thiscloud.com.ar/login
2. Click en botón "Continuar con Google"
3. Seleccionar cuenta de Google
4. Verificar redirección correcta después de login
5. Verificar que el usuario aparezca en Dashboard

## Troubleshooting

### Error: redirect_uri_mismatch
**Causa:** La redirect URI no está autorizada en Google Cloud Console

**Solución:**
1. Verificar que `https://controlpeso.thiscloud.com.ar/signin-google` esté en **Authorized redirect URIs**
2. Verificar que NO tenga trailing slash
3. Verificar que sea **https** (no http)
4. Esperar 5-10 minutos para que Google propague los cambios

### Error: 400 Bad Request (Cookies disabled)
**Causa:** Cookies bloqueadas o esquema incorrecto (http en vez de https)

**Solución:**
1. Verificar que NPM Plus tenga Force SSL activado
2. Verificar que `UseForwardedHeaders()` esté en pipeline (ya configurado)
3. Verificar headers en DevTools: `X-Forwarded-Proto: https`

### Error: 500 Internal Server Error después de login
**Causa:** ClientSecret incorrecto o problema en DB

**Solución:**
1. Verificar logs: `docker logs controlpeso-web`
2. Verificar que SQL Server esté running: `docker ps`
3. Verificar ClientSecret en `docker-compose.override.yml`

## Security Checklist

✅ ClientId y ClientSecret en `docker-compose.override.yml` (NO en appsettings.json)
✅ `docker-compose.override.yml` en .gitignore (NO subir a Git)
✅ SSL habilitado en NPM Plus (Force SSL ON)
✅ Forwarded headers configurados
✅ Cookies con SecurePolicy.Always (solo HTTPS)
✅ HSTS habilitado
✅ Dominio verificado en Google (opcional pero recomendado)

## References

- Google OAuth 2.0 docs: https://developers.google.com/identity/protocols/oauth2
- ASP.NET Core Authentication: https://learn.microsoft.com/aspnet/core/security/authentication
- Forwarded Headers: https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer
