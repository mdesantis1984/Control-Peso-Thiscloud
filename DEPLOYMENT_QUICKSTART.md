# 🚀 Despliegue Docker - Guía Rápida

## Pre-Producción Local (Recomendado)

### Opción 1: Script Automatizado (RECOMENDADO) ⚡

```powershell
# Despliegue completo automático (build + start + init DB + verify)
.\scripts\Deploy-Docker-Local.ps1 -Action full

# O si prefieres paso a paso
.\scripts\Deploy-Docker-Local.ps1 -Action build      # 1. Build imagen
.\scripts\Deploy-Docker-Local.ps1 -Action start      # 2. Iniciar servicios
.\scripts\Deploy-Docker-Local.ps1 -Action init-db    # 3. Crear base de datos
.\scripts\Deploy-Docker-Local.ps1 -Action verify     # 4. Verificar todo funciona
```

### Opción 2: Manual (Paso a Paso)

```bash
# 1. Configurar credenciales
# Editar docker-compose.override.yml con:
#   - SA_PASSWORD (SQL Server)
#   - Google OAuth (ClientId, ClientSecret)
#   - Telegram (BotToken, ChatId)

# 2. Build
docker compose build --no-cache

# 3. Iniciar servicios
docker compose up -d

# 4. Verificar health
docker compose ps
# Esperar a que ambos contenedores muestren "healthy"

# 5. Inicializar base de datos
docker cp scripts/init-database.sql controlpeso-sqlserver:/tmp/init-database.sql
docker exec -it controlpeso-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "TU_PASSWORD" -i /tmp/init-database.sql -C

# 6. Verificar aplicación
curl http://localhost:8080/health
# Abrir navegador: http://localhost:8080
```

---

## Verificación Post-Despliegue ✅

### 1. Health Check
```bash
curl http://localhost:8080/health
# Debe responder: Healthy
```

### 2. Google OAuth
1. Navegar a: http://localhost:8080
2. Click en "Iniciar con Google"
3. Verificar que el login funciona correctamente

### 3. Telegram
1. Navegar a: http://localhost:8080/diagnostics/telegram
2. Click en "ENVIAR MENSAJE DE PRUEBA"
3. Verificar que recibes mensaje en Telegram

### 4. Logs (Solo Error y Critical)
```bash
# Ver logs en tiempo real
docker compose logs -f controlpeso-web

# Ver últimas 50 líneas
docker compose logs --tail=50 controlpeso-web

# Verificar que solo hay errores (no INFO, no DEBUG)
docker compose logs controlpeso-web | grep -E "Information|Debug"
# No debería mostrar nada
```

---

## Comandos Útiles 🛠️

```bash
# Ver estado de servicios
docker compose ps

# Reiniciar solo la aplicación web (sin perder DB)
docker compose restart controlpeso-web

# Ver logs en tiempo real
docker compose logs -f

# Detener servicios (conserva datos)
docker compose down

# Detener y ELIMINAR TODO (⚠️ CUIDADO - borra DB)
docker compose down -v

# Inspeccionar volúmenes
docker volume ls | grep controlpeso
docker volume inspect controlpeso-sqldata
```

---

## Configuración de Logging ⚙️

El proyecto está configurado para **solo loguear Error y Critical** en producción:

### docker-compose.yml
```yaml
- ThisCloud__Loggings__MinimumLevel=Error
- ThisCloud__Loggings__Console__Enabled=false  # Sin logs en consola
```

### appsettings.Production.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Error",
      "Microsoft.AspNetCore": "Error",
      "ControlPeso": "Error"
    }
  }
}
```

**Resultado**: Solo verás errores y critical en los logs, sin spam de información.

---

## Troubleshooting 🔧

### Error: "Cannot connect to SQL Server"
```bash
# Verificar estado
docker compose ps

# Ver logs de SQL Server
docker compose logs sqlserver

# Verificar conexión
docker exec -it controlpeso-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "TU_PASSWORD" -Q "SELECT @@VERSION" -C
```

### Error: "Port 8080 already in use"
```bash
# Cambiar puerto en docker-compose.override.yml
services:
  controlpeso-web:
    ports:
      - "9090:8080"  # Usar puerto diferente
```

### Google OAuth: "redirect_uri_mismatch"
Agregar URL en Google Cloud Console:
```
http://localhost:8080/signin-google
```

### Telegram: "401 Unauthorized"
Verificar token completo en `docker-compose.override.yml`:
```yaml
- Telegram__BotToken=7928521075:AAGhzDZ1L04NWpSs-vB2rTU76Bu7U-T8kak
```

---

## Migrar Datos desde Local 📦

Si ya tienes datos en tu SQL Server local:

```bash
# 1. Backup local (en SSMS)
Right-click ControlPeso → Tasks → Back Up...
# Guardar en: F:\Proyectos\backup\ControlPeso.bak

# 2. Copiar al contenedor
docker cp F:\Proyectos\backup\ControlPeso.bak controlpeso-sqlserver:/var/opt/mssql/backup/

# 3. Restaurar
docker exec -it controlpeso-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "TU_PASSWORD" -C -Q \
  "RESTORE DATABASE ControlPeso FROM DISK='/var/opt/mssql/backup/ControlPeso.bak' WITH REPLACE"
```

---

## Backup de Volúmenes 💾

```bash
# Backup de datos SQL Server
docker run --rm -v controlpeso-sqldata:/data -v ${PWD}:/backup alpine \
  tar czf /backup/controlpeso-sqldata-$(date +%Y%m%d-%H%M%S).tar.gz -C /data .

# Backup de logs
docker run --rm -v controlpeso-logs:/data -v ${PWD}:/backup alpine \
  tar czf /backup/controlpeso-logs-$(date +%Y%m%d-%H%M%S).tar.gz -C /data .
```

---

## Despliegue a Producción (Nginx Proxy Manager)

Ver guía completa en: **docs/DEPLOYMENT_DOCKER.md**

### Cambios necesarios:

1. **docker-compose.override.yml**:
   ```yaml
   - AllowedHosts=controlpeso.thiscloud.com.ar
   ```

2. **Nginx Proxy Manager**:
   - Domain: `controlpeso.thiscloud.com.ar`
   - Forward to: `controlpeso-web:8080`
   - SSL: Let's Encrypt

3. **Google OAuth**:
   - Agregar URL: `https://controlpeso.thiscloud.com.ar/signin-google`

---

## Checklist Final ✅

Antes de considerar el despliegue completo:

- [ ] `docker compose ps` muestra ambos servicios `Up (healthy)`
- [ ] Base de datos inicializada (tablas: Users, WeightLogs, etc.)
- [ ] Google OAuth funciona (puedes hacer login)
- [ ] Telegram envía mensajes correctamente
- [ ] Logs configurados en nivel Error (no spam)
- [ ] `curl http://localhost:8080/health` responde `Healthy`
- [ ] Backup de volúmenes configurado
- [ ] URLs de producción agregadas en Google OAuth (si aplica)

---

## 📚 Documentación Completa

- **Guía detallada**: `docs/DEPLOYMENT_DOCKER.md`
- **Script automatizado**: `scripts/Deploy-Docker-Local.ps1`
- **Schema SQL**: `scripts/init-database.sql`
- **Configuración**: `docker-compose.yml` + `docker-compose.override.yml`

---

**Última actualización**: 2025-03-01  
**Versión**: 3.0
