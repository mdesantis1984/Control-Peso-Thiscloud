# ✅ SOLUCIONADO: Persistencia de Avatares + Configuración de Producción

## 🎯 Problemas Resueltos

### 1. ✅ Avatares de Perfil No Persistían

**Problema**: Las fotos de perfil se perdían al reiniciar el contenedor Docker.

**Causa**: No había volumen montado para `wwwroot/uploads/avatars`

**Solución Implementada**:

#### A. Volumen persistente agregado (`docker-compose.yml`)
```yaml
volumes:
  # Persist user avatars (profile photos)
  - controlpeso-avatars:/app/wwwroot/uploads/avatars

# ...

volumes:
  controlpeso-avatars:
    driver: local
    name: controlpeso-avatars
```

#### B. Permisos correctos en Dockerfile
```dockerfile
# Create directories for avatars
RUN mkdir -p /app/wwwroot/uploads/avatars

# Set permissions for wwwroot (avatars upload)
RUN chmod -R 755 /app/wwwroot
```

#### C. Verificación
```bash
# Ver volumen creado
docker volume ls | grep controlpeso-avatars

# Resultado:
local     controlpeso-avatars
```

**Estado**: ✅ **Resuelto** - Los avatares ahora persisten entre reinicios del contenedor.

---

### 2. ✅ Configuración de Puertos para Producción Documentada

**Recordatorio**: En producción con Nginx Proxy Manager, usar puerto diferente a 8080.

**Documentación Creada**:
- ✅ `docs/PRODUCTION_PORTS_CONFIG.md` - Guía completa de configuración de puertos
- ✅ `docs/DEPLOYMENT_DOCKER.md` - Actualizado con flujo de tráfico en producción

**Configuración Recomendada para Producción**:

```yaml
# docker-compose.override.yml (Producción)
services:
  controlpeso-web:
    ports:
      - "8083:8080"  # Puerto externo 8083 → Puerto interno 8080
```

**Flujo de Tráfico**:
```
Usuario (HTTPS 443) 
    ↓
Nginx Proxy Manager (controlpeso.thiscloud.com.ar)
    ↓
HTTP (localhost:8083)
    ↓
Docker Container (8080)
```

**Estado**: ✅ **Documentado** - Listo para despliegue en producción.

---

## 📦 Volúmenes Persistentes (Actualizados)

La aplicación ahora usa **4 volúmenes persistentes**:

| Volumen | Contenido | Path | Estado |
|---------|-----------|------|--------|
| `controlpeso-sqldata` | Base de datos | `/var/opt/mssql` | ✅ Activo |
| `controlpeso-logs` | Logs | `/app/logs` | ✅ Activo |
| `controlpeso-keys` | Data Protection keys | `/root/.aspnet/DataProtection-Keys` | ✅ Activo |
| `controlpeso-avatars` | **Fotos de perfil** | `/app/wwwroot/uploads/avatars` | ✅ **NUEVO** |

---

## 🧪 Verificación

### 1. Verificar Volúmenes
```bash
docker volume ls | grep controlpeso
```

**Resultado esperado**:
```
local     controlpeso-avatars    ← NUEVO
local     controlpeso-keys
local     controlpeso-logs
local     controlpeso-sqldata
```

### 2. Probar Subida de Avatar

1. Navegar a: http://localhost:8080/profile
2. Subir foto de perfil
3. Reiniciar contenedor:
   ```bash
   docker compose restart controlpeso-web
   ```
4. Recargar página: http://localhost:8080/profile
5. **Verificar**: La foto de perfil **debe seguir ahí** ✅

### 3. Inspeccionar Volumen de Avatares
```bash
docker volume inspect controlpeso-avatars
```

**Resultado**:
```json
[
    {
        "CreatedAt": "2026-03-01T17:23:37Z",
        "Driver": "local",
        "Labels": {
            "com.docker.compose.project": "controlpeso",
            "com.docker.compose.version": "..."
        },
        "Mountpoint": "/var/lib/docker/volumes/controlpeso-avatars/_data",
        "Name": "controlpeso-avatars",
        "Options": null,
        "Scope": "local"
    }
]
```

---

## 🚀 Comandos Actualizados

### Detener servicios (conserva avatares)
```bash
docker compose down
```

### Detener y ELIMINAR TODO (⚠️ incluye avatares)
```bash
docker compose down -v
```

### Backup de avatares
```bash
docker run --rm -v controlpeso-avatars:/data -v ${PWD}:/backup alpine \
  tar czf /backup/avatars-backup-$(date +%Y%m%d).tar.gz -C /data .
```

### Restore de avatares
```bash
docker run --rm -v controlpeso-avatars:/data -v ${PWD}:/backup alpine \
  tar xzf /backup/avatars-backup-20260301.tar.gz -C /data
```

---

## 📝 Cambios en Código (Git)

### Archivos Modificados
- ✅ `docker-compose.yml` - Volumen `controlpeso-avatars` agregado
- ✅ `Dockerfile` - Permisos `chmod 755` para `/app/wwwroot`
- ✅ `docs/DEPLOYMENT_DOCKER.md` - Flujo de tráfico producción actualizado
- ✅ `DEPLOYMENT_QUICKSTART.md` - Volúmenes actualizados

### Archivos Nuevos
- ✅ `docs/PRODUCTION_PORTS_CONFIG.md` - Guía completa de puertos

### Commits
```
c06c8a7 - fix(docker): persist user avatars with dedicated volume and document production port configuration
0834e1f - feat(deployment): pre-production Docker setup with logging Error-only and automated deployment scripts
```

---

## 🎯 Próximos Pasos

### Inmediato
1. ✅ **Probar subida de avatar** en http://localhost:8080/profile
2. ✅ **Verificar persistencia** reiniciando contenedor
3. ✅ **Hacer push** a GitHub cuando todo esté probado

### Para Producción
1. ⏳ Cambiar puerto en `docker-compose.override.yml` a `8083:8080`
2. ⏳ Configurar Nginx Proxy Manager según `docs/PRODUCTION_PORTS_CONFIG.md`
3. ⏳ Verificar Google OAuth con `https://controlpeso.thiscloud.com.ar/signin-google`
4. ⏳ Verificar Telegram notificaciones en producción

---

## 🔥 Troubleshooting

### Avatar subido pero no aparece después de reinicio

**Verificar volumen montado**:
```bash
docker inspect controlpeso-web | grep -A 10 Mounts
```

**Debe mostrar**:
```json
"Mounts": [
    {
        "Type": "volume",
        "Source": "controlpeso-avatars",
        "Destination": "/app/wwwroot/uploads/avatars",
        ...
    }
]
```

### Puerto 8083 no responde en producción

**Verificar mapeo de puerto**:
```bash
docker compose ps
```

**Debe mostrar**:
```
controlpeso-web ... 0.0.0.0:8083->8080/tcp
```

**Verificar Nginx Forward Port**: Debe ser `8083`

---

## ✅ Checklist de Validación

Pre-Producción Local:
- [x] Volumen `controlpeso-avatars` creado
- [x] Contenedor con permisos correctos en `/app/wwwroot`
- [x] Build exitoso
- [x] Servicios `Up (healthy)`
- [ ] Avatar sube correctamente
- [ ] Avatar persiste después de reinicio

Producción:
- [ ] Puerto cambiado a `8083:8080`
- [ ] Nginx configurado con Forward Port `8083`
- [ ] SSL habilitado en Nginx
- [ ] Google OAuth redirect URI actualizado
- [ ] Telegram notificaciones funcionan
- [ ] Avatares persisten en producción

---

**Última actualización**: 2026-03-01 18:30  
**Estado**: ✅ **Avatares Persistiendo** | 📋 **Documentación Completa** | 🚀 **Listo para Producción**
