# 🚀 Configuración de Puertos - Producción vs Desarrollo

## 📋 Resumen

| Entorno | Puerto Externo | Puerto Interno | Acceso |
|---------|----------------|----------------|---------|
| **Desarrollo** | 8080 | 8080 | http://localhost:8080 |
| **Pre-Producción** | 8080 | 8080 | http://localhost:8080 |
| **Producción** | 8083 | 8080 | Nginx → http://localhost:8083 |

---

## 🏠 Desarrollo Local (Actual)

### docker-compose.override.yml (Local)
```yaml
services:
  controlpeso-web:
    # Puerto directo sin proxy
    ports:
      - "8080:8080"
```

**Acceso directo**: http://localhost:8080

---

## 🌐 Producción con Nginx Proxy Manager

### 1. Cambiar Puerto en docker-compose.override.yml

**⚠️ IMPORTANTE**: Usar puerto externo diferente a 8080 para evitar conflictos.

```yaml
services:
  controlpeso-web:
    ports:
      - "8083:8080"  # Puerto externo 8083, interno 8080
```

**¿Por qué 8083?**
- Puerto 8080 puede estar ocupado por otros servicios
- Nginx Proxy Manager escuchará en 80/443
- Nginx redirigirá tráfico a `localhost:8083`

### 2. Configurar Nginx Proxy Manager

#### Crear Proxy Host

**Settings**:
- **Domain Names**: `controlpeso.thiscloud.com.ar`
- **Scheme**: `http`
- **Forward Hostname/IP**: `localhost` (si Nginx está en el mismo servidor)
  - O usar IP del servidor Docker: `10.0.0.100`
  - O usar nombre del host: `docker-host`
- **Forward Port**: `8083`
- **Block Common Exploits**: ✅
- **Websockets Support**: ✅ (CRÍTICO para Blazor Server)
- **Cache Assets**: ✅

#### SSL Configuration

- **Force SSL**: ✅
- **HTTP/2 Support**: ✅
- **HSTS Enabled**: ✅
- **HSTS Subdomains**: ✅
- **SSL Certificate**: Let's Encrypt
- **Email**: `marco.desantis@thiscloud.com`
- **Agree to Let's Encrypt ToS**: ✅

#### Advanced (Opcional)

```nginx
# Configuración adicional para Blazor Server
location / {
    proxy_pass http://localhost:8083;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
    
    # Timeouts para SignalR (Blazor Server)
    proxy_read_timeout 3600s;
    proxy_send_timeout 3600s;
}
```

---

## 🔄 Flujo de Tráfico

### Desarrollo Local
```
Usuario → http://localhost:8080 → Docker Container (8080)
```

### Producción
```
Usuario → HTTPS (443) → Nginx Proxy Manager (controlpeso.thiscloud.com.ar)
         ↓
         HTTP (8083) → Docker Container (8080)
```

**Ventajas**:
- Nginx maneja SSL/TLS (Let's Encrypt)
- Nginx maneja compresión gzip/brotli
- Nginx maneja cache de assets estáticos
- Nginx protege contra ataques comunes
- Blazor Server funciona correctamente con WebSockets

---

## 🔧 Configuración Adicional para Producción

### 1. AllowedHosts

Editar `docker-compose.override.yml`:

```yaml
services:
  controlpeso-web:
    environment:
      - AllowedHosts=controlpeso.thiscloud.com.ar
```

### 2. Google OAuth Redirect URIs

Agregar en Google Cloud Console:
```
https://controlpeso.thiscloud.com.ar/signin-google
```

### 3. Telegram Environment

```yaml
- Telegram__Environment=Production
```

---

## 📦 Ejemplo Completo: docker-compose.override.yml (Producción)

```yaml
services:
  sqlserver:
    environment:
      - SA_PASSWORD=TuPasswordSeguro2026!
      - MSSQL_COLLATION=Modern_Spanish_CI_AS

  controlpeso-web:
    ports:
      - "8083:8080"  # Puerto externo para Nginx
    
    environment:
      # Database
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=ControlPeso;User Id=sa;Password=TuPasswordSeguro2026!;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False

      # Google OAuth
      - Authentication__Google__ClientId=180510012560-6a1l32rfl33pdk7q7aehbe8o06urbl0h.apps.googleusercontent.com
      - Authentication__Google__ClientSecret=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

      # Telegram
      - Telegram__Enabled=true
      - Telegram__BotToken=7928521075:AAGhzDZ1L04NWpSs-vB2rTU76Bu7U-T8kak
      - Telegram__ChatId=8393180247
      - Telegram__Environment=Production

      # Production Settings
      - AllowedHosts=controlpeso.thiscloud.com.ar
      - ASPNETCORE_ENVIRONMENT=Production
```

---

## ✅ Checklist de Despliegue a Producción

- [ ] Cambiar puerto en `docker-compose.override.yml` a `8083:8080`
- [ ] Configurar Nginx Proxy Host con dominio `controlpeso.thiscloud.com.ar`
- [ ] Habilitar SSL en Nginx (Let's Encrypt)
- [ ] Agregar redirect URI en Google OAuth: `https://controlpeso.thiscloud.com.ar/signin-google`
- [ ] Verificar `AllowedHosts=controlpeso.thiscloud.com.ar`
- [ ] Verificar `Telegram__Environment=Production`
- [ ] Probar acceso: https://controlpeso.thiscloud.com.ar
- [ ] Verificar Google OAuth funciona
- [ ] Verificar Telegram notificaciones
- [ ] Verificar SignalR/Blazor Server funciona (navegación, tiempo real)

---

## 🔥 Troubleshooting

### Error: "502 Bad Gateway" en Nginx

**Causa**: Puerto incorrecto en Forward Port

**Solución**:
```
Nginx Proxy Manager → Edit Proxy Host → Forward Port: 8083
```

### Error: "WebSocket connection failed"

**Causa**: Websockets no habilitado en Nginx

**Solución**:
```
Nginx Proxy Manager → Edit Proxy Host → ✅ Websockets Support
```

### Error: "redirect_uri_mismatch" en Google OAuth

**Causa**: URL no agregada en Google Cloud Console

**Solución**:
```
https://console.cloud.google.com/apis/credentials
→ Select OAuth Client
→ Authorized redirect URIs
→ Add: https://controlpeso.thiscloud.com.ar/signin-google
```

---

## 📚 Referencias

- **Nginx Proxy Manager**: https://nginxproxymanager.com/
- **Let's Encrypt**: https://letsencrypt.org/
- **Google OAuth**: https://console.cloud.google.com/apis/credentials
- **Blazor SignalR**: https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/signalr

---

**Última actualización**: 2025-03-01  
**Versión**: 3.0
