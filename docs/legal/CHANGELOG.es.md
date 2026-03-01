# Historial de Cambios

Todos los cambios notables de **Control Peso Thiscloud** se documentarán en este archivo.

El formato se basa en [Keep a Changelog](https://keepachangelog.com/es/1.0.0/),
y este proyecto se adhiere al [Versionado Semántico](https://semver.org/lang/es/).

---

## [No publicado]

### Funciones Planificadas

- 🌐 Soporte de idiomas adicionales (Portugués, Francés)
- 📊 Panel de análisis avanzado con rangos de fechas personalizados
- 📱 Soporte de Progressive Web App (PWA) para acceso offline
- 🔔 Notificaciones por email para objetivos de peso e hitos
- 📈 Exportar datos a CSV/Excel
- 🎯 Recordatorios de objetivos de peso y mensajes motivacionales
- 🔄 Importación de datos desde otras apps de seguimiento de peso
- 🏆 Insignias de logros y gamificación
- 👥 Funciones sociales (compartir opcional con amigos/familia)
- 🩺 Integración con plataformas de salud (Apple Health, Google Fit)

---

## [1.3.0] - 2026-02-25

### Agregado

- ✅ **Infraestructura de Despliegue en Producción**:
  - Configuración Docker Compose para producción con SQL Server Express 2022
  - Sistema de backup automático diario con retención de 30 días
  - Scripts de backup: backup-sqlserver.sh, restore-backup.sh, backup-now.sh
  - Script de migración a SQL Server (migrate-sqlite-to-sqlserver.sh)
  - Imagen Docker personalizada de SQL Server con daemon cron para backups programados
  
- 🔐 **Endurecimiento de Seguridad de Aplicación**:
  - HTTP Strict Transport Security (HSTS) con política de 1 año max-age
  - Redirección HTTPS permanente (código de estado 308) para producción
  - Middleware de forwarded headers para soporte de proxy inverso (NPM Plus)
  - Content Security Policy (CSP) con headers restrictivos
  - Documentación de gestión de secretos y plantillas

- 🌐 **SEO y Accesibilidad**:
  - Meta tags completos (Open Graph, Twitter Cards)
  - Actualización de robots.txt y sitemap.xml
  - Atributo lang HTML establecido en es-AR (Español - Argentina)
  - Mejora de estructura HTML semántica

- 📄 **Documentación Legal** (Bilingüe - EN/ES):
  - Política de Privacidad (inspirada en GDPR, contexto argentino)
  - Términos y Condiciones con descargo de responsabilidad completo
  - Licencias de Terceros y agradecimientos
  - Historial de Cambios (este archivo)

- 🚀 **Automatización CI/CD**:
  - Workflow de versionado semántico automático (release.yml)
  - Creación automática de tags y releases de GitHub en push a main
  - Cálculo de versión basado en conventional commits (feat/fix/BREAKING CHANGE)

- 📊 **Infraestructura de Logging**:
  - Integración con ThisCloud.Framework.Loggings (basado en Serilog)
  - Logging estructurado con redacción y IDs de correlación
  - Políticas de rotación y retención de logs

### Cambiado

- 🗄️ **Base de Datos**: Agregado soporte para SQL Server Express 2022 (producción)
- 🐳 **Docker**: Orquestación multi-contenedor con servicios separados de BD y web
- 📦 **NuGet**: Agregado Microsoft.EntityFrameworkCore.SqlServer 10.0.3
- 🔒 **Configuración**: Patrón de variables de entorno para datos sensibles
- 🌍 **Hosting**: Preparado para despliegue con proxy inverso detrás de NPM Plus

### Corregido

- 🔧 Eliminada advertencia de connection string hardcoded del DbContext
- 🔐 Sanitizadas direcciones IP internas, puertos y rutas de la documentación
- 📝 .gitignore ahora excluye correctamente docker-compose.override.yml y archivos .env

### Seguridad

- 🔒 Todos los secretos movidos a variables de entorno y archivos docker-compose override
- 🛡️ Contraseñas de base de datos cifradas con requisitos fuertes
- 🔐 Google OAuth ClientSecret gestionado via User Secrets (dev) y env vars (prod)
- 📊 Google Analytics configurado con anonimización de IP y cookies Secure
- 🚫 Sin detalles sensibles de infraestructura en repositorio público

### Documentación

- 📚 Guía completa de despliegue en producción (plan de 13 pasos)
- 📖 Documentación de configuración de Google OAuth para producción
- 💾 Procedimientos completos de backup y restore
- 🔑 Mejores prácticas de gestión de secretos
- 🏗️ Sanitización de infraestructura (placeholders para IPs, puertos, rutas)
- 📄 Documentación de cumplimiento legal (Privacidad, Términos, Licencias)

---

## [1.2.0] - 2026-02-15

### Agregado

- 🎨 **Gestión de Temas**:
  - Integración de MudBlazor ThemeManager
  - Toggle de tema Oscuro/Claro
  - Paleta de colores personalizable
  - Ajustes de tipografía
  - Vista previa de tema en tiempo real

- 📱 **Diseño Responsivo**:
  - Optimización de layout mobile-first
  - Componentes adaptativos para diferentes tamaños de pantalla
  - Controles amigables para táctil

- 🌐 **Internacionalización**:
  - Soporte bilingüe completo (Español/Inglés)
  - Componente selector de idioma
  - Formato localizado de fechas y números
  - Archivos de recursos para strings de UI

### Cambiado

- 🎨 Actualizado MudBlazor a 8.0.0
- 📊 Mejorado rendimiento de renderizado de gráficos
- 🖼️ Mejorada jerarquía visual en dashboard

---

## [1.1.0] - 2026-02-01

### Agregado

- 📈 **Tendencias y Análisis**:
  - Análisis de tendencia de peso (subida/bajada/neutral)
  - Cálculos estadísticos (promedio, mín, máx)
  - Proyección de peso basada en datos históricos
  - Resúmenes de tendencias semanales y mensuales

- 🔍 **Filtrado Avanzado**:
  - Filtros por rango de fechas
  - Búsqueda por notas
  - Ordenar por fecha, peso o tendencia
  - Paginación para conjuntos de datos grandes

- 💬 **Función de Notas**:
  - Agregar notas personales a entradas de peso
  - Máximo 500 caracteres por nota
  - Editar y eliminar notas

### Corregido

- 🐛 Gráfico de peso no se actualizaba después de agregar nueva entrada
- 🔄 Problema de refresh después de editar log de peso
- 📅 Selector de fecha no respetaba idioma del usuario

---

## [1.0.0] - 2026-01-15

### Agregado - Lanzamiento Inicial

- ✅ **Funciones Principales**:
  - Autenticación Google OAuth 2.0
  - Creación, edición y eliminación de entradas de peso
  - Tabla de historial de peso con fecha, hora, peso y notas
  - Estadísticas básicas de peso (actual, inicial, objetivo, progreso)
  - Gráfico de líneas de visualización de peso a lo largo del tiempo

- 🎨 **Interfaz de Usuario**:
  - Componentes Material Design de MudBlazor
  - Tema oscuro por defecto
  - Layout responsivo
  - Menú de navegación intuitivo

- 🗄️ **Base de Datos**:
  - Base de datos SQLite (enfoque Database First)
  - Entity Framework Core 9.0.1
  - Entidades: User, WeightLog, UserPreference, AuditLog

- 🔐 **Seguridad**:
  - Autenticación Google OAuth (sin almacenamiento de contraseñas)
  - Comunicación HTTPS cifrada
  - Cookies HttpOnly + Secure + SameSite
  - Validación de entrada con FluentValidation

- 📊 **Modelo de Datos**:
  - Gestión de usuarios con roles (Usuario, Administrador)
  - Logs de peso con fecha, hora, peso (kg), unidad de visualización, notas
  - Preferencias de usuario (modo oscuro, notificaciones, zona horaria)
  - Audit log para seguimiento de cambios

- 🌐 **Configuración**:
  - Configuración basada en entorno (Desarrollo/Producción)
  - User Secrets para datos sensibles (desarrollo)
  - Integración de Google Analytics 4

### Stack Técnico

- 🚀 **.NET 10** - Framework .NET más reciente
- 🔥 **Blazor Server** - Renderizado interactivo del lado del servidor
- 🎨 **MudBlazor 8.0.0** - Biblioteca de componentes Material Design
- 🗄️ **Entity Framework Core 9.0.1** - ORM con Database First
- 🔐 **Google OAuth 2.0** - Proveedor de autenticación
- 📊 **Google Analytics 4** - Análisis web
- ✅ **FluentValidation 11.11.0** - Validación de entrada
- 📝 **Serilog** (via ThisCloud.Framework.Loggings) - Logging estructurado

---

## Numeración de Versiones

Este proyecto usa [Versionado Semántico](https://semver.org/lang/es/):

- **MAJOR** versión (X.0.0): Cambios incompatibles de API o cambios arquitectónicos significativos
- **MINOR** versión (0.X.0): Nuevas funciones agregadas de manera retrocompatible
- **PATCH** versión (0.0.X): Correcciones de bugs retrocompatibles

---

## Notas de Lanzamiento

### Cómo Leer Este Historial

- 🚀 **Agregado**: Nuevas características o funcionalidad
- 🔄 **Cambiado**: Cambios en funcionalidad existente
- 🗑️ **Deprecado**: Funciones que se eliminarán en futuros lanzamientos
- ❌ **Eliminado**: Funciones que han sido eliminadas
- 🐛 **Corregido**: Correcciones de bugs
- 🔒 **Seguridad**: Mejoras de seguridad o parches de vulnerabilidades

---

## Contribuir

¡Damos la bienvenida a contribuciones! Si desea contribuir a este proyecto, por favor:

1. Haga fork del repositorio
2. Cree una rama de función (`feature/nombre-de-su-funcion`)
3. Siga [Conventional Commits](https://www.conventionalcommits.org/es/) para mensajes de commit
4. Envíe un pull request

Formato de mensaje de commit:
```
<tipo>(<scope>): <asunto>

Tipos: feat, fix, docs, style, refactor, test, chore, ci
```

Ejemplos:
- `feat(dashboard): agregar gráfico de proyección de peso`
- `fix(auth): resolver problema de redirección de Google OAuth`
- `docs(readme): actualizar instrucciones de instalación`

---

## Soporte

Para preguntas, problemas o solicitudes de funciones:

- **Issues**: [GitHub Issues](https://github.com/mdesantis1984/Control-Peso-Thiscloud/issues)
- **Email**: [correo de contacto por configurar]
- **Sitio Web**: https://controlpeso.thiscloud.com.ar

---

## Licencia

Este proyecto está licenciado bajo la **Licencia MIT** - ver el archivo LICENSE para detalles.

---

**¡Gracias por usar Control Peso Thiscloud!**

Sigue tu peso. Alcanza tus objetivos. Mantente saludable. 💪
