# INCIDENTE DE SEGURIDAD - appsettings.Development.json

**Fecha**: 2026-02-20  
**Severidad**: MEDIA (Mitigada)  
**Estado**: ⚠️ ACCIÓN REQUERIDA POR USUARIO

---

## 🚨 Resumen Ejecutivo

El archivo `src/ControlPeso.Web/appsettings.Development.json` estaba **trackeado en Git** desde commit `d9d6fa1`, lo que representa un riesgo de seguridad si contiene secretos reales.

### Estado Actual

✅ **Protecciones Implementadas**:
- `.gitignore` actualizado (archivos sensibles ahora ignorados)
- Archivo `.example` creado con estructura documentada
- Build exitoso con nuevas protecciones

⚠️ **Pendiente**:
- Verificar historial de Git por secretos expuestos
- Remover archivo del índice de Git
- Configurar secretos vía User Secrets
- Commitear cambios de seguridad

---

## ✅ Acciones Completadas (AI Assistant)

### 1. Actualizar `.gitignore`

```gitignore
# Archivos de configuración con datos sensibles
appsettings.Development.json
appsettings.*.json
!appsettings.json
appsettings.Local.json
appsettings.Production.json
```

### 2. Crear Template de Ejemplo

Archivo: `src/ControlPeso.Web/appsettings.Development.json.example`

Contiene estructura completa con placeholders claros.

---

## ⏳ Acciones REQUERIDAS (Usuario)

### PASO 1: Verificar Historial (CRÍTICO)

```powershell
# Ver todos los cambios históricos
git log -p -- src/ControlPeso.Web/appsettings.Development.json

# Buscar secretos específicos (reemplazar con tus valores)
git log --all -S "GOCSPX-" -- src/ControlPeso.Web/appsettings.Development.json
git log --all -S "78kj" -- src/ControlPeso.Web/appsettings.Development.json
```

**SI encuentras secretos reales**: 
- ⚠️ ROTAR INMEDIATAMENTE (Google, LinkedIn, Telegram)
- ⚠️ Limpiar historial con BFG o filter-branch
- ⚠️ Ver `docs/SECURITY_INCIDENT.md` sección "Limpieza"

**SI NO encuentras secretos**: 
- ✅ Proceder con Paso 2

### PASO 2: Remover del Tracking de Git

```powershell
# Remover del índice (NO borra archivo local)
git rm --cached src/ControlPeso.Web/appsettings.Development.json

# Verificar estado
git status
# Deberías ver:
#   deleted: src/ControlPeso.Web/appsettings.Development.json (staged - verde)
#   appsettings.Development.json (untracked - rojo)
```

### PASO 3: Commitear Protecciones de Seguridad

```powershell
git add .gitignore
git add src/ControlPeso.Web/appsettings.Development.json.example
git add docs/SECURITY_INCIDENT.md

git commit -m "security: protect sensitive configuration files from Git

- Add appsettings.Development.json to .gitignore
- Remove appsettings.Development.json from Git tracking
- Create appsettings.Development.json.example as template

BREAKING CHANGE: Developers must now use User Secrets or copy .example file locally"
```

### PASO 4: Configurar Secretos Localmente

**OPCIÓN A: User Secrets (RECOMENDADO)**

```powershell
cd src/ControlPeso.Web

# Google OAuth
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_GOOGLE_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "GOCSPX-YOUR_ROTATED_SECRET_HERE"

# LinkedIn OAuth (si tienes)
dotnet user-secrets set "Authentication:LinkedIn:ClientId" "TU_CLIENT_ID"
dotnet user-secrets set "Authentication:LinkedIn:ClientSecret" "TU_CLIENT_SECRET"

# Telegram (después de configurar bot)
dotnet user-secrets set "Telegram:Enabled" "true"
dotnet user-secrets set "Telegram:BotToken" "TU_BOT_TOKEN"
dotnet user-secrets set "Telegram:ChatId" "TU_CHAT_ID"
```

**OPCIÓN B: Archivo Local** (menos seguro)

```powershell
Copy-Item src/ControlPeso.Web/appsettings.Development.json.example `
          src/ControlPeso.Web/appsettings.Development.json

# Editar con tus valores reales
code src/ControlPeso.Web/appsettings.Development.json
```

### PASO 5: Verificar Configuración

```powershell
# Ver User Secrets configurados
dotnet user-secrets list --project src/ControlPeso.Web

# Compilar proyecto
dotnet build

# Correr app
dotnet run --project src/ControlPeso.Web
```

---

## 🔐 Rotación de Secretos (SI hubo exposición)

### Google OAuth

1. https://console.cloud.google.com/apis/credentials
2. DELETE current Client ID
3. CREATE NEW OAuth 2.0 Client
4. Configure redirect URIs
5. UPDATE User Secrets

### LinkedIn OAuth

1. https://www.linkedin.com/developers/apps
2. Auth → "Regenerate Client Secret"
3. UPDATE User Secrets

### Telegram Bot

1. Telegram → @BotFather
2. `/mybots` → Select bot → "Revoke current token"
3. UPDATE User Secrets

---

## 📚 Referencias

- **User Secrets**: https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets
- **BFG Repo-Cleaner**: https://rtyley.github.io/bfg-repo-cleaner/
- **GitHub Secret Scanning**: https://docs.github.com/en/code-security/secret-scanning

---

## 📊 Checklist de Resolución

- [x] `.gitignore` actualizado
- [x] Template `.example` creado
- [x] Documentación completa
- [x] **Historial verificado** (COMPLETADO - 2026-03-01)
- [x] **Archivo removido del tracking** (COMPLETADO)
- [x] **Commit de seguridad** (COMPLETADO - git-filter-repo)
- [ ] **User Secrets configurados** (REQUIERE ROTACIÓN MANUAL)
- [ ] **App tested localmente** (PENDIENTE DESPUÉS DE ROTACIÓN)

---

**Responsable**: Usuario  
**Timeline**: Completar ANTES del próximo push  
**Follow-up**: Verificar que `.gitignore` funciona en próximo commit
