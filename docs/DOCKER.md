# Control Peso Thiscloud - Docker Deployment

> **Última Actualización**: 2026-02-19  
> **OAuth Status**: ✅ Google OAuth 2.0 FUNCIONAL | LinkedIn UI Removida (Backend Preservado)

Este documento describe cómo desplegar **Control Peso Thiscloud** localmente usando **Docker Compose**.

---

## 📋 Requisitos Previos

- **Docker** 20.10+ instalado ([Descargar Docker](https://www.docker.com/products/docker-desktop))
- **Docker Compose** v2.0+ (incluido con Docker Desktop)
- **Git** para clonar el repositorio
- **Credenciales OAuth de Google** (obligatorio)

---

## 🚀 Inicio Rápido

### 1. Clonar el repositorio

```bash
git clone https://github.com/mdesantis1984/Control-Peso-Thiscloud.git
cd Control-Peso-Thiscloud
```

### 2. Configurar credenciales OAuth (CRÍTICO)

⚠️ **IMPORTANTE**: Las credenciales sensibles NO están en el repositorio por seguridad.

Crea el archivo `docker-compose.override.yml` (NO commitear a Git):

```bash
# Windows PowerShell
Copy-Item docker-compose.override.yml.example docker-compose.override.yml

# Linux/macOS
cp docker-compose.override.yml.example docker-compose.override.yml
```

Edita `docker-compose.override.yml` con **tus credenciales reales de Google**:

```yaml
version: '3.8'
services:
  controlpeso-web:
    environment:
      # ✅ OBLIGATORIO: Google OAuth 2.0
      - Authentication__Google__ClientId=180510012560-EXAMPLE.apps.googleusercontent.com
      - Authentication__Google__ClientSecret=GOCSPX-EXAMPLE_SECRET

      # ⚠️ OPCIONAL: LinkedIn OAuth (UI removida, backend preservado)
      - Authentication__LinkedIn__ClientId=YOUR_LINKEDIN_CLIENT_ID
      - Authentication__LinkedIn__ClientSecret=YOUR_LINKEDIN_CLIENT_SECRET

      # 📊 OPCIONAL: Google Analytics 4
      - GoogleAnalytics__MeasurementId=G-XXXXXXXXXX

      # 📊 OPCIONAL: Cloudflare Analytics
      - CloudflareAnalytics__Token=your_token_here
```

🔒 **SEGURIDAD**:
- ✅ `docker-compose.override.yml` está en `.gitignore` (línea 408)
- ✅ El archivo `.example` tiene placeholders (seguro para Git)
- ❌ **NUNCA commitees `docker-compose.override.yml` al repositorio Git**
- ✅ El archivo persiste localmente (no se borra entre reinicios de Docker)

### 3. Construir y ejecutar

Docker Compose automáticamente combina:
- `docker-compose.yml` (configuración base pública)
- `docker-compose.override.yml` (tus credenciales privadas)

```bash
# Construir imagen y levantar contenedor
docker-compose up -d --build

# Ver logs en tiempo real
docker-compose logs -f controlpeso-web
```

### 4. Verificar despliegue

```bash
# 1. Verificar estado del contenedor
docker ps | grep controlpeso

# 2. Verificar credenciales OAuth dentro del contenedor
docker exec controlpeso-web printenv | grep Authentication__Google

# Deberías ver:
# Authentication__Google__ClientId=180510012560-...
# Authentication__Google__ClientSecret=GOCSPX-...

# 3. Verificar health endpoint
curl http://localhost:8080/health

# Respuesta esperada:
# {"status":"healthy","timestamp":"2026-02-19T...","version":"1.0.0"}

# 4. Probar login OAuth
# - Abrir http://localhost:8080/login
# - Clic en "Continuar con Google"
# - Autenticar con cuenta Google
# - Debería redirigir a http://localhost:8080/ (Dashboard)
```

### 5. Acceder a la aplicación

Abre tu navegador en:

**🌐 http://localhost:8080**

---

## 🔑 Obtener Credenciales OAuth

### Google OAuth 2.0 (OBLIGATORIO)

1. Ve a [Google Cloud Console](https://console.cloud.google.com/apis/credentials)
2. Crea un nuevo proyecto: `control-peso-thiscloud`
3. Habilita APIs:
   - **Google+ API**
   - **Google Identity Toolkit API**
4. Crea credenciales → **OAuth 2.0 Client ID**
5. Tipo de aplicación: **Aplicación web**
6. Nombre: `Control Peso Thiscloud (Development)`
7. URIs de redirección autorizadas:
   - **Desarrollo**: `http://localhost:8080/signin-google`
   - **Producción**: `https://controlpeso.thiscloud.com.ar/signin-google`
8. Copia el **Client ID** y **Client Secret** a `docker-compose.override.yml`
9. Configura la pantalla de consentimiento:
   - Nombre de la app: `Control Peso Thiscloud`
   - Email de soporte: `support@thiscloud.com.ar`
   - Contacto del desarrollador: `marco.alejandro.desantis@gmail.com`
   - Scopes: `openid`, `profile`, `email`

**Archivo JSON de Credenciales** (descargable desde Google Cloud Console):
```json
{
  "web": {
    "client_id": "180510012560-EXAMPLE.apps.googleusercontent.com",
    "client_secret": "GOCSPX-EXAMPLE_SECRET",
    "redirect_uris": [
      "http://localhost:8080/signin-google",
      "https://controlpeso.thiscloud.com.ar/signin-google"
    ]
  }
}
```

### LinkedIn OAuth (OPCIONAL - UI Removida)

⚠️ **NOTA**: La UI del botón de LinkedIn fue removida de la página de login, pero el backend OAuth está **preservado** para uso futuro.

Si deseas habilitar LinkedIn:

1. Ve a [LinkedIn Developers](https://www.linkedin.com/developers/apps)
2. Crea una nueva aplicación
3. En **Auth** → **OAuth 2.0 settings**
4. Redirect URLs:
   - `http://localhost:8080/signin-linkedin`
   - `https://controlpeso.thiscloud.com.ar/signin-linkedin`
5. Permisos requeridos: `openid`, `profile`, `email`
6. Copia el **Client ID** y **Client Secret** a `docker-compose.override.yml`
7. Agrega el botón de LinkedIn en `src/ControlPeso.Web/Components/Pages/Login.razor`

---

## 🗄️ Base de Datos SQLite

La base de datos SQLite (`controlpeso.db`) se almacena en un **volumen persistente** de Docker:

```bash
# Ver volúmenes
docker volume ls | grep controlpeso

# Inspeccionar volumen de datos
docker volume inspect controlpeso-data

# Backup de la base de datos
docker cp controlpeso-web:/app/data/controlpeso.db ./backup-controlpeso.db
```

### Seed Data (Demo)

La aplicación incluye **3 usuarios demo** con ~85 registros de peso (ver `DbSeeder.cs`):

- **Admin**: admin@controlpeso.thiscloud.app (Role: Administrator)
- **User1**: user1@controlpeso.thiscloud.app (Role: User)
- **User2**: user2@controlpeso.thiscloud.app (Role: User)

El seeding se ejecuta automáticamente en el primer inicio si la base de datos está vacía.

---

## 📊 Logs

Los logs se almacenan en el volumen `controlpeso-logs` usando **Serilog** con formato **NDJSON**:

```bash
# Ver logs en tiempo real
docker-compose logs -f controlpeso-web

# Ver logs estructurados (archivo .ndjson)
docker exec -it controlpeso-web cat /app/logs/controlpeso-$(date +%Y%m%d).ndjson | jq

# Copiar logs localmente
docker cp controlpeso-web:/app/logs ./logs-backup
```

### Configuración de Logging

Por defecto (ver `docker-compose.yml`):

- **Nivel mínimo**: Information
- **Console sink**: ✅ Habilitado
- **File sink**: ✅ Habilitado (`/app/logs/controlpeso-.ndjson`)
- **Rolling**: 10 MB por archivo, mantener últimos 30 archivos
- **Redaction**: ✅ Habilitado (PII/secrets enmascarados)
- **Correlation ID**: ✅ Auto-generado (`X-Correlation-Id`)

---

## 🛠️ Comandos Útiles

### Gestión del Contenedor

```bash
# Iniciar servicios
docker-compose up -d

# Detener servicios
docker-compose down

# Detener y ELIMINAR volúmenes (⚠️ DATOS SE PIERDEN)
docker-compose down -v

# Reiniciar servicios
docker-compose restart

# Ver estado
docker-compose ps

# Ver logs
docker-compose logs -f controlpeso-web

# Acceder a shell del contenedor
docker exec -it controlpeso-web /bin/bash
```

### Reconstruir Imagen

Si modificas código fuente o dependencias:

```bash
# Reconstruir imagen y reiniciar
docker-compose up -d --build --force-recreate
```

### Limpiar Todo

```bash
# Detener contenedor
docker-compose down

# Eliminar imagen
docker rmi controlpeso-controlpeso-web

# Eliminar volúmenes (⚠️ SE PIERDEN DATOS)
docker volume rm controlpeso-data controlpeso-logs

# Limpiar todo (contenedores, imágenes, volúmenes, networks)
docker system prune -a --volumes
```

---

## 🔒 Seguridad

### Secrets Management (CRÍTICO)

⚠️ **NUNCA commitees credenciales sensibles al repositorio Git.**

El proyecto usa **dos capas de seguridad**:

#### 1. docker-compose.override.yml (Secrets reales - NO en Git)

```yaml
# Este archivo contiene TUS credenciales reales
# ⚠️ NUNCA lo commitees al repositorio
services:
  controlpeso-web:
    environment:
      - Authentication__Google__ClientId=TUS_CREDENCIALES_REALES
      - Authentication__Google__ClientSecret=TUS_CREDENCIALES_REALES
      # ...
```

El `.gitignore` ya incluye:

```gitignore
# Docker - Secrets Management
docker-compose.override.yml  # ← TUS CREDENCIALES (NO va a Git)
.env
.env.*
!.env.example
```

#### 2. docker-compose.override.yml.example (Template - SÍ en Git)

```yaml
# Este archivo es un TEMPLATE (valores de ejemplo)
# ✅ SÍ va al repositorio como guía
services:
  controlpeso-web:
    environment:
      - Authentication__Google__ClientId=YOUR_CLIENT_ID_HERE
      - Authentication__Google__ClientSecret=YOUR_SECRET_HERE
      # ...
```

### Flujo de Trabajo Seguro

```bash
# 1. Clonar repositorio (NO contiene credenciales)
git clone https://github.com/mdesantis1984/Control-Peso-Thiscloud.git

# 2. Copiar template y agregar TUS credenciales
cp docker-compose.override.yml.example docker-compose.override.yml

# 3. Editar con credenciales REALES
nano docker-compose.override.yml  # o notepad en Windows

# 4. Docker Compose combina automáticamente ambos archivos
docker-compose up -d
# → docker-compose.yml (base) + docker-compose.override.yml (secrets)
```

### Variables Sensibles

Las siguientes variables contienen información sensible:

- `Authentication__Google__ClientSecret` (⚠️ CRÍTICO)
- `Authentication__LinkedIn__ClientSecret` (⚠️ CRÍTICO)
- `Authentication__Google__ClientId` (sensible pero no crítico)
- `Authentication__LinkedIn__ClientId` (sensible pero no crítico)
- `CloudflareAnalytics__Token` (opcional, sensible)

En **producción**, usa servicios especializados:

- **Azure**: Key Vault
- **AWS**: Secrets Manager
- **Docker Swarm/Kubernetes**: Secrets
- **HashiCorp Vault**: Enterprise secrets management

### Headers de Seguridad

El middleware `SecurityHeadersMiddleware` aplica:

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: geolocation=(), microphone=(), camera=()`
- `Content-Security-Policy` (13 directivas adaptadas para Blazor + MudBlazor)

### Rate Limiting

Configurado en `Program.cs`:

- **OAuth endpoints**: 5 requests/minuto por IP
- **Fixed endpoints**: 10 requests/minuto por IP

---

## 🌐 Producción

Para **producción**, modifica `docker-compose.yml`:

### 1. HTTPS con Certificados SSL

```yaml
services:
  controlpeso-web:
    ports:
      - "443:443"
      - "80:80"
    environment:
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/cert.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=${CERT_PASSWORD}
    volumes:
      - ./certs:/https:ro
```

### 2. Usar SQL Server en vez de SQLite

```yaml
services:
  controlpeso-web:
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=ControlPeso;User Id=sa;Password=${SQL_PASSWORD};TrustServerCertificate=True
    depends_on:
      - sqlserver

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=${SQL_PASSWORD}
    volumes:
      - sqlserver-data:/var/opt/mssql
    ports:
      - "1433:1433"

volumes:
  sqlserver-data:
```

### 3. Reverse Proxy (Nginx/Traefik)

```yaml
services:
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./certs:/etc/nginx/certs:ro
    depends_on:
      - controlpeso-web
```

---

## 🩺 Health Checks

### Endpoint de Salud

La aplicación expone un endpoint `/health` (si se implementa en `Program.cs`):

```bash
curl http://localhost:8080/health
```

### Docker Health Check

El `Dockerfile` incluye un health check automático:

```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1
```

Ver estado:

```bash
docker inspect --format='{{json .State.Health}}' controlpeso-web | jq
```

---

## 📦 Volúmenes Docker

### Ubicación por Defecto

```bash
# Windows (Docker Desktop)
\\wsl$\docker-desktop-data\data\docker\volumes\controlpeso-data\_data
\\wsl$\docker-desktop-data\data\docker\volumes\controlpeso-logs\_data

# Linux
/var/lib/docker/volumes/controlpeso-data/_data
/var/lib/docker/volumes/controlpeso-logs/_data

# macOS (Docker Desktop)
~/Library/Containers/com.docker.docker/Data/vms/0/data/docker/volumes/controlpeso-data/_data
```

### Backup de Volúmenes

```bash
# Backup database
docker run --rm \
  -v controlpeso-data:/data \
  -v $(pwd):/backup \
  alpine tar czf /backup/controlpeso-data-$(date +%Y%m%d).tar.gz -C /data .

# Backup logs
docker run --rm \
  -v controlpeso-logs:/logs \
  -v $(pwd):/backup \
  alpine tar czf /backup/controlpeso-logs-$(date +%Y%m%d).tar.gz -C /logs .
```

### Restore de Volúmenes

```bash
# Restore database
docker run --rm \
  -v controlpeso-data:/data \
  -v $(pwd):/backup \
  alpine sh -c "cd /data && tar xzf /backup/controlpeso-data-20260218.tar.gz"
```

---

## 🐛 Troubleshooting

### Problema: "Error de conexión OAuth"

**Causa**: URLs de redirección incorrectas en Google/LinkedIn.

**Solución**:

1. Verifica que las redirect URLs incluyan `http://localhost:8080`
2. Asegúrate de que el puerto coincide con `docker-compose.yml`
3. Reinicia el contenedor después de cambiar `.env`:

```bash
docker-compose down
docker-compose up -d
```

### Problema: "Base de datos no encontrada"

**Causa**: El volumen no está montado correctamente.

**Solución**:

```bash
# Verificar volumen
docker volume inspect controlpeso-data

# Verificar montaje en el contenedor
docker exec -it controlpeso-web ls -la /app/data

# Recrear volumen
docker-compose down -v
docker-compose up -d
```

### Problema: "Cannot connect to the Docker daemon"

**Causa**: Docker Desktop no está ejecutándose.

**Solución**: Inicia Docker Desktop y espera a que esté "Running".

### Problema: "Port 8080 already in use"

**Causa**: Otro proceso está usando el puerto 8080.

**Solución**:

1. Cambiar el puerto en `docker-compose.yml`:

```yaml
ports:
  - "8081:8080"  # Usar 8081 en host
```

2. O detener el proceso que usa el puerto:

```bash
# Windows
netstat -ano | findstr :8080
taskkill /PID <PID> /F

# Linux/macOS
lsof -i :8080
kill -9 <PID>
```

### Problema: "Out of disk space"

**Causa**: Docker usa mucho espacio.

**Solución**:

```bash
# Ver uso de espacio
docker system df

# Limpiar imágenes/contenedores sin usar
docker system prune -a

# Limpiar volúmenes sin usar
docker volume prune
```

---

## 📚 Referencias

- **Documentación oficial**: Ver `docs/` en el repositorio
  - `ARCHITECTURE.md`: Arquitectura Onion
  - `DATABASE.md`: Database First workflow
  - `SECURITY.md`: OAuth, headers, rate limiting
  - `DEPLOYMENT.md`: Azure/IIS deployment
  - `SEO.md`: SEO strategy
- **Docker Docs**: https://docs.docker.com/
- **Docker Compose**: https://docs.docker.com/compose/
- **.NET Docker Images**: https://hub.docker.com/_/microsoft-dotnet

---

## 🤝 Soporte

¿Problemas con Docker? Abre un **issue** en GitHub:

📧 https://github.com/mdesantis1984/Control-Peso-Thiscloud/issues

---

**¡Disfruta de Control Peso Thiscloud en Docker!** 🐳🚀
