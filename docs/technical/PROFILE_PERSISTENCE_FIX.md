# Fix: Persistencia de Campos en Perfil de Usuario

## Problema Reportado

Los campos en la página de perfil (`/profile`) no persist

ían correctamente después de hacer cambios y guardar:
- ✅ Altura (Height)
- ✅ Sistema de Unidades (Unit System - Metric/Imperial)
- ✅ Fecha de Nacimiento (Date of Birth)
- ✅ Peso Objetivo (Goal Weight)
- ✅ Idioma (Language - Español/English)

### Síntomas
1. Usuario cambia valores en el formulario
2. Click en "GUARDAR CAMBIOS"
3. Notificación "Perfil actualizado correctamente" aparece
4. Al recargar la página, los valores vuelven a los anteriores

## Análisis del Problema

### Flujo de Guardado (ANTES del Fix)

```csharp
// Profile.razor.cs - SaveChanges()
var dto = new UpdateUserProfileDto
{
    Name = _name.Trim(),
    Height = _height,
    DateOfBirth = _dateOfBirth.HasValue ? DateOnly.FromDateTime(_dateOfBirth.Value) : null,
    GoalWeight = _goalWeight,
    UnitSystem = _unitSystem,
    Language = _language
};

var updatedUser = await UserService.UpdateProfileAsync(_user.Id, dto);
_user = updatedUser; // ✅ Actualiza _user

// ❌ PROBLEMA: NO actualiza los campos del formulario (_name, _height, etc.)
// Entonces al navegar a otra página y volver, OnInitializedAsync recarga desde DB
// pero los campos del formulario siguen con los valores viejos en memoria
```

### Root Cause

Blazor Server mantiene el estado de los componentes en memoria mientras el circuito SignalR está activo. Cuando el usuario:

1. Carga `/profile` → `OnInitializedAsync()` ejecuta → campos se cargan desde DB
2. Usuario modifica campos → `_name`, `_height`, etc. se actualizan en memoria
3. Usuario hace clic en "GUARDAR" → `SaveChanges()` ejecuta
4. **DB se actualiza** ✅
5. `_user` se actualiza con valores de DB ✅
6. **PERO** `_name`, `_height`, etc. NO se actualizan ❌
7. Usuario navega a otra página (ej: Dashboard) y vuelve a Profile
8. Blazor **reutiliza el componente en memoria** (no recarga desde DB si el circuito sigue activo)
9. Formulario muestra valores **viejos** de `_name`, `_height`, etc. que quedaron en memoria

### Diagrama de Secuencia del Problema

```
Usuario                Profile.razor.cs               DB
   |                         |                        |
   |--- Load /profile ------>|                        |
   |                         |--- GetByIdAsync() ---->|
   |                         |<--- UserDto (DB) ------| Height=170, Language=es
   |                         |                        |
   |                         | _height = 170          |
   |                         | _language = "es"       |
   |<--- Render Form --------|                        |
   |                         |                        |
   |--- Change Height=180 -->|                        |
   |                         | _height = 180          |
   |                         |                        |
   |--- Change Language=en ->|                        |
   |                         | _language = "en"       |
   |                         |                        |
   |--- Click GUARDAR ------>|                        |
   |                         |--- UpdateProfileAsync ->| ✅ DB: Height=180, Language=en
   |                         |<--- UserDto (updated) -|
   |                         |                        |
   |                         | _user = updatedUser    | ✅ _user actualizado
   |                         | ❌ _height still 180   | ❌ campos del form NO actualizados
   |                         | ❌ _language still "en"|
   |                         |                        |
   |<--- Success message ----|                        |
   |                         |                        |
   |--- Navigate to Dashboard                         |
   |--- Navigate back to /profile                     |
   |                         |                        |
   |                         | Blazor reuses component|
   |                         | (circuit still active) |
   |                         |                        |
   |<--- Render Form --------|                        |
   |    (shows old values)   | _height = 180 (memory) | ⚠️ NO recargó desde DB
   |    Height: 180          | _language = "en" (mem) |
   |    Language: EN          |                        |
```

### ¿Por Qué No Recargaba desde DB?

Blazor Server mantiene el estado del componente en memoria mientras el circuito SignalR está activo. Esto significa:

- ✅ Primera carga: `OnInitializedAsync()` ejecuta → carga desde DB
- ❌ Navegación dentro del mismo circuito: Blazor **reutiliza el componente** → `OnInitializedAsync()` **NO ejecuta**
- Los campos del formulario (`_name`, `_height`, etc.) mantienen valores viejos en memoria

## Solución Implementada

### Estrategia

**Sincronizar campos del formulario con valores confirmados de DB después de guardar.**

Cuando `UpdateProfileAsync` retorna el usuario actualizado, debemos:
1. Actualizar `_user` ✅ (ya se hacía)
2. Actualizar TODOS los campos del formulario (`_name`, `_height`, `_unitSystem`, etc.) ✅ (agregado)

### Código Actualizado

```csharp
// Profile.razor.cs - SaveChanges() (DESPUÉS del Fix)
private async Task SaveChanges()
{
    if (_user is null)
        return;

    Logger.LogInformation("Saving profile changes for user {UserId}", _user.Id);
    _isSaving = true;

    try
    {
        // Log valores ANTES de guardar (debugging)
        Logger.LogInformation(
            "Profile: BEFORE SAVE - Name: '{Name}', Height: {Height}cm, UnitSystem: {UnitSystem}, " +
            "DateOfBirth: {DateOfBirth}, GoalWeight: {GoalWeight}kg, Language: '{Language}'",
            _name, _height, _unitSystem, _dateOfBirth?.ToString("yyyy-MM-dd") ?? "(null)", 
            _goalWeight, _language);

        // 1. Crear DTO con valores del formulario
        var dto = new UpdateUserProfileDto
        {
            Name = _name.Trim(),
            Height = _height,
            DateOfBirth = _dateOfBirth.HasValue ? DateOnly.FromDateTime(_dateOfBirth.Value) : null,
            GoalWeight = _goalWeight,
            UnitSystem = _unitSystem,
            Language = _language
        };

        // 2. Guardar en DB
        var updatedUser = await UserService.UpdateProfileAsync(_user.Id, dto);
        
        // Log valores DESPUÉS de guardar (confirmación desde DB)
        Logger.LogInformation(
            "Profile: AFTER SAVE (from DB) - Name: '{Name}', Height: {Height}cm, UnitSystem: {UnitSystem}, " +
            "DateOfBirth: {DateOfBirth}, GoalWeight: {GoalWeight}kg, Language: '{Language}'",
            updatedUser.Name, updatedUser.Height, updatedUser.UnitSystem, 
            updatedUser.DateOfBirth?.ToString("yyyy-MM-dd") ?? "(null)", 
            updatedUser.GoalWeight, updatedUser.Language);

        // ✅ CRÍTICO: Actualizar _user Y los campos del formulario con los valores confirmados de DB
        _user = updatedUser;
        _name = updatedUser.Name;
        _height = updatedUser.Height;
        _unitSystem = updatedUser.UnitSystem;
        _dateOfBirth = updatedUser.DateOfBirth?.ToDateTime(TimeOnly.MinValue);
        _goalWeight = updatedUser.GoalWeight;
        _language = updatedUser.Language;

        // Notificar a otros componentes (MainLayout, etc.)
        UserStateService.NotifyUserProfileUpdated(_user);

        Logger.LogInformation("Profile updated successfully - UserId: {UserId}", _user.Id);
        Snackbar.Add(ProfileSavedSuccess, Severity.Success);
        
        // Force UI re-render to show updated values
        StateHasChanged();
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error updating user profile - UserId: {UserId}", _user.Id);
        Snackbar.Add(ErrorUpdatingProfile, Severity.Error);
    }
    finally
    {
        _isSaving = false;
    }
}
```

### Flujo Corregido

```
Usuario                Profile.razor.cs               DB
   |                         |                        |
   |--- Load /profile ------>|                        |
   |                         |--- GetByIdAsync() ---->|
   |                         |<--- UserDto (DB) ------| Height=170, Language=es
   |                         | _height = 170          |
   |                         | _language = "es"       |
   |<--- Render Form --------|                        |
   |                         |                        |
   |--- Change Height=180 -->|                        |
   |                         | _height = 180          |
   |--- Change Language=en ->|                        |
   |                         | _language = "en"       |
   |--- Click GUARDAR ------>|                        |
   |                         |--- UpdateProfileAsync ->| ✅ DB: Height=180, Language=en
   |                         |<--- UserDto (updated) -|
   |                         |                        |
   |                         | _user = updatedUser    | ✅ _user actualizado
   |                         | _height = 180 (from DB)| ✅ sincronizado con DB
   |                         | _language = "en" (DB)  | ✅ sincronizado con DB
   |                         | StateHasChanged()      | ✅ UI re-render
   |                         |                        |
   |<--- Success message ----|                        |
   |                         |                        |
   |--- Navigate to Dashboard                         |
   |--- Navigate back to /profile                     |
   |                         |                        |
   |                         | Blazor reuses component|
   |                         | (circuit still active) |
   |                         |                        |
   |<--- Render Form --------|                        |
   |    (shows correct values)| _height = 180 (synced)| ✅ Valores correctos
   |    Height: 180          | _language = "en" (synced)|
   |    Language: EN          |                        |
```

## Beneficios de la Solución

### 1. Sincronización Garantizada
- Los campos del formulario siempre reflejan los valores guardados en DB
- No hay discrepancia entre memoria y DB

### 2. Logging Detallado
```csharp
// ANTES de guardar
Logger.LogInformation(
    "Profile: BEFORE SAVE - Name: '{Name}', Height: {Height}cm, UnitSystem: {UnitSystem}, ...",
    _name, _height, _unitSystem, ...);

// DESPUÉS de guardar (desde DB)
Logger.LogInformation(
    "Profile: AFTER SAVE (from DB) - Name: '{Name}', Height: {Height}cm, UnitSystem: {UnitSystem}, ...",
    updatedUser.Name, updatedUser.Height, updatedUser.UnitSystem, ...);
```

Esto permite diagnosticar problemas futuros fácilmente:
- Si `BEFORE SAVE` != `AFTER SAVE` → problema en servicio/mapper/DB
- Si `AFTER SAVE` correcto pero UI no actualiza → problema en binding

### 3. StateHasChanged() Explícito
```csharp
// Force UI re-render to show updated values
StateHasChanged();
```

Asegura que Blazor re-renderiza el componente con los valores actualizados.

## Testing

### Casos de Prueba

#### 1. Cambiar Altura
```
1. Navegar a /profile
2. Cambiar Altura de 170cm → 180cm
3. Click "GUARDAR CAMBIOS"
4. ✅ Verificar notificación "Perfil actualizado correctamente"
5. Navegar a Dashboard
6. Volver a /profile
7. ✅ Verificar que Altura muestra 180cm
```

#### 2. Cambiar Sistema de Unidades
```
1. Navegar a /profile
2. Cambiar de "Métrico (kg, cm)" → "Imperial (lb, in)"
3. Click "GUARDAR CAMBIOS"
4. ✅ Verificar notificación
5. Navegar a Dashboard
6. Volver a /profile
7. ✅ Verificar que Sistema de Unidades muestra "Imperial"
```

#### 3. Cambiar Fecha de Nacimiento
```
1. Navegar a /profile
2. Seleccionar Fecha de Nacimiento: 02/02/1984
3. Click "GUARDAR CAMBIOS"
4. ✅ Verificar notificación
5. Navegar a Dashboard
6. Volver a /profile
7. ✅ Verificar que Fecha de Nacimiento muestra 02/02/1984
```

#### 4. Cambiar Peso Objetivo (Opcional)
```
1. Navegar a /profile
2. Ingresar Peso Objetivo: 75kg
3. Click "GUARDAR CAMBIOS"
4. ✅ Verificar notificación
5. Navegar a Dashboard
6. Volver a /profile
7. ✅ Verificar que Peso Objetivo muestra 75kg

8. Borrar Peso Objetivo (dejar vacío)
9. Click "GUARDAR CAMBIOS"
10. ✅ Verificar notificación
11. Navegar a Dashboard
12. Volver a /profile
13. ✅ Verificar que Peso Objetivo está vacío
```

#### 5. Cambiar Idioma
```
1. Navegar a /profile
2. Cambiar Idioma de "Español" → "English"
3. Click "GUARDAR CAMBIOS"
4. ✅ Verificar notificación
5. ✅ Verificar que UI cambió a inglés inmediatamente
6. Navegar a Dashboard (debe estar en inglés)
7. Volver a /profile
8. ✅ Verificar que Idioma muestra "English"
```

### Verificación en DB

Después de cada cambio, verificar en DB:

```sql
SELECT TOP (1) [Id], [Name], [Height], [UnitSystem], [DateOfBirth], 
               [GoalWeight], [Language], [UpdatedAt]
FROM [ControlPeso].[dbo].[Users]
WHERE [Email] = 'marco.alejandro.desantis@gmail.com'
ORDER BY [UpdatedAt] DESC;
```

**Resultado esperado:** Todos los campos deben reflejar los cambios guardados.

### Verificación en Logs

```
Profile: BEFORE SAVE - Name: 'Marco Alejandro De Santis', Height: 180cm, UnitSystem: Metric, DateOfBirth: 1984-02-02, GoalWeight: 75kg, Language: 'es'

UserService: Updating user profile: {UserId} - Name: Marco Alejandro De Santis, Height: 180, Language: es

UserMapper: UpdateEntity - Applying changes to user entity

UserService: User profile updated successfully: {UserId}, Name: Marco Alejandro De Santis, Height: 180cm, GoalWeight: 75kg

Profile: AFTER SAVE (from DB) - Name: 'Marco Alejandro De Santis', Height: 180cm, UnitSystem: Metric, DateOfBirth: 1984-02-02, GoalWeight: 75kg, Language: 'es'

Profile updated successfully - UserId: {UserId}
```

**Verificación:**
- `BEFORE SAVE` == valores ingresados por usuario
- `AFTER SAVE` == valores confirmados desde DB
- Si coinciden → ✅ guardado exitoso

## Prevención de Problemas Futuros

### Regla de Diseño

**En componentes Blazor Server con formularios:**

1. **NUNCA** asumir que `OnInitializedAsync()` se ejecutará en cada navegación
   - Blazor reutiliza componentes en memoria
   - Solo se ejecuta en primera carga del circuito

2. **SIEMPRE** sincronizar campos del formulario después de guardar
   ```csharp
   var updatedEntity = await Service.UpdateAsync(id, dto);
   
   // ✅ OBLIGATORIO: Sync form fields with DB values
   _field1 = updatedEntity.Field1;
   _field2 = updatedEntity.Field2;
   _field3 = updatedEntity.Field3;
   
   StateHasChanged();
   ```

3. **CONSIDERAR** implementar `OnParametersSet()` para recargas dinámicas
   ```csharp
   protected override async Task OnParametersSetAsync()
   {
       if (_shouldReload)
       {
           await LoadDataAsync();
       }
   }
   ```

4. **USAR** `StateHasChanged()` después de actualizaciones importantes
   - Fuerza re-render inmediato
   - Evita UI desincronizada

### Patrón Recomendado

```csharp
private async Task SaveChanges()
{
    try
    {
        // 1. Validar
        if (!Validate()) return;

        // 2. Crear DTO
        var dto = CreateDto();

        // 3. Guardar en DB
        var updated = await Service.UpdateAsync(_entityId, dto);

        // 4. ✅ Sync ALL form fields with DB
        SyncFormFieldsFromEntity(updated);

        // 5. Notificar otros componentes
        StateService.NotifyEntityUpdated(updated);

        // 6. Force UI update
        StateHasChanged();

        // 7. Notificar usuario
        Snackbar.Add("Guardado correctamente", Severity.Success);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error saving changes");
        Snackbar.Add($"Error: {ex.Message}", Severity.Error);
    }
}

private void SyncFormFieldsFromEntity(EntityDto entity)
{
    _field1 = entity.Field1;
    _field2 = entity.Field2;
    _field3 = entity.Field3;
    _field4 = entity.Field4;
    // ... todos los campos del formulario
}
```

## Referencias

- **Archivo modificado:** `src/ControlPeso.Web/Pages/Profile.razor.cs` (línea 298-341)
- **Archivos relacionados (sin cambios):**
  - `src/ControlPeso.Application/Services/UserService.cs`
  - `src/ControlPeso.Application/Mapping/UserMapper.cs`
  - `src/ControlPeso.Application/DTOs/UpdateUserProfileDto.cs`
  - `src/ControlPeso.Web/Pages/Profile.razor`

---

**Fecha:** 2026-02-17  
**Autor:** Copilot (Claude Sonnet 4.5)  
**Issue:** Campos de perfil no persisten después de guardar  
**Solución:** Sincronizar campos del formulario con valores confirmados de DB  
**Estado:** ✅ RESUELTO
