# 📐 Matriz de Contenido para Consolidación

> **Decisiones validadas por usuario** ✅  
> **Próximo paso**: Crear 3 documentos maestros

---

## 🏗️ **1. INFRAESTRUCTURA.MD** (Documento Maestro)

### Estructura Propuesta (TOC)

```markdown
# Infraestructura - Control Peso Thiscloud

## 1. Arquitectura del Sistema
   1.1. Arquitectura Onion (Clean Architecture)
       - Diagrams
       - Layer Dependencies
       - Design Patterns
   1.2. Arquitectura de Avatares (ProtectedBrowserStorage)
       - Upload & Display Flow
       - Security Model

## 2. Deployment & Containerización
   2.1. Deployment Overview (Azure, Docker, IIS)
   2.2. Docker Local Development
       - docker-compose.yml
       - docker-compose.override.yml workflow
   2.3. Docker Production Deployment
       - Build & Transfer workflow
       - MCP SSH automation
   2.4. Configuración de Puertos
       - NPMplus reverse proxy
       - Container ports mapping

## 3. Base de Datos
   3.1. Entity Framework Core Database First
       - Scaffold workflow
       - Schema management (docs/schema/schema_v1.sql)
   3.2. Sistema de Backups Automáticos
       - Backup schedule
       - Retention policy
       - Restore procedures

## 4. SEO & Crawlers
   4.1. SEO Infrastructure (50+ AI Crawlers)
       - Minimal APIs (/robots.txt, /sitemap.xml)
       - Blazor routing precedence solution
   4.2. robots.txt Generation
       - 50+ AI crawlers list
       - Dynamic generation
   4.3. sitemap.xml Generation
       - Dynamic URL discovery
       - Cache strategy

## 5. Seguridad
   5.1. Overview (CSP, HTTPS, Cookies)
   5.2. Google OAuth 2.0 Setup
       - Google Cloud Console configuration
       - Redirect URIs
   5.3. Gestión de Configuración & Secretos
       - User Secrets (Development)
       - Environment Variables (Production)
       - Templates pattern (.template files)
   5.4. Credential Rotation Guide
       - Google OAuth rotation
       - Connection strings rotation

## 6. Analytics
   6.1. Cloudflare Analytics Setup
       - Free tier configuration
       - Dashboard access

## 7. Scripts de Automatización
   7.1. Scripts Preservados
       - clearObjBinVs.ps1 (limpieza)
       - setup-config.ps1 (templates)
       - Deploy-Docker-Local.ps1 (deploy local)
       - deploy-docker.ps1 (deploy producción)
       - migrate-to-sqlserver.ps1 (migración DB)
   7.2. Comandos Inline Frecuentes
       - Docker (build, transfer, deploy)
       - Git (branch management)
       - EF Core (scaffold)
```

### Archivos Fuente a Consolidar (15 archivos)

| Archivo Fuente | Sección Destino | Notas |
|----------------|-----------------|-------|
| ARCHITECTURE.md | 1.1 Arquitectura Onion | Completo |
| AVATAR_ARCHITECTURE.md | 1.2 Arquitectura Avatares | Sección específica |
| DEPLOYMENT.md | 2.1 Deployment Overview | Base principal |
| DOCKER.md | 2.2 Docker Local | Fusionar con DEPLOYMENT.md |
| DOCKER_DEPLOYMENT.md | 2.3 Docker Production | Fusionar con DEPLOYMENT.md |
| PRODUCTION_PORTS_CONFIG.md | 2.4 Puertos | Sección específica |
| DATABASE.md | 3.1 EF Core Database First | Completo |
| BACKUP_SYSTEM.md | 3.2 Backups | Completo |
| SEO_INFRASTRUCTURE.md | 4.1-4.3 SEO completo | **Fuente de verdad SEO** |
| SECURITY.md | 5.1 Overview | Completo |
| GOOGLE_OAUTH_SETUP.md | 5.2 OAuth Setup | Completo |
| SECRETS_MANAGEMENT.md | 5.3 Secretos (parte 1) | Fusionar |
| CONFIG_MANAGEMENT.md | 5.3 Secretos (parte 2) | Fusionar con SECRETS_MANAGEMENT |
| CREDENTIAL_ROTATION_GUIDE.md | 5.4 Credential Rotation | Completo |
| CLOUDFLARE_ANALYTICS.md | 6.1 Analytics | Completo |

**Tamaño estimado**: ~150-180 KB

---

## 📋 **2. DOCUMENTACIONFUNCIONAL.MD** (Documento Maestro)

### Estructura Propuesta (TOC)

```markdown
# Documentación Funcional - Control Peso Thiscloud

## 1. Planes del Proyecto (Referencias)
   1.1. Plan Maestro v1.0 → Ver: Plan_ControlPeso_Thiscloud_v1_0.md
   1.2. Fase 9 Pixel Perfect → Ver: Plan_Fase_9_Pixel_Perfect.md
   1.3. Fase 10 Globalización → Ver: Plan_Fase_10_Globalizacion.md

## 2. Sistemas Implementados
   2.1. Sistema de Notificaciones
       - Arquitectura (observer pattern)
       - NotificationService
       - Componentes UI (MudSnackbar, BellIcon)
       - Diagramas de flujo (ASCII art)
   2.2. Integración ThisCloud Framework
       - ThisCloud.Framework.Loggings análisis
       - Decisión: usar solo Loggings, no Web
       - Logging estructurado con Serilog

## 3. Accesibilidad & UI
   3.1. Auditoría WCAG AA
       - Criterios evaluados
       - Score actual
       - Recomendaciones

## 4. Testing & Cobertura
   4.1. Test Coverage Report
       - Cobertura por capa: 88.9% global
       - Unit tests: Domain, Application, Infrastructure
       - Integration tests: Web (Blazor components)
   4.2. Testing Strategy
       - xUnit + Moq/NSubstitute
       - bUnit para componentes Blazor
```

### Archivos Fuente a Consolidar (4 archivos)

| Archivo Fuente | Sección Destino | Notas |
|----------------|-----------------|-------|
| NOTIFICATION_SYSTEM.md | 2.1 Sistema Notificaciones (base) | Arquitectura principal |
| NOTIFICATION_SYSTEM_DIAGRAMS.md | 2.1 Sistema Notificaciones (diagramas) | Fusionar con NOTIFICATION_SYSTEM |
| THISCLOUD_FRAMEWORK_INTEGRATION.md | 2.2 Integración Framework | Completo |
| WCAG_AA_AUDIT.md | 3.1 Auditoría WCAG | Completo |
| TEST_COVERAGE_REPORT.md | 4.1-4.2 Testing | Completo |

**Archivos PRESERVADOS (no consolidar, solo referenciar)**:
- ✅ Plan_ControlPeso_Thiscloud_v1_0.md (97 KB) → Histórico
- ✅ Plan_Fase_10_Globalizacion.md (67 KB) → Histórico
- ✅ Plan_Fase_9_Pixel_Perfect.md (34 KB) → Histórico

**Tamaño estimado**: ~90-100 KB (sin contar Planes)

---

## 🔧 **3. SOPORTE.MD** (Documento Maestro)

### Estructura Propuesta (TOC)

```markdown
# Soporte & Troubleshooting - Control Peso Thiscloud

## 1. Logging & Diagnóstico
   1.1. Best Practices
       - Logging estructurado con ILogger<T>
       - Parámetros nombrados (NO string interpolation)
       - Niveles de log (Verbose, Debug, Info, Warning, Error, Critical)
       - Redaction automática (secretos, PII)
   1.2. Query Guide
       - Docker logs commands
       - Filtering con emojis (🤖🗺️✅❌)
       - Correlation ID tracking
   1.3. ThisCloud.Framework.Loggings
       - Serilog configuration
       - File rolling policy
       - Structured logging ejemplos

## 2. Telegram Bot
   2.1. Quickstart
       - BotFather setup
       - Token configuration
       - Webhook vs Polling
   2.2. Troubleshooting
       - Common errors
       - Debugging tips

## 3. Blazor Técnico
   3.1. Prerendering & JS Interop
       - OnAfterRender lifecycle
       - IJSRuntime injection
       - LocalStorage access patterns
   3.2. Unit System Global State Management
       - ProtectedBrowserStorage
       - State persistence
       - Component communication

## 4. Comandos & Scripts de Referencia
   4.1. Docker Commands
       - Build, transfer, deploy workflow
       - Container management
       - Logs & debugging
   4.2. Git Commands
       - Branch management (Git Flow)
       - Feature branch workflow
       - Cleanup commands
   4.3. EF Core Commands
       - Scaffold workflow
       - Migration alternativas (Database First)
   4.4. Scripts Preservados
       - clearObjBinVs.ps1
       - setup-config.ps1
       - Deploy-Docker-Local.ps1
       - deploy-docker.ps1
       - migrate-to-sqlserver.ps1
```

### Archivos Fuente a Consolidar (6 archivos)

| Archivo Fuente | Sección Destino | Notas |
|----------------|-----------------|-------|
| LOGGING_BEST_PRACTICES.md | 1.1 Best Practices | Completo |
| LOGGING_QUERY_GUIDE.md | 1.2 Query Guide | Completo |
| TELEGRAM_QUICKSTART.md | 2.1 Quickstart | Completo (incluye TELEGRAM_SETUP.md) |
| TELEGRAM_TROUBLESHOOTING.md | 2.2 Troubleshooting | Completo |
| BLAZOR_PRERENDERING_JS_INTEROP.md | 3.1 Prerendering | Completo |
| UNIT_SYSTEM_GLOBAL_STATE_MANAGEMENT.md | 3.2 Global State | Completo |

**Tamaño estimado**: ~60-70 KB

---

## 🗑️ Archivos a ELIMINAR (27 total)

### Documentos .md OBSOLETOS (23 archivos)

```
✅ APROBADO POR USUARIO - ELIMINAR:

docs/SEO.md
docs/RESUMEN_SEO_ULTRA_COMPLETO.md
docs/SEO_DEPLOYMENT_CHECKLIST.md
docs/DOCKER.md
docs/DOCKER_DEPLOYMENT.md
docs/GOOGLE_OAUTH_PRODUCTION.md
docs/SECURITY_INCIDENT.md
docs/PREPRODUCTION_SETUP_COMPLETE.md
docs/FASE_9_NOTIFICATION_SYSTEM_SUMMARY.md
docs/NOTIFICATION_SYSTEM_DIAGRAMS.md
docs/LIGHTHOUSE_REPORT.md
docs/UI_DISCREPANCIES.md
docs/VISUAL_ISSUES_CHECKLIST.md
docs/LOGGING_FIXES_SUMMARY.md
docs/LOGGING_PHASE2_SUMMARY.md
docs/Fix_ButtonGroup_CSS_2026-02-28.md
docs/Fix_N1_Query_UserService_Caching_2026-02-28.md
docs/AVATAR_FIX_AND_PRODUCTION_PORTS.md
docs/AVATAR_PERSISTENCE_FIX.md
docs/PROFILE_DTO_REFACTORING.md
docs/TELEGRAM_SETUP.md
docs/technical/NOTIFICATION_DUPLICATION_FIX.md
docs/technical/PROFILE_PERSISTENCE_FIX.md
```

### Scripts .ps1 OBSOLETOS (4 archivos)

```
✅ APROBADO POR USUARIO - ELIMINAR:

fix_tests.ps1
test-seo-endpoints.ps1
scripts/smoke-tests.ps1
scripts/Validate-GoogleOAuthConfig.ps1
```

---

## 📊 Matriz Cruzada de Redundancias

| Tema | Archivos Fuente | Archivo Destino | Decisión |
|------|----------------|-----------------|----------|
| **Docker** | DOCKER.md + DOCKER_DEPLOYMENT.md + DEPLOYMENT.md | infraestructura.md §2 | Fusionar 3 → 1 |
| **SEO** | SEO.md + SEO_INFRASTRUCTURE.md + RESUMEN_SEO_ULTRA_COMPLETO.md | infraestructura.md §4 | Solo SEO_INFRASTRUCTURE (más actualizado) |
| **OAuth** | GOOGLE_OAUTH_SETUP.md + GOOGLE_OAUTH_PRODUCTION.md | infraestructura.md §5.2 | Fusionar 2 → 1 |
| **Secretos** | SECRETS_MANAGEMENT.md + CONFIG_MANAGEMENT.md | infraestructura.md §5.3 | Fusionar 2 → 1 |
| **Telegram** | TELEGRAM_SETUP.md + TELEGRAM_QUICKSTART.md | soporte.md §2.1 | Solo QUICKSTART (más conciso) |
| **Notificaciones** | NOTIFICATION_SYSTEM.md + NOTIFICATION_SYSTEM_DIAGRAMS.md | documentacionfuncional.md §2.1 | Fusionar 2 → 1 |

---

## ✅ Orden de Ejecución (Próximos Steps)

1. **Crear infraestructura.md** (consolidar 15 archivos)
2. **Crear documentacionfuncional.md** (consolidar 4 archivos, referenciar 3 Planes)
3. **Crear soporte.md** (consolidar 6 archivos)
4. **Eliminar 23 archivos .md obsoletos**
5. **Eliminar 4 scripts .ps1 obsoletos**
6. **Actualizar .github/copilot-instructions.md** (referencias a nuevos docs)

---

## 🎯 Criterio de Éxito

- ✅ 3 documentos maestros creados y validados
- ✅ 27 archivos obsoletos eliminados (23 .md + 4 .ps1)
- ✅ 3 Planes preservados como histórico
- ✅ 8 documentos legales intactos
- ✅ 5 scripts .ps1 core preservados
- ✅ copilot-instructions.md actualizado
- ✅ Cero impacto en funcionalidad
- ✅ Cero impacto en código fuente

**RESULTADO FINAL**: De 56 archivos .md → 14 archivos (.md organizados (3 maestros + 3 planes + 8 legales)
