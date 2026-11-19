# Sistema de Notificaciones Históricas

> **Verificado contra el código**: 2026-11-19 (versión 1.3.0)

Cada mensaje que la aplicación muestra como Snackbar a un usuario autenticado se guarda además en la tabla `UserNotifications`, y el usuario puede consultarlo desde la campanita de la barra superior.

---

## Arquitectura

```
Domain
  ├── Entities/UserNotifications.cs        (entidad EF Core)
  └── Enums/NotificationSeverity.cs        (Normal, Info, Success, Warning, Error)

Application
  ├── DTOs/UserNotificationDto.cs          (UserNotificationDto + CreateUserNotificationDto)
  └── Interfaces/IUserNotificationService.cs

Infrastructure
  └── Services/UserNotificationService.cs  (implementación con IDbContextFactory)

Web
  ├── Services/NotificationService.cs      (envuelve ISnackbar y guarda el historial)
  └── Components/Shared/
      ├── NotificationBell.razor(.cs)      (badge con contador + toggle del panel)
      └── NotificationPanel.razor(.cs)     (lista en popover)
```

`UserNotificationService` se registra en `AddInfrastructureServices` (`IUserNotificationService`, scoped). `NotificationService` y `UserPreferencesService` se registran en `Program.cs` / `AddInfrastructureServices`.

---

## Base de datos (PostgreSQL)

Tabla `UserNotifications` (esquema de referencia: `docs/schema/postgresql_schema_v1.sql`, migración `InitialPostgreSql`):

| Columna | Tipo | Notas |
|---------|------|-------|
| `Id` | `uuid` | PK, `gen_random_uuid()` |
| `UserId` | `uuid` | FK → `Users(Id)`, `ON DELETE CASCADE` |
| `Type` | `text` | Nombre de `NotificationSeverity` (`Success`, `Error`, ...) |
| `Title` | `text` | Vacío cuando no hay título |
| `Message` | `text` | |
| `IsRead` | `boolean` | |
| `CreatedAt` | `timestamp without time zone` | UTC, por defecto `now() at time zone 'utc'` |

Índice: `IX_UserNotifications_UserId`. La entidad **no** tiene `ReadAt`: el DTO expone `ReadAt` siempre `null`.

---

## Servicios

### `IUserNotificationService` (Application)

| Método | Comportamiento |
|--------|----------------|
| `GetUnreadAsync(userId)` | Hasta 50 no leídas, más recientes primero |
| `GetAllAsync(userId, page = 1, pageSize = 20)` | Paginado (`PagedResult<UserNotificationDto>`), leídas y no leídas |
| `GetUnreadCountAsync(userId)` | Contador para el badge |
| `CreateAsync(dto)` | Crea una notificación |
| `MarkAsReadAsync(id)` / `MarkAllAsReadAsync(userId)` | Marca como leída(s) |
| `DeleteAsync(id)` / `DeleteAllAsync(userId)` | Borra una / todas las del usuario |

### `NotificationService` (Web)

Reemplaza a `ISnackbar.Add` en los componentes:

```
await NotificationService.AddAsync("Registro guardado", Severity.Success)
  1. Severity.Error  → se muestra siempre el Snackbar
  2. Otro severity   → se consulta IUserPreferencesService.GetNotificationsEnabledAsync(userId)
        habilitado   → se muestra el Snackbar
        deshabilitado→ se suprime el Snackbar
        sin sesión   → se muestra (no hay preferencias)
  3. Con sesión iniciada, SIEMPRE se guarda en el historial (IUserNotificationService.CreateAsync)
     Si guardar falla, se registra el error y no se propaga: el historial es secundario.
```

`MudBlazor.Severity` se convierte a `NotificationSeverity` dentro de `NotificationService` (`ConvertToNotificationSeverity`).

---

## Componentes

- **`NotificationBell`**: `MudBadge` con el contador de no leídas; consulta el contador cada **60 segundos** (`System.Threading.Timer`, liberado en `Dispose`) y al cambiar el estado de autenticación; abre/cierra el panel. Se incluye en `MainLayout.razor`.
- **`NotificationPanel`**: lista las últimas 50 notificaciones (leídas y no leídas) con un chip de color por severidad, marcar todas como leídas, borrar todas, borrar una y estados de carga y vacío. Muestra marcas de tiempo relativas ("Hace un momento", "Hace X min", "Hace Xh", "Hace Xd"; a partir de 7 días, fecha completa).

| Severidad | Color MudBlazor |
|-----------|-----------------|
| Normal | `Default` |
| Info | `Info` |
| Success | `Success` |
| Warning | `Warning` |
| Error | `Error` |

---

## Tests

`tests/ControlPeso.Infrastructure.Tests/Services/UserNotificationServiceTests.cs` cubre el servicio de infraestructura. La lógica de `NotificationService` y de los componentes se cubre en `tests/ControlPeso.Web.Tests`.

---

## Troubleshooting

| Síntoma | Causa probable | Qué revisar |
|---------|----------------|-------------|
| El badge no muestra contador | Usuario no autenticado o contador en cero | `AuthenticationState`; `GetUnreadCountAsync` |
| No se guardan notificaciones | El código usa `ISnackbar` directamente en lugar de `NotificationService`, o falló `CreateAsync` | Logs de Serilog: `NotificationService: Error saving notification to history` |
| El Snackbar no aparece | El usuario desactivó las notificaciones en su perfil (los errores se muestran siempre) | `UserPreferences.NotificationsEnabled` |
| El contador tarda en subir | El badge consulta cada 60 s | Normal; el panel actualiza al abrirse |

---

## Limitaciones conocidas

- Las notificaciones no se archivan ni se purgan automáticamente.
- El contador se actualiza por polling, no por push (SignalR).

Referencias: [ARCHITECTURE.md](./ARCHITECTURE.md), [DATABASE.md](./DATABASE.md).
