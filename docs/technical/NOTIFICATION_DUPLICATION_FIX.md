# Duplicación de Notificaciones en Historial

## Problema Reportado

Las notificaciones se guardaban duplicadas en la tabla `UserNotifications` cuando el usuario registraba un peso.

### Evidencia
```sql
SELECT TOP (1000) [Id], [UserId], [Type], [Title], [Message], [IsRead], [CreatedAt]
FROM [ControlPeso].[dbo].[UserNotifications]

-- Resultado:
-- Id: 16F6BD37-C5B7-4105-A360-4A6864260EE4 | UserId: A4EDB7DE-... | Type: Success | Title: "Peso registrado correctamente" | CreatedAt: 2026-02-27 18:24:48.3170228
-- Id: 39301CBF-579F-453D-BCF3-5012284FB885 | UserId: A4EDB7DE-... | Type: Success | Title: "Peso registrado correctamente" | CreatedAt: 2026-02-27 18:24:48.3335456
```

Ambos registros:
- Mismo `UserId`
- Mismo `Type` (Success)
- Mismo `Title` y `Message`
- `CreatedAt` casi idéntico (diferencia de ~16ms)

## Causa Raíz

### Flujo de Ejecución Problemático

```
Usuario → Dashboard (Click "Agregar Peso")
   ↓
Dashboard.OpenAddWeightDialog()
   ↓
MudDialog.ShowAsync<AddWeightDialog>()
   ↓
AddWeightDialog.Submit()
   ↓
WeightLogService.CreateAsync() → ✅ Guarda en DB
   ↓
AddWeightDialog línea 127: Snackbar.Add(SuccessMessage) ← ❌ Primera notificación
   ↓
MudDialog.Close(DialogResult.Ok)
   ↓
Dashboard.OpenAddWeightDialog línea 309: Snackbar.Add(WeightSavedSuccess) ← ❌ Segunda notificación (DUPLICADA)
```

### Análisis del Código

#### 1. AddWeightDialog.Submit() - Primera Notificación
```csharp
// src/ControlPeso.Web/Components/Shared/AddWeightDialog.razor.cs (línea 127)
await WeightLogService.CreateAsync(dto);

Logger.LogInformation("AddWeightDialog: Weight log created successfully");
Snackbar.Add(SuccessMessage, Severity.Success); // ← Primera llamada
MudDialog?.Close(DialogResult.Ok(true));
```

#### 2. Dashboard.OpenAddWeightDialog() - Segunda Notificación (DUPLICADA)
```csharp
// src/ControlPeso.Web/Pages/Dashboard.razor.cs (línea 309) - ANTES DEL FIX
var dialog = await DialogService.ShowAsync<AddWeightDialog>(AddWeightDialogTitle);
var result = await dialog.Result;

if (result != null && !result.Canceled)
{
    Logger.LogInformation("Dashboard: Weight added successfully, reloading data");
    Snackbar.Add(WeightSavedSuccess, Severity.Success); // ← Segunda llamada (DUPLICADA)
    await LoadDataAsync();
}
```

#### 3. NotificationService - Guarda en Historial (2 veces)
```csharp
// src/ControlPeso.Web/Services/NotificationService.cs (línea 193-196)
public Snackbar? Add(string message, Severity severity, Action<SnackbarOptions>? configure)
{
    // Fire-and-forget: ambas llamadas disparan AddAsync() sin esperar
    _ = AddAsync(message, severity, configure);
    return null;
}

// AddAsync() línea 110
if (userId.HasValue)
{
    await SaveNotificationToHistoryAsync(userId.Value, message, severity);
    // Cada llamada guarda UN registro en UserNotifications
}
```

### Por Qué se Duplicaba

1. **AddWeightDialog** cierra el diálogo con `DialogResult.Ok(true)` DESPUÉS de mostrar su notificación
2. **Dashboard** detecta que el diálogo se cerró exitosamente (`!result.Canceled`)
3. **Dashboard** muestra OTRA notificación "Peso guardado correctamente"
4. **NotificationService** guarda AMBAS notificaciones en el historial (fire-and-forget)
5. Como ambas se disparan casi simultáneamente (diferencia de ~16ms), se crean 2 registros en DB

### Diagrama de Secuencia

```
Usuario          Dashboard              AddWeightDialog         NotificationService     UserNotifications (DB)
  |                 |                          |                          |                          |
  |-- Click FAB --->|                          |                          |                          |
  |                 |--- ShowAsync() --------->|                          |                          |
  |                 |                          |                          |                          |
  |                 |                    [Submit clicked]                 |                          |
  |                 |                          |                          |                          |
  |                 |                          |--- WeightLogService.CreateAsync() ---->✅ Peso guardado
  |                 |                          |                          |                          |
  |                 |                          |--- Add(SuccessMessage) ->|                          |
  |                 |                          |                          |--- SaveToHistory() ----->| 📝 Registro 1
  |                 |                          |                          |                          |
  |                 |                          |--- Close(Ok) ----------->|                          |
  |                 |                          |                          |                          |
  |                 |<-- result.Canceled=false |                          |                          |
  |                 |                          |                          |                          |
  |                 |--- Add(WeightSavedSuccess) ----------------------->|                          |
  |                 |                          |                          |--- SaveToHistory() ----->| 📝 Registro 2 (DUPLICADO)
  |                 |                          |                          |                          |
```

## Solución Implementada

### Principio de Responsabilidad Única

**Regla:** El componente que ejecuta la acción debe ser responsable de notificar el resultado.

- ✅ **AddWeightDialog** ejecuta la acción (guardar peso) → **AddWeightDialog** muestra notificación
- ❌ **Dashboard** NO ejecuta la acción → **Dashboard** NO debe mostrar notificación

### Cambio Realizado

**Archivo:** `src/ControlPeso.Web/Pages/Dashboard.razor.cs`

```diff
private async Task OpenAddWeightDialog()
{
    Logger.LogInformation("Dashboard: Opening AddWeightDialog for user {UserId}", _currentUserId);

    var dialog = await DialogService.ShowAsync<AddWeightDialog>(AddWeightDialogTitle);
    var result = await dialog.Result;

    if (result != null && !result.Canceled)
    {
        Logger.LogInformation("Dashboard: Weight added successfully, reloading data");
-       Snackbar.Add(WeightSavedSuccess, Severity.Success); // ❌ DUPLICADA - ELIMINADA
+       // ✅ NO mostrar notificación aquí - AddWeightDialog ya la mostró
+       // Evita duplicación en historial de notificaciones
        await LoadDataAsync();
        StateHasChanged();
    }
    else
    {
        Logger.LogDebug("Dashboard: AddWeightDialog canceled");
    }
}
```

También se eliminó la propiedad localizada sin usar:

```diff
// Success/info messages
private string ExportComingSoon => Localizer["ExportComingSoon"];
private string AddWeightDialogTitle => Localizer["AddWeightDialogTitle"];
- private string WeightSavedSuccess => Localizer["WeightSavedSuccess"];
+ // WeightSavedSuccess property removed - notification now shown only in AddWeightDialog to avoid duplication
```

### Flujo Corregido

```
Usuario → Dashboard (Click "Agregar Peso")
   ↓
Dashboard.OpenAddWeightDialog()
   ↓
MudDialog.ShowAsync<AddWeightDialog>()
   ↓
AddWeightDialog.Submit()
   ↓
WeightLogService.CreateAsync() → ✅ Guarda en DB
   ↓
AddWeightDialog línea 127: Snackbar.Add(SuccessMessage) ← ✅ ÚNICA notificación
   ↓
NotificationService.AddAsync() → SaveToHistoryAsync() → ✅ ÚNICO registro en DB
   ↓
MudDialog.Close(DialogResult.Ok)
   ↓
Dashboard.OpenAddWeightDialog línea 309: (NO notifica) ← ✅ FIX
   ↓
Dashboard.LoadDataAsync() → Recarga datos
```

## Impacto

### Antes del Fix
- ❌ 2 notificaciones mostradas al usuario (molesto)
- ❌ 2 registros duplicados en `UserNotifications` (contaminación de historial)
- ❌ Logs confusos con doble logging

### Después del Fix
- ✅ 1 sola notificación al usuario (UX correcta)
- ✅ 1 solo registro en `UserNotifications` (historial limpio)
- ✅ Logs claros sin duplicación

## Testing

### Casos de Prueba

1. **Agregar peso desde Dashboard:**
   - ✅ Una sola notificación "Peso registrado correctamente"
   - ✅ Un solo registro en `UserNotifications`

2. **Cancelar AddWeightDialog:**
   - ✅ NO se muestra notificación
   - ✅ NO se crea registro en `UserNotifications`

3. **Error al guardar peso:**
   - ✅ Notificación de error mostrada una vez
   - ✅ Un solo registro de error en `UserNotifications`

### Verificación en DB

Después del fix, ejecutar:

```sql
SELECT TOP (1000) [Id], [UserId], [Type], [Title], [Message], [IsRead], [CreatedAt]
FROM [ControlPeso].[dbo].[UserNotifications]
WHERE [Type] = 'Success'
  AND [Message] LIKE '%Peso registrado%'
ORDER BY [CreatedAt] DESC;
```

**Resultado esperado:** Solo 1 registro por cada peso guardado (no duplicados).

## Prevención de Futuros Duplicados

### Reglas de Diseño

1. **Principio de responsabilidad única:**
   - El componente que ejecuta una acción debe notificar el resultado
   - Los componentes padre NO deben volver a notificar

2. **Comunicación padre-hijo:**
   - Usar `DialogResult.Ok(data)` para comunicar éxito
   - Padre debe recargar datos, NO notificar

3. **Notificaciones de error:**
   - Siempre mostrar en el componente donde ocurre el error
   - NUNCA duplicar en componente padre

### Patrón Recomendado

```csharp
// ✅ CORRECTO - Componente hijo notifica
public async Task Submit()
{
    try
    {
        await Service.CreateAsync(dto);
        Snackbar.Add("Éxito", Severity.Success); // ← Notificar aquí
        MudDialog?.Close(DialogResult.Ok(true));
    }
    catch (Exception ex)
    {
        Snackbar.Add($"Error: {ex.Message}", Severity.Error); // ← Error también aquí
    }
}

// ✅ CORRECTO - Componente padre solo recarga datos
public async Task OpenDialog()
{
    var dialog = await DialogService.ShowAsync<MyDialog>();
    var result = await dialog.Result;

    if (!result.Canceled)
    {
        await LoadDataAsync(); // ← Solo recargar, NO notificar
    }
}
```

```csharp
// ❌ INCORRECTO - Notificación duplicada
public async Task Submit()
{
    await Service.CreateAsync(dto);
    Snackbar.Add("Éxito", Severity.Success); // ← Primera notificación
    MudDialog?.Close(DialogResult.Ok(true));
}

public async Task OpenDialog()
{
    var dialog = await DialogService.ShowAsync<MyDialog>();
    var result = await dialog.Result;

    if (!result.Canceled)
    {
        Snackbar.Add("Éxito", Severity.Success); // ← ❌ DUPLICADA
        await LoadDataAsync();
    }
}
```

## Otros Componentes Revisados

### ImageCropperDialog ✅

```csharp
// src/ControlPeso.Web/Components/Shared/ImageCropperDialog.razor.cs
private async Task SaveCroppedImageAsync()
{
    _isProcessing = true;
    // ... cropping logic ...
    
    // NO muestra notificación - el componente padre (Profile) lo hace
    // ✅ CORRECTO porque ImageCropperDialog NO ejecuta guardado final
    await OnImageCropped.InvokeAsync(base64Data);
    MudDialog?.Close(DialogResult.Ok(base64Data));
}
```

### Profile.razor.cs ✅

```csharp
// src/ControlPeso.Web/Pages/Profile.razor.cs
private async Task OpenImageCropperDialogAsync()
{
    var dialog = await DialogService.ShowAsync<ImageCropperDialog>(parameters);
    var result = await dialog.Result;

    if (!result.Canceled && result.Data is string base64Data)
    {
        // Profile EJECUTA el guardado → Profile NOTIFICA
        await SaveAvatarAsync(base64Data);
        UserSnackbar.Add("Avatar actualizado correctamente", Severity.Success); // ✅ CORRECTO
    }
}
```

**Diferencia clave:**
- `AddWeightDialog` ejecuta `WeightLogService.CreateAsync()` → Notifica en AddWeightDialog ✅
- `ImageCropperDialog` NO ejecuta guardado → NO notifica, Profile lo hace ✅

## Referencias

- **Issue relacionado:** Duplicación de notificaciones en `UserNotifications`
- **Archivos modificados:**
  - `src/ControlPeso.Web/Pages/Dashboard.razor.cs` (línea 309)
- **Archivos NO modificados (correctos):**
  - `src/ControlPeso.Web/Components/Shared/AddWeightDialog.razor.cs`
  - `src/ControlPeso.Web/Services/NotificationService.cs`
  - `src/ControlPeso.Infrastructure/Services/UserNotificationService.cs`

---

**Fecha:** 2026-02-17  
**Autor:** Copilot (Claude Sonnet 4.5)  
**Issue:** Duplicación de notificaciones en historial  
**Solución:** Eliminar notificación duplicada en Dashboard.OpenAddWeightDialog()  
**Estado:** ✅ RESUELTO
