# 🔒 Gestión Segura de Credenciales en Docker Compose

## ⚠️ IMPORTANTE - Flujo de Seguridad

Este proyecto **NO incluye credenciales sensibles** en el repositorio Git por razones de seguridad.

### Archivos y su Propósito

| Archivo | En Git? | Propósito |
|---------|---------|-----------|
| `docker-compose.yml` | ✅ SÍ | Configuración base (sin secrets) |
| `docker-compose.override.yml` | ❌ NO | **TUS credenciales reales** (gitignored) |
| `docker-compose.override.yml.example` | ✅ SÍ | Template de ejemplo |

### Configuración Inicial (Primera vez)

```bash
# 1. Copiar el template
cp docker-compose.override.yml.example docker-compose.override.yml

# 2. Editar con TUS credenciales REALES
nano docker-compose.override.yml  # o notepad/vim/code

# 3. Docker Compose combina automáticamente ambos archivos
docker-compose up -d
```

### ¿Qué hace Docker Compose automáticamente?

Cuando ejecutas `docker-compose up`, Docker **combina** ambos archivos:

```
docker-compose.yml              docker-compose.override.yml
(configuración base)      +     (tus credenciales)
      ↓                               ↓
          COMBINACIÓN AUTOMÁTICA
                  ↓
      Contenedor con TODO configurado
```

### Ejemplo de docker-compose.override.yml

```yaml
services:
  controlpeso-web:
    environment:
      # Google OAuth (OBLIGATORIO)
      - Authentication__Google__ClientId=123456789-abc...apps.googleusercontent.com
      - Authentication__Google__ClientSecret=GOCSPX-abcdefghijklmnopqrstuvwxyz
      
      # LinkedIn OAuth (OBLIGATORIO)
      - Authentication__LinkedIn__ClientId=abcdefghijklmn
      - Authentication__LinkedIn__ClientSecret=abcdefghijklmnopqrstuvwxyz
      
      # Google Analytics 4 (OPCIONAL)
      - GoogleAnalytics__MeasurementId=G-XXXXXXXXXX
      
      # Cloudflare Analytics (OPCIONAL)
      - CloudflareAnalytics__Token=your_token_here
```

## 🔑 Obtener Credenciales OAuth

### Google OAuth 2.0

1. Ve a [Google Cloud Console](https://console.cloud.google.com/apis/credentials)
2. Crea un proyecto → Credenciales → OAuth 2.0 Client ID
3. Tipo: **Aplicación web**
4. Redirect URLs:
   - `http://localhost:8080/signin-google`
   - `http://localhost:8080/auth/callback/google`
5. Copia Client ID y Client Secret a `docker-compose.override.yml`

### LinkedIn OAuth

1. Ve a [LinkedIn Developers](https://www.linkedin.com/developers/apps)
2. Crea una aplicación → Auth → OAuth 2.0 settings
3. Redirect URLs:
   - `http://localhost:8080/signin-linkedin`
   - `http://localhost:8080/auth/callback/linkedin`
4. Permisos: `openid`, `profile`, `email`
5. Copia Client ID y Client Secret a `docker-compose.override.yml`

## ❌ NO HACER NUNCA

```bash
# ❌ MAL - Commitear credenciales al repositorio
git add docker-compose.override.yml
git commit -m "add credentials"  # ← PELIGRO! Expone secrets

# ❌ MAL - Hardcodear credenciales en docker-compose.yml
services:
  app:
    environment:
      - SECRET=my_real_secret_here  # ← docker-compose.yml va a Git!
```

## ✅ HACER SIEMPRE

```bash
# ✅ BIEN - Verificar que override NO esté en staging
git status
# → Untracked files: docker-compose.override.yml (correcto, no debe aparecer)

# ✅ BIEN - Verificar .gitignore
cat .gitignore | grep override
# → docker-compose.override.yml (confirmado que está ignorado)

# ✅ BIEN - Usar el template
cp docker-compose.override.yml.example docker-compose.override.yml
```

## 🛡️ Verificación de Seguridad

### Antes de hacer commit

```bash
# Verificar que override NO esté staged
git status

# Si aparece, hacer unstage
git reset HEAD docker-compose.override.yml

# Verificar .gitignore
git check-ignore -v docker-compose.override.yml
# → .gitignore:XXX:docker-compose.override.yml
```

### Si accidentalmente commitaste credenciales

```bash
# ⚠️ Acción URGENTE si expusiste secrets

# 1. Remover del historial (si no has pusheado)
git reset --soft HEAD~1
git restore --staged docker-compose.override.yml

# 2. Si ya pusheaste, ROTAR INMEDIATAMENTE las credenciales:
#    - Generar nuevos Client ID/Secret en Google/LinkedIn
#    - Eliminar los credenciales comprometidos
#    - Actualizar docker-compose.override.yml con nuevos valores

# 3. Limpiar historial (avanzado)
git filter-branch --force --index-filter \
  "git rm --cached --ignore-unmatch docker-compose.override.yml" \
  --prune-empty --tag-name-filter cat -- --all
```

## 📚 Recursos Adicionales

- [Documentación completa Docker](docs/DOCKER.md)
- [Best Practices Secrets Management](https://docs.docker.com/compose/use-secrets/)
- [.gitignore patterns](https://git-scm.com/docs/gitignore)

---

**¿Preguntas?** Abre un issue en GitHub.
