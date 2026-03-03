# Configuration Management - Control Peso Thiscloud

## 📁 Sistema de Configuración con Templates

Para evitar exponer secretos en el repositorio y mantener la configuración entre versiones, usamos **template files**.

---

## 🎯 Cómo Funciona

### Archivos en el Repositorio (Committed):
- ✅ `appsettings.json` - Configuración base (sin secretos)
- ✅ `appsettings.Development.json.template` - Template de desarrollo
- ✅ `appsettings.Production.json.template` - Template de producción
- ✅ `setup-config.ps1` - Script de setup automático

### Archivos Locales (En .gitignore):
- 🔒 `appsettings.Development.json` - Tu configuración real de desarrollo
- 🔒 `appsettings.Production.json` - Tu configuración real de producción

**Resultado**: Los templates se commitean, los archivos con secretos NO.

---

## 🚀 Setup Inicial (Primera vez o después de clonar)

### 1. Ejecutar script de setup:
```powershell
pwsh setup-config.ps1
```

Esto crea los archivos de configuración desde los templates si no existen.

### 2. Editar archivos creados:
```powershell
# Editar configuración de desarrollo
code src/ControlPeso.Web/appsettings.Development.json

# Editar configuración de producción
code src/ControlPeso.Web/appsettings.Production.json
```

### 3. Reemplazar placeholders:
Busca y reemplaza TODOS los valores `YOUR_*_HERE` con tus valores reales:

#### Development (`appsettings.Development.json`):
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID_HERE",      // ← REEMPLAZAR
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET_HERE" // ← REEMPLAZAR
    }
  },
  "Telegram": {
    "BotToken": "YOUR_TELEGRAM_BOT_TOKEN_HERE",     // ← REEMPLAZAR
    "ChatId": "YOUR_TELEGRAM_CHAT_ID_HERE"          // ← REEMPLAZAR
  }
}
```

#### Production (`appsettings.Production.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Password=YOUR_SQL_PASSWORD_HERE;" // ← REEMPLAZAR
  },
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID_HERE",      // ← REEMPLAZAR
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET_HERE" // ← REEMPLAZAR
    }
  },
  "GoogleAnalytics": {
    "MeasurementId": "YOUR_GA_MEASUREMENT_ID_HERE"   // ← REEMPLAZAR
  }
}
```

---

## 🔄 Workflow de Desarrollo

### Clonaste el repo por primera vez:
```bash
git clone https://github.com/mdesantis1984/Control-Peso-Thiscloud.git
cd Control-Peso-Thiscloud
pwsh setup-config.ps1  # Crea archivos desde templates
# Editar archivos y reemplazar placeholders
dotnet run --project src/ControlPeso.Web
```

### Cambiaste de rama:
```bash
git checkout develop
pwsh setup-config.ps1  # Verifica/crea archivos si no existen
dotnet run --project src/ControlPeso.Web
```

### Actualizaste desde main/develop:
```bash
git pull origin develop
pwsh setup-config.ps1  # Verifica archivos
dotnet run --project src/ControlPeso.Web
```

**TUS ARCHIVOS DE CONFIGURACIÓN NUNCA SE PIERDEN** porque están en `.gitignore` (git no los toca).

---

## 🔒 Seguridad

### ✅ Archivos SEGUROS para commitear:
- `appsettings.json` (solo config base sin secretos)
- `*.template` files (placeholders, no valores reales)
- `setup-config.ps1` (script de setup)

### ❌ Archivos QUE NUNCA SE DEBEN COMMITEAR:
- `appsettings.Development.json` (tiene secretos reales)
- `appsettings.Production.json` (tiene secretos reales)
- `appsettings.*.json` (cualquier ambiente específico)

**Nota**: `.gitignore` ya está configurado para ignorar estos archivos automáticamente.

---

## 📝 Actualizar Templates (Para maintainers)

Si necesitás agregar nueva configuración que todos los devs deben tener:

### 1. Actualizar template:
```powershell
# Editar el template (NO el archivo real)
code src/ControlPeso.Web/appsettings.Development.json.template
```

### 2. Agregar nueva sección con placeholder:
```json
{
  "NewService": {
    "ApiKey": "YOUR_NEW_SERVICE_API_KEY_HERE",
    "Endpoint": "https://api.example.com"
  }
}
```

### 3. Commitear el template actualizado:
```bash
git add src/ControlPeso.Web/appsettings.Development.json.template
git commit -m "feat(config): add NewService configuration to template"
git push
```

### 4. Notificar al equipo:
- Los devs ejecutan `pwsh setup-config.ps1` (detecta cambios)
- Editan su archivo local y agregan el nuevo valor real
- El script NO sobreescribe archivos existentes (safe)

---

## 🛠️ Troubleshooting

### Problema: "Template file not found"
**Causa**: Falta el archivo `.template` en el repo.

**Solución**:
```bash
git pull origin develop  # Asegurar templates actualizados
pwsh setup-config.ps1
```

### Problema: "Configuration files are in .gitignore"
**Causa**: Intentaste commitear archivos con secretos.

**Solución**: Esto es CORRECTO. Los archivos están protegidos intencionalmente.

### Problema: "Perdí mi configuración local"
**Causa**: Borraste accidentalmente los archivos de config.

**Solución**:
```powershell
pwsh setup-config.ps1  # Re-crear desde templates
# Editar y reemplazar placeholders nuevamente
```

**Prevención**: Hacer backup de tus archivos de config locales en un lugar seguro (fuera del repo).

### Problema: "Cambié de branch y la app no arranca"
**Causa**: El branch nuevo tiene configuración diferente.

**Solución**:
```bash
git checkout <branch>
pwsh setup-config.ps1  # Verificar/crear archivos
# Editar si hay nuevas configuraciones requeridas
dotnet run --project src/ControlPeso.Web
```

---

## 📊 Estructura de Archivos

```
ControlPeso.Thiscloud/
├── src/
│   └── ControlPeso.Web/
│       ├── appsettings.json                         ✅ Committed (base config)
│       ├── appsettings.Development.json             🔒 Local (gitignored)
│       ├── appsettings.Development.json.template    ✅ Committed (template)
│       ├── appsettings.Production.json              🔒 Local (gitignored)
│       └── appsettings.Production.json.template     ✅ Committed (template)
├── setup-config.ps1                                 ✅ Committed (setup script)
├── docs/
│   └── CONFIG_MANAGEMENT.md                         ✅ Committed (this file)
└── .gitignore                                       ✅ Committed (ignores secrets)
```

---

## 🎓 Best Practices

### Para Developers:
1. ✅ **NUNCA** commitear archivos con secretos reales
2. ✅ Ejecutar `setup-config.ps1` después de cada `git clone` o `git checkout`
3. ✅ Hacer backup de tus archivos de config locales (fuera del repo)
4. ✅ Usar User Secrets en desarrollo cuando sea posible
5. ✅ Reportar si falta alguna configuración en los templates

### Para Maintainers:
1. ✅ Actualizar templates cuando agregues nuevas configuraciones
2. ✅ Documentar placeholders claramente (`YOUR_*_HERE`)
3. ✅ Notificar al equipo sobre cambios de configuración
4. ✅ Revisar PRs para asegurar que no se commitean secretos
5. ✅ Mantener `.gitignore` actualizado

---

## 🚨 Emergency: "Commiteé secretos accidentalmente"

Si commiteaste secretos por error:

### 1. Rotar secretos INMEDIATAMENTE:
- Regenerar Google OAuth Client Secret
- Regenerar Telegram Bot Token
- Cambiar SQL Server password
- Regenerar API keys

### 2. Limpiar historial de Git:
```bash
# Usar BFG Repo-Cleaner o git-filter-branch
# Contactar al lead para asistencia
```

### 3. Force push (después de limpiar):
```bash
git push origin <branch> --force
```

### 4. Notificar al equipo:
- Informar sobre el incidente
- Solicitar que todos hagan `git pull --rebase`

---

## 📞 Soporte

Para consultas sobre configuración:
- **Documentation**: `docs/CONFIG_MANAGEMENT.md` (este archivo)
- **Setup Script**: `pwsh setup-config.ps1`
- **Repository**: https://github.com/mdesantis1984/Control-Peso-Thiscloud

---

**Last Updated**: 2026-02-28  
**Version**: 1.0.0  
**Maintainer**: Control Peso Thiscloud Team
