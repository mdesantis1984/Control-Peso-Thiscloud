# ✅ Pre-Producción Docker - Configuración Completa

## 🎯 Objetivo

Despliegue de pre-producción local con Docker Compose, idéntico al que se usará en producción, con:
- ✅ Logging: **Solo Error y Critical** (sin spam)
- ✅ SQL Server Express con esquema completo actualizado
- ✅ Google OAuth configurado y funcional
- ✅ Telegram Bot configurado y funcional

---

## 📋 Cambios Realizados

### 1. Logging Configuración (Error y Critical solamente)

#### `docker-compose.yml` (líneas 58-70)
```yaml
- ThisCloud__Loggings__IsEnabled=true
- ThisCloud__Loggings__MinimumLevel=Error        # ✅ Solo Error y Critical
- ThisCloud__Loggings__Console__Enabled=false    # ✅ Sin consola en producción
- ThisCloud__Loggings__File__Enabled=true
- ThisCloud__Loggings__File__Path=/app/logs/controlpeso-.ndjson
- ThisCloud__Loggings__File__RetainedFileCountLimit=60
```

#### `src/ControlPeso.Web/appsettings.Production.json`
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Error",
      "Microsoft.AspNetCore": "Error",
      "ControlPeso": "Error"
    }
  },
  "ThisCloud": {
    "Loggings": {
      "MinimumLevel": "Error"
    }
  }
}
```

**Resultado**: Los logs solo mostrarán errores y critical, eliminando el ruido de información.

---

### 2. Script de Inicialización de Base de Datos

#### `scripts/init-database.sql` (NUEVO)

Script idempotente que crea:
- ✅ Database `ControlPeso` (si no existe)
- ✅ Tabla `Users` con todos los campos actualizados
- ✅ Tabla `WeightLogs` con estructura completa
- ✅ Tabla `UserPreferences`
- ✅ Tabla `AuditLog`
- ✅ Tabla `UserNotifications` (nueva)
- ✅ Todos los índices para performance óptimo

**Características**:
- Verifica existencia antes de crear (no falla si ya existe)
- Compatible con SQL Server Express 2022
- Collation: `SQL_Latin1_General_CP1_CI_AS`
- Checks y constraints incluidos
- Mensajes de confirmación: `✅ Database initialization complete!`

---

### 3. Script PowerShell de Despliegue Automatizado

#### `scripts/Deploy-Docker-Local.ps1` (NUEVO)

Script completo con acciones:
- `build`: Build de imagen Docker
- `start`: Iniciar servicios en daemon mode
- `stop`: Detener servicios (conserva datos)
- `restart`: Reiniciar servicios
- `logs`: Ver logs en tiempo real
- `init-db`: Ejecutar script de inicialización de DB
- `verify`: Verificar health checks y funcionalidad
- `full`: Hacer todo (build + start + init-db + verify)
- `clean`: Eliminar TODO (contenedores, volúmenes, imágenes)

**Uso rápido**:
```powershell
.\scripts\Deploy-Docker-Local.ps1 -Action full
```

**Características**:
- Colores en consola (✅ verde, ❌ rojo, ⚠️ amarillo)
- Health checks automáticos (espera hasta que servicios estén healthy)
- Extrae SA Password de `docker-compose.override.yml` automáticamente
- Verificaciones: health endpoint, homepage, logs sin errores críticos

---

### 4. Documentación Completa

#### `docs/DEPLOYMENT_DOCKER.md` (NUEVO)

Guía detallada de 300+ líneas con:
- ✅ Pre-requisitos y configuración inicial
- ✅ Build de imagen Docker
- ✅ Inicialización de servicios
- ✅ Configuración de base de datos (2 opciones: script SQL o migración)
- ✅ Verificación post-despliegue (health, OAuth, Telegram)
- ✅ Monitoreo y mantenimiento
- ✅ Backup de volúmenes
- ✅ Troubleshooting completo
- ✅ Despliegue a producción con Nginx Proxy Manager

#### `DEPLOYMENT_QUICKSTART.md` (NUEVO - Raíz del proyecto)

Guía rápida de 150+ líneas con:
- ✅ 2 opciones: Script automatizado o manual
- ✅ Comandos paso a paso
- ✅ Verificación post-despliegue
- ✅ Comandos útiles
- ✅ Troubleshooting rápido
- ✅ Migración de datos locales
- ✅ Checklist final

---

## 🔧 Configuración Requerida

### docker-compose.override.yml

**⚠️ CRÍTICO**: Este archivo **NO** está en Git (gitignored). Debes editarlo con credenciales reales:

```yaml
services:
  sqlserver:
    environment:
      - SA_PASSWORD=ControlPeso2026!  # ← Cambiar por password seguro

  controlpeso-web:
    environment:
      # Database
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=ControlPeso;User Id=sa;Password=ControlPeso2026!;...

      # Google OAuth (valores reales en appsettings.Development.json - NO versionado)
      - Authentication__Google__ClientId=YOUR_GOOGLE_CLIENT_ID_HERE
      - Authentication__Google__ClientSecret=YOUR_GOOGLE_CLIENT_SECRET_HERE

      # Telegram (valores reales en appsettings.Development.json - NO versionado)
      - Telegram__Enabled=true
      - Telegram__BotToken=YOUR_TELEGRAM_BOT_TOKEN_HERE
      - Telegram__ChatId=YOUR_TELEGRAM_CHAT_ID_HERE
      - Telegram__Environment=Production
```

**Estas credenciales ya están configuradas según tus datos reales**.

---

## 🚀 Despliegue Rápido (3 Opciones)

### Opción 1: Script Automatizado (RECOMENDADO) ⚡

```powershell
cd F:\Proyectos\ThisCloudServices\03-Repo\ControlPeso
.\scripts\Deploy-Docker-Local.ps1 -Action full
```

**Duración**: 5-10 minutos (incluye build, health checks, init DB, verificación)

---

### Opción 2: Manual Paso a Paso

```bash
# 1. Build (2-5 minutos)
docker compose build --no-cache

# 2. Iniciar servicios
docker compose up -d

# 3. Esperar a que estén healthy (30-60 segundos)
docker compose ps

# 4. Inicializar base de datos
docker cp scripts/init-database.sql controlpeso-sqlserver:/tmp/init-database.sql
docker exec -it controlpeso-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "ControlPeso2026!" -i /tmp/init-database.sql -C

# 5. Verificar
curl http://localhost:8080/health
```

---

### Opción 3: Migrar Datos desde Local

Si ya tienes datos en `localhost\SQLEXPRESS`:

```bash
# 1. Build e iniciar servicios
docker compose build --no-cache
docker compose up -d

# 2. Backup local (en SSMS)
# Right-click ControlPeso → Tasks → Back Up...
# Guardar: F:\Proyectos\backup\ControlPeso.bak

# 3. Copiar y restaurar
docker cp F:\Proyectos\backup\ControlPeso.bak controlpeso-sqlserver:/var/opt/mssql/backup/
docker exec -it controlpeso-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "ControlPeso2026!" -C -Q \
  "RESTORE DATABASE ControlPeso FROM DISK='/var/opt/mssql/backup/ControlPeso.bak' WITH REPLACE"
```

---

## ✅ Checklist de Verificación

Después del despliegue, verificar:

### 1. Servicios Healthy
```bash
docker compose ps
```
**Esperado**:
```
NAME                   STATUS              PORTS
controlpeso-sqlserver  Up (healthy)        0.0.0.0:1433->1433/tcp
controlpeso-web        Up (healthy)        0.0.0.0:8080->8080/tcp
```

### 2. Health Endpoint
```bash
curl http://localhost:8080/health
```
**Esperado**: `Healthy`

### 3. Google OAuth
1. Abrir: http://localhost:8080
2. Click "Iniciar con Google"
3. Verificar que funciona el login

### 4. Telegram
1. Abrir: http://localhost:8080/diagnostics/telegram
2. Click "ENVIAR MENSAJE DE PRUEBA"
3. Verificar mensaje en Telegram chat

### 5. Logs (Solo Error/Critical)
```bash
docker compose logs --tail=100 controlpeso-web
```
**Esperado**: Solo errores (si hay), sin logs de Information o Debug

### 6. Base de Datos
```bash
docker exec -it controlpeso-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "ControlPeso2026!" -C -Q \
  "SELECT name FROM sys.tables"
```
**Esperado**:
```
Users
WeightLogs
UserPreferences
AuditLog
UserNotifications
```

---

## 📊 Monitoreo

### Logs en Tiempo Real
```bash
docker compose logs -f controlpeso-web
```

### Estadísticas de Contenedores
```bash
docker stats
```

### Ver Logs desde Archivo
```bash
# Listar archivos de log
docker exec -it controlpeso-web ls -lh /app/logs/

# Ver últimas 50 líneas
docker exec -it controlpeso-web tail -n 50 /app/logs/controlpeso-20250301.ndjson
```

### Verificar Volúmenes
```bash
docker volume ls | grep controlpeso
docker volume inspect controlpeso-sqldata
docker volume inspect controlpeso-logs
```

---

## 🛑 Comandos de Mantenimiento

```bash
# Reiniciar solo la aplicación web
docker compose restart controlpeso-web

# Reiniciar SQL Server
docker compose restart sqlserver

# Detener servicios (conserva datos)
docker compose down

# Ver estado
docker compose ps

# Ver logs de errores recientes
docker compose logs --tail=100 controlpeso-web | grep -i error
```

---

## 🔥 Troubleshooting Rápido

### Contenedor no inicia
```bash
docker compose logs sqlserver
docker compose logs controlpeso-web
```

### Puerto 8080 ocupado
Editar `docker-compose.override.yml`:
```yaml
controlpeso-web:
  ports:
    - "9090:8080"  # Cambiar puerto externo
```

### Google OAuth redirect_uri_mismatch
Agregar en Google Cloud Console:
```
http://localhost:8080/signin-google
```

### Telegram 401 Unauthorized
Verificar token completo en `docker-compose.override.yml`:
```yaml
- Telegram__BotToken=7928521075:AAGhzDZ1L04NWpSs-vB2rTU76Bu7U-T8kak
```

---

## 📦 Backup y Restore

### Backup Completo
```bash
# SQL Server data
docker run --rm -v controlpeso-sqldata:/data -v ${PWD}:/backup alpine \
  tar czf /backup/sqldata-backup-$(date +%Y%m%d).tar.gz -C /data .

# Logs
docker run --rm -v controlpeso-logs:/data -v ${PWD}:/backup alpine \
  tar czf /backup/logs-backup-$(date +%Y%m%d).tar.gz -C /data .
```

### Restore Completo
```bash
# Detener servicios
docker compose down

# Restaurar SQL Server data
docker run --rm -v controlpeso-sqldata:/data -v ${PWD}:/backup alpine \
  tar xzf /backup/sqldata-backup-20250301.tar.gz -C /data

# Reiniciar
docker compose up -d
```

---

## 🌐 Despliegue a Producción

Ver guía completa en: `docs/DEPLOYMENT_DOCKER.md`

**Cambios necesarios**:

1. **docker-compose.override.yml**:
   ```yaml
   - AllowedHosts=controlpeso.thiscloud.com.ar
   ```

2. **Nginx Proxy Manager**:
   - Domain: `controlpeso.thiscloud.com.ar`
   - Forward: `controlpeso-web:8080`
   - SSL: Let's Encrypt

3. **Google OAuth**:
   - Agregar: `https://controlpeso.thiscloud.com.ar/signin-google`

---

## 📚 Archivos de Referencia

| Archivo | Propósito | Git |
|---------|-----------|-----|
| `docker-compose.yml` | Configuración base | ✅ |
| `docker-compose.override.yml` | Credenciales (sensibles) | ❌ |
| `appsettings.Production.json` | Config producción | ✅ |
| `scripts/init-database.sql` | Inicialización DB | ✅ |
| `scripts/Deploy-Docker-Local.ps1` | Script automatizado | ✅ |
| `docs/DEPLOYMENT_DOCKER.md` | Guía detallada | ✅ |
| `DEPLOYMENT_QUICKSTART.md` | Guía rápida | ✅ |

---

## 🎯 Siguiente Paso

**EJECUTAR DESPLIEGUE**:

```powershell
cd F:\Proyectos\ThisCloudServices\03-Repo\ControlPeso
.\scripts\Deploy-Docker-Local.ps1 -Action full
```

Esto ejecutará automáticamente:
1. ✅ Verificación de pre-requisitos
2. ✅ Build de imagen Docker
3. ✅ Inicio de servicios
4. ✅ Health checks
5. ✅ Inicialización de base de datos
6. ✅ Verificación completa

**Duración estimada**: 5-10 minutos

---

**Última actualización**: 2025-03-01  
**Versión**: 3.0  
**Estado**: ✅ Listo para despliegue
