# GUÍA DE ROTACIÓN DE CREDENCIALES - Google OAuth

**Fecha**: 2026-03-01  
**Razón**: Secretos fueron expuestos en historial de Git (ya limpiado)  
**Estado**: ⚠️ ACCIÓN REQUERIDA

---

## 🚨 IMPORTANTE: ROTAR INMEDIATAMENTE

Aunque el historial de Git fue limpiado, los secretos estuvieron expuestos públicamente.
**DEBES crear nuevas credenciales** para garantizar la seguridad.

---

## Paso 1: Eliminar Cliente OAuth Actual

1. Ir a: https://console.cloud.google.com/apis/credentials
2. Seleccionar proyecto: **ControlPeso** (o el nombre de tu proyecto)
3. Buscar el Client ID antiguo (el que fue expuesto)
4. Click en el ícono 🗑️ (eliminar)
5. Confirmar eliminación

---

## Paso 2: Crear Nuevo Cliente OAuth

1. Click en **"+ CREATE CREDENTIALS"** → **"OAuth client ID"**
2. Application type: **Web application**
3. Name: **ControlPeso Production** (o nombre descriptivo)
4. Authorized JavaScript origins:
   - `https://controlpeso.thiscloud.com.ar`
   - `http://localhost:8080`
   - `https://localhost:7065`
   - `http://localhost:5212`
5. Authorized redirect URIs:
   - `https://controlpeso.thiscloud.com.ar/signin-google`
   - `http://localhost:8080/signin-google`
   - `https://localhost:7065/signin-google`
   - `http://localhost:5212/signin-google`
6. Click **"CREATE"**
7. Copiar el nuevo Client ID y Client Secret

---

## Paso 3: Actualizar Credenciales Locales

### Opción A: User Secrets (Recomendado para desarrollo)

```powershell
cd src/ControlPeso.Web
dotnet user-secrets set "Authentication:Google:ClientId" "NUEVO_CLIENT_ID.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "GOCSPX-NUEVO_SECRET"
```

### Opción B: docker-compose.override.yml (Para Docker local)

Editar `docker-compose.override.yml`:

```yaml
services:
  controlpeso-web:
    environment:
      - Authentication__Google__ClientId=NUEVO_CLIENT_ID.apps.googleusercontent.com
      - Authentication__Google__ClientSecret=GOCSPX-NUEVO_SECRET
```

---

## Paso 4: Verificar Funcionamiento

```powershell
# Desarrollo local
dotnet run --project src/ControlPeso.Web

# Docker
docker-compose up -d --build
```

Ir a `https://localhost:7065/login` y probar login con Google.

---

## Paso 5: Actualizar Producción

Actualizar variables de entorno en el servidor de producción:
- `Authentication__Google__ClientId`
- `Authentication__Google__ClientSecret`

---

## ✅ Checklist de Rotación

- [ ] Cliente OAuth anterior eliminado de Google Console
- [ ] Nuevo Cliente OAuth creado
- [ ] User Secrets actualizados (desarrollo local)
- [ ] docker-compose.override.yml actualizado (Docker local)
- [ ] Login probado localmente
- [ ] Producción actualizada (si aplica)

---

## 📝 Notas de Seguridad

- Los secretos antiguos ya **NO son válidos** después de eliminar el cliente
- El historial de Git fue limpiado con `git-filter-repo`
- Los tags fueron actualizados con force push
- Nunca commitear secretos reales en documentación

---

**Completado por**: Sistema automatizado  
**Revisión requerida por**: Propietario del proyecto
