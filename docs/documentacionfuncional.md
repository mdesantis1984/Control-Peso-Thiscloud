# Documentación Funcional - Control Peso Thiscloud

> **Verificado contra el código**: 2026-11-19 (versión 9.0.0)

Qué hace la aplicación, qué reglas aplica y cómo se prueba. La arquitectura técnica está en [ARCHITECTURE.md](./ARCHITECTURE.md); la infraestructura y el despliegue en [DEPLOYMENT.md](./DEPLOYMENT.md) y [infraestructura.md](./infraestructura.md).

## Tabla de Contenido

1. [Funcionalidades](#1-funcionalidades)
2. [Reglas de negocio y validaciones](#2-reglas-de-negocio-y-validaciones)
3. [Sistemas transversales](#3-sistemas-transversales)
4. [Accesibilidad y UI](#4-accesibilidad-y-ui)
5. [Testing](#5-testing)

---

## 1. Funcionalidades

| Ruta | Acceso | Descripción |
|------|--------|-------------|
| `/` | Público | Página de inicio. |
| `/login`, `/logout` | Público / autenticado | Inicio de sesión con Google (único método; LinkedIn solo si está configurado). La sesión persiste 30 días con renovación deslizante. |
| `/dashboard` | Autenticado | Resumen: peso actual, cambio, progreso hacia el objetivo, tendencia y gráfico; alta rápida de registros. |
| `/history` | Autenticado | Historial paginado de registros de peso con filtros, edición y borrado. |
| `/trends` | Autenticado | Análisis de tendencias (promedios, variaciones y proyección) por período. |
| `/profile` | Autenticado | Datos personales, altura, fecha de nacimiento, peso objetivo, sistema de unidades, idioma, tema, notificaciones y avatar. |
| `/admin` | Rol Administrador | Gestión de usuarios (rol y estado) y exportación CSV (CsvHelper). |
| `/privacy`, `/terms`, `/licenses`, `/changelog` | Público | Páginas legales y changelog (alias en español: `/privacidad`, `/terminos`, `/licencias`, `/historial`). |
| `/diag-auth` | Diagnóstico | Información de autenticación para soporte. |
| `/health`, `/health/ready` | Público | Vivacidad y disponibilidad de la base de datos. |
| `/robots.txt`, `/sitemap.xml` | Público | Generados dinámicamente ([SEO_INFRASTRUCTURE.md](./SEO_INFRASTRUCTURE.md)). |

Las páginas de demostración (Counter, Weather, páginas de prueba) fueron eliminadas.

### Registros de peso

- Un usuario crea, edita y elimina sus propios registros (fecha, hora, peso, nota opcional). El peso se guarda **siempre en kg**; la unidad de visualización depende del perfil ([technical/UNIT_SYSTEM_GLOBAL_STATE_MANAGEMENT.md](./technical/UNIT_SYSTEM_GLOBAL_STATE_MANAGEMENT.md)).
- Cada operación muestra un aviso en pantalla y se guarda en el historial de notificaciones del usuario ([NOTIFICATION_SYSTEM.md](./NOTIFICATION_SYSTEM.md)).

### Perfil y avatar

- El perfil se valida con `UpdateUserProfileValidator`. El avatar se gestiona como se describe en [AVATAR_ARCHITECTURE.md](./AVATAR_ARCHITECTURE.md).
- Cambiar idioma, tema o unidades se refleja de inmediato en toda la interfaz.

### Administración

- Solo usuarios con rol Administrador acceden a `/admin`. Los cambios de rol y de estado de un usuario se registran en la tabla `AuditLog` (`AdminService`).

---

## 2. Reglas de negocio y validaciones

Las reglas viven en `src/ControlPeso.Application/Validators/` (FluentValidation 12) y sus mensajes se localizan (es-AR / en-US).

| Entidad | Regla |
|---------|-------|
| Registro de peso (alta/edición) | Peso entre **20 y 500 kg**; nota de hasta **500** caracteres; fecha y hora obligatorias; unidad de visualización válida. |
| Perfil | Nombre de 1 a **200** caracteres; altura entre **50 y 300 cm**; fecha de nacimiento no futura y dentro de los últimos 150 años; idioma `es` o `en`; peso objetivo entre **20 y 500 kg**. |

Las validaciones se ejecutan en la capa Application, de modo que se aplican igual a cualquier cliente de los servicios.

---

## 3. Sistemas transversales

| Sistema | Resumen | Documento |
|---------|---------|-----------|
| Notificaciones | Historial persistido por usuario, campana con sondeo cada 60 s y snackbars. | [NOTIFICATION_SYSTEM.md](./NOTIFICATION_SYSTEM.md) |
| Logging | ThisCloud.Framework.Loggings sobre Serilog; NDJSON con redacción y correlación. **Es la única vía de registro de errores** (no hay alertas externas). | [THISCLOUD_FRAMEWORK_INTEGRATION.md](./THISCLOUD_FRAMEWORK_INTEGRATION.md), [LOGGING_BEST_PRACTICES.md](./LOGGING_BEST_PRACTICES.md), [LOGGING_QUERY_GUIDE.md](./LOGGING_QUERY_GUIDE.md) |
| Internacionalización | Cultura por defecto `es-AR`, soporte `en-US`; recursos en `ControlPeso.Shared.Resources`. | [ARCHITECTURE.md](./ARCHITECTURE.md) |
| Tema y unidades | Modo claro/oscuro y sistema Métrico/Imperial, guardados por usuario. | [technical/](./technical/) |
| SEO | `robots.txt` y `sitemap.xml` dinámicos. | [SEO_INFRASTRUCTURE.md](./SEO_INFRASTRUCTURE.md) |
| Analytics | Google Analytics 4 solo si está configurado. | [CLOUDFLARE_ANALYTICS.md](./CLOUDFLARE_ANALYTICS.md) |

---

## 4. Accesibilidad y UI

Criterios que el código aplica (nivel WCAG 2.1 AA como objetivo):

- `<html lang="es-AR">` y componentes MudBlazor, que traen soporte de teclado y atributos ARIA por defecto.
- Imágenes con texto alternativo (por ejemplo el avatar usa el nombre del usuario).
- Modo claro y oscuro con contraste definido en el tema MudBlazor.
- Mensajes de validación localizados (es/en).

Pendientes conocidos: no existe un enlace "saltar al contenido" ni una auditoría automatizada de contraste en CI. La auditoría manual WCAG anterior era una foto puntual y se retiró de la documentación; ante cambios de UI, repetir una revisión con Lighthouse.

---

## 5. Testing

Cinco proyectos en `tests/` (xUnit, FluentAssertions, Moq y NSubstitute; bUnit para componentes):

| Proyecto | Foco |
|----------|------|
| `ControlPeso.Domain.Tests` | Entidades, enums y excepciones. |
| `ControlPeso.Application.Tests` | Servicios, validadores (incluida su localización) y mappers. |
| `ControlPeso.Infrastructure.Tests` | Servicios de infraestructura y acceso a datos; las pruebas contra PostgreSQL usan Testcontainers (requieren Docker); el procesamiento de imágenes con SkiaSharp se prueba en `Services/ImageProcessingServiceTests.cs`. |
| `ControlPeso.Shared.Resources.Tests` | Completitud y consistencia de los recursos es/en. |
| `ControlPeso.Web.Tests` | Páginas y componentes con bUnit, middleware y extensiones. Usan SQLite/InMemory solo dentro de las pruebas. |

Hay del orden de 700 métodos de prueba (`[Fact]`/`[Theory]`); el número exacto de casos lo informa `dotnet test`.

```bash
dotnet test                                  # toda la solución
dotnet test tests/ControlPeso.Application.Tests
dotnet test --collect:"XPlat Code Coverage"  # con cobertura (coverlet)
```

La CI ejecuta build, tests, verificación de paquetes vulnerables, gitleaks sobre el diff de los pull requests y un smoke test de Docker ([DEPLOYMENT.md](./DEPLOYMENT.md)). No se versiona un informe de cobertura: se genera bajo demanda con el comando anterior.

---

## Referencias

- [ARCHITECTURE.md](./ARCHITECTURE.md) · [DATABASE.md](./DATABASE.md) · [SECURITY.md](./SECURITY.md)
- [soporte.md](./soporte.md) para logging, troubleshooting y comandos habituales.
