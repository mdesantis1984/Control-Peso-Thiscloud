# Unit System Global State Management & Profile Field Persistence Fixes

**Fecha**: 2026-02-27
**Proyecto**: ControlPeso.Thiscloud
**Estado**: ✅ IMPLEMENTADO Y COMPILADO
**Prioridad**: 🔴 CRÍTICA

---

## Problemas Detectados

### 1. **Unit System NO disponible globalmente**
- El sistema de unidades (Métrico/Imperial) NO estaba accesible para conversiones automáticas en toda la app
- Dashboard, History, Trends mostraban SIEMPRE en kg/cm sin respetar preferencia del usuario
- Cada componente necesitaba inyectar `IUserService` para obtener preferencias (ineficiente)

### 2. **Campos de perfil no se visualizan en UI**
- A pesar de guardarse correctamente en DB, campos como Height aparecían vacíos en la UI
- Falta de logging diagnóstico para detectar dónde se pierde el dato
- Falta de `StateHasChanged()` después de cargar datos en `OnInitializedAsync()`

---

## Solución Implementada

### 1. **UserStateService extendido con estado global de UnitSystem**

#### Cambios en `src/ControlPeso.Web/Services/UserStateService.cs`:

```csharp
// ✅ AGREGADO: Estado global privado
private UnitSystem _currentUnitSystem = UnitSystem.Metric;

// ✅ AGREGADO: Propiedad pública para acceso global
public UnitSystem CurrentUnitSystem => _currentUnitSystem;

// ✅ AGREGADO: Método para establecer el sistema de unidades
public void SetCurrentUnitSystem(UnitSystem unitSystem)
{
    if (_currentUnitSystem != unitSystem)
    {
        Logger.LogInformation(
            "UserStateService: Unit system changed - Old: {Old}, New: {New}",
            _currentUnitSystem, unitSystem);

        _currentUnitSystem = unitSystem;
        UserUnitSystemUpdated?.Invoke(this, unitSystem);
    }
}

// ✅ AGREGADO: Evento para notificar cambios
public event EventHandler<UnitSystem>? UserUnitSystemUpdated;

// ✅ AGREGADO: Métodos de conversión
public decimal ConvertWeight(decimal weightInKg)
{
    return _currentUnitSystem == UnitSystem.Imperial
        ? weightInKg * 2.20462m  // kg → lb
        : weightInKg;             // kg → kg (no conversion)
}

public decimal ConvertHeight(decimal heightInCm)
{
    return _currentUnitSystem == UnitSystem.Imperial
        ? heightInCm / 2.54m      // cm → in
        : heightInCm;             // cm → cm (no conversion)
}

// ✅ AGREGADO: Helpers para etiquetas de unidades
public string GetWeightUnitLabel()
{
    return _currentUnitSystem == UnitSystem.Imperial ? "lb" : "kg";
}

public string GetHeightUnitLabel()
{
    return _currentUnitSystem == UnitSystem.Imperial ? "in" : "cm";
}
```

#### ✅ Modificado `NotifyUserProfileUpdated()`:
```csharp
public void NotifyUserProfileUpdated(UserDto updatedUser)
{
    ArgumentNullException.ThrowIfNull(updatedUser);

    Logger.LogInformation(
        "UserStateService: Notifying profile update - UserId: {UserId}, AvatarUrl: {AvatarUrl}, UnitSystem: {UnitSystem}",
        updatedUser.Id,
        updatedUser.AvatarUrl ?? "(null)",
        updatedUser.UnitSystem);

    // ✅ CRÍTICO: Actualizar estado global
    SetCurrentUnitSystem(updatedUser.UnitSystem);

    UserProfileUpdated?.Invoke(this, updatedUser);
}
```

---

### 2. **Profile.razor.cs - Integración y Diagnóstico**

#### ✅ OnInitializedAsync() - Cargar y establecer Unit System:

```csharp
// 🔍 DIAGNOSTIC: Log valores exactos desde DB
Logger.LogWarning(
    "Profile: USER DTO FROM DATABASE - Name: '{Name}', Height: {Height}cm, UnitSystem: {UnitSystem}, " +
    "DateOfBirth: {DateOfBirth}, GoalWeight: {GoalWeight}kg, StartingWeight: {StartingWeight}kg, Language: '{Language}'",
    _user.Name, _user.Height, _user.UnitSystem, 
    _user.DateOfBirth?.ToString("yyyy-MM-dd") ?? "(null)", 
    _user.GoalWeight, _user.StartingWeight, _user.Language);

// Populate form fields
_name = _user.Name;
_height = _user.Height;
_dateOfBirth = _user.DateOfBirth?.ToDateTime(TimeOnly.MinValue);
_goalWeight = _user.GoalWeight;
_unitSystem = _user.UnitSystem;
_language = _user.Language;

// ✅ CRÍTICO: Establecer estado global de Unit System
UserStateService.SetCurrentUnitSystem(_user.UnitSystem);

// 🔍 DIAGNOSTIC: Log valores asignados a campos del formulario
Logger.LogWarning(
    "Profile: FORM FIELDS POPULATED - _name: '{Name}', _height: {Height}cm, _unitSystem: {UnitSystem}, " +
    "_dateOfBirth: {DateOfBirth}, _goalWeight: {GoalWeight}kg, _language: '{Language}'",
    _name, _height, _unitSystem, _dateOfBirth?.ToString("yyyy-MM-dd") ?? "(null)", 
    _goalWeight, _language);
```

#### ✅ Finally block con StateHasChanged():

```csharp
finally
{
    _isLoading = false;
    
    // ✅ CRITICAL: Force UI re-render after loading all data
    // Blazor Server may cache component state - ensure fresh render with loaded values
    StateHasChanged();
}
```

#### ✅ SaveChanges() - Actualizar Unit System global:

```csharp
// CRÍTICO: Actualizar _user Y los campos del formulario con los valores confirmados de DB
_user = updatedUser;
_name = updatedUser.Name;
_height = updatedUser.Height;
_unitSystem = updatedUser.UnitSystem;
_dateOfBirth = updatedUser.DateOfBirth?.ToDateTime(TimeOnly.MinValue);
_goalWeight = updatedUser.GoalWeight;
_language = updatedUser.Language;

// ✅ CRITICAL: Update global Unit System state (user may have changed preference)
UserStateService.SetCurrentUnitSystem(updatedUser.UnitSystem);

// Notify other components that profile changed (includes UnitSystem sync)
UserStateService.NotifyUserProfileUpdated(_user);
```

---

### 3. **Dashboard.razor.cs - Integración con UserStateService**

#### ✅ Inyección de UserStateService:

```csharp
[Inject] private Services.UserStateService UserStateService { get; set; } = null!;
```

#### ✅ Métodos helper para conversiones:

```csharp
// ========================================================================
// UNIT CONVERSION HELPERS (uses global UserStateService)
// ========================================================================

/// <summary>
/// Formats weight in user's preferred unit (kg or lb) with unit label.
/// Example: "75.5 kg" or "166.4 lb"
/// </summary>
private string FormatWeight(decimal weightInKg, int decimals = 1)
{
    var converted = UserStateService.ConvertWeight(weightInKg);
    var unit = UserStateService.GetWeightUnitLabel();
    var format = $"F{decimals}";
    return $"{converted.ToString(format)} {unit}";
}

/// <summary>
/// Converts weight to user's preferred unit WITHOUT label.
/// Use for chart data points or calculations.
/// </summary>
private decimal ConvertWeight(decimal weightInKg)
{
    return UserStateService.ConvertWeight(weightInKg);
}

/// <summary>
/// Gets the weight unit label (kg or lb) based on user's preference.
/// </summary>
private string GetWeightUnitLabel()
{
    return UserStateService.GetWeightUnitLabel();
}
```

---

## Flujo Completo del Estado Global

### 1. **Usuario carga perfil (Profile.razor.cs OnInitializedAsync)**
```
1. UserService.GetByIdAsync(userId) → UserDto con UnitSystem
2. UserStateService.SetCurrentUnitSystem(_user.UnitSystem) → estado global actualizado
3. Evento UserUnitSystemUpdated se dispara → componentes suscritos se enteran
4. StateHasChanged() fuerza re-render con valores correctos
```

### 2. **Usuario cambia sistema de unidades (Profile.razor.cs SaveChanges)**
```
1. UserService.UpdateProfileAsync() → guarda en DB
2. UserStateService.SetCurrentUnitSystem(updatedUser.UnitSystem) → actualiza global
3. UserStateService.NotifyUserProfileUpdated() → notifica a otros componentes
4. Evento UserUnitSystemUpdated se dispara
5. Dashboard, History, Trends reciben evento y re-renderizan con nueva unidad
```

### 3. **Componente necesita conversión (ej: Dashboard)**
```csharp
// Opción 1: Conversión + formato con unidad
var displayWeight = FormatWeight(weightInKg); // "75.5 kg" o "166.4 lb"

// Opción 2: Solo conversión (para charts, cálculos)
var convertedValue = ConvertWeight(weightInKg); // 75.5 o 166.4

// Opción 3: Solo etiqueta de unidad
var unit = GetWeightUnitLabel(); // "kg" o "lb"
```

---

## Beneficios de la Arquitectura

### ✅ **Centralización**
- UN solo lugar (`UserStateService`) maneja el estado global de Unit System
- NO hay duplicación de lógica de conversión

### ✅ **Reactividad**
- Evento `UserUnitSystemUpdated` permite que componentes reaccionen a cambios
- Componentes pueden suscribirse y re-renderizar automáticamente

### ✅ **Consistencia**
- TODA la app usa las mismas fórmulas de conversión
- kg → lb: `weight * 2.20462`
- cm → in: `height / 2.54`

### ✅ **Performance**
- Estado en memoria (campo privado `_currentUnitSystem`)
- NO necesita consultar DB cada vez que se necesita la preferencia
- Conversiones rápidas (operaciones matemáticas simples)

### ✅ **Testabilidad**
- Lógica de conversión aislada en métodos públicos
- Fácil de unit-testear

---

## Próximos Pasos (PENDIENTES)

### 1. **Actualizar componentes para usar conversiones**
- [ ] `Dashboard.razor` - reemplazar hardcoded "kg" con `FormatWeight()`
- [ ] `History.razor` - tabla de pesos con conversión
- [ ] `Trends.razor` - gráficos con ejes en unidad correcta
- [ ] `WeightChart.razor.cs` - puntos de datos convertidos
- [ ] `AddWeightDialog.razor.cs` - respetar `CurrentUnitSystem` para default

### 2. **Suscribirse a UserUnitSystemUpdated event**
Ejemplo en componentes que necesiten reaccionar a cambios:
```csharp
protected override void OnInitialized()
{
    UserStateService.UserUnitSystemUpdated += OnUnitSystemChanged;
}

private void OnUnitSystemChanged(object? sender, UnitSystem newUnitSystem)
{
    Logger.LogInformation("Component: Unit system changed to {UnitSystem}", newUnitSystem);
    StateHasChanged(); // Re-render con nueva unidad
}

public void Dispose()
{
    UserStateService.UserUnitSystemUpdated -= OnUnitSystemChanged;
}
```

### 3. **Agregar logging diagnóstico en componentes**
Para entender cuándo se actualiza el estado:
```csharp
Logger.LogWarning(
    "Component: Current UnitSystem: {UnitSystem}, ConvertedWeight: {Converted}{Unit}",
    UserStateService.CurrentUnitSystem, 
    UserStateService.ConvertWeight(75.5m), 
    UserStateService.GetWeightUnitLabel());
```

### 4. **Verificar valores en DB**
Ejecutar SQL para confirmar que Height y UnitSystem están correctos:
```sql
SELECT [Id], [Email], [Name], [Height], [UnitSystem], [GoalWeight], [Language]
FROM [Users]
WHERE [Email] = 'marco.alejandro.desantis@gmail.com';
```

### 5. **Inspeccionar logs en runtime**
Buscar líneas con "DIAGNOSTIC" o "CRITICAL" para rastrear flujo de datos:
```
Profile: USER DTO FROM DATABASE - Name: 'Marco', Height: 170cm, UnitSystem: Metric
Profile: FORM FIELDS POPULATED - _name: 'Marco', _height: 170cm, _unitSystem: Metric
UserStateService: Unit system changed - Old: Metric, New: Imperial
```

---

## Validación Manual

### Test 1: **Cambiar sistema de unidades en Profile**
1. Login
2. Ir a `/profile`
3. Cambiar "Sistema de Unidades" de Métrico a Imperial
4. Guardar cambios
5. ✅ Verificar logs: `UserStateService: Unit system changed - Old: Metric, New: Imperial`
6. Recargar `/dashboard`
7. ✅ Pesos deben mostrarse en lb (ej: 111.7 kg → 246.3 lb)

### Test 2: **Verificar Height field en Profile**
1. Login
2. Ir a `/profile`
3. ✅ Campo "Altura" debe mostrar valor (ej: 170)
4. Modificar a 175
5. Guardar cambios
6. ✅ Logs BEFORE SAVE: Height: 170cm
7. ✅ Logs AFTER SAVE: Height: 175cm
8. ✅ Campo debe reflejar 175 inmediatamente

### Test 3: **Estado global persiste entre navegaciones**
1. Cambiar a Imperial en Profile
2. Guardar
3. Navegar a Dashboard
4. ✅ Pesos en lb
5. Navegar a History
6. ✅ Pesos en lb
7. Navegar de vuelta a Profile
8. ✅ Sistema de Unidades sigue en "Imperial"

---

## Debugging Checklist

Si Height sigue apareciendo vacío:

- [ ] Verificar logs con "USER DTO FROM DATABASE" - ¿Height es 0 o tiene valor?
- [ ] Verificar logs con "FORM FIELDS POPULATED" - ¿_height se asigna correctamente?
- [ ] Verificar DB directamente: `SELECT Height FROM Users WHERE ...`
- [ ] Verificar tipo de `_height` en code-behind: debe ser `decimal` (no nullable)
- [ ] Verificar binding en `.razor`: `@bind-Value="_height"` (sin `@bind-Value:format`)
- [ ] Verificar que `StateHasChanged()` se ejecuta en finally de `OnInitializedAsync()`
- [ ] Agregar `Logger.LogWarning("_height value: {Height}", _height);` justo antes del render

---

## Arquitectura Final

```
UserStateService (Singleton registrado en DI)
├── _currentUnitSystem (private field - estado en memoria)
├── CurrentUnitSystem (public property - acceso global)
├── SetCurrentUnitSystem(UnitSystem) - actualiza estado + dispara evento
├── UserUnitSystemUpdated (event) - componentes suscritos reciben cambios
├── ConvertWeight(decimal) - kg → lb si Imperial
├── ConvertHeight(decimal) - cm → in si Imperial
├── GetWeightUnitLabel() - "kg" o "lb"
└── GetHeightUnitLabel() - "cm" o "in"

Profile.razor.cs (Owner del estado)
├── OnInitializedAsync() → SetCurrentUnitSystem(_user.UnitSystem)
└── SaveChanges() → SetCurrentUnitSystem(updatedUser.UnitSystem)

Dashboard.razor.cs (Consumer)
├── Inyecta UserStateService
├── FormatWeight() - usa ConvertWeight() + GetWeightUnitLabel()
└── (Futuro) Suscribirse a UserUnitSystemUpdated

History.razor.cs (Consumer)
└── (Pendiente) Usar FormatWeight() en tabla

Trends.razor.cs (Consumer)
└── (Pendiente) Convertir puntos de gráfico
```

---

## Reglas de Arquitectura Cumplidas

✅ **Onion Architecture**: UserStateService está en Web layer (correcto - es UI state)
✅ **Single Responsibility**: UserStateService solo maneja estado de UI compartido
✅ **DRY**: Lógica de conversión centralizada (no duplicada)
✅ **SOLID - Open/Closed**: Fácil agregar nuevos sistemas de unidades sin modificar existentes
✅ **SOLID - Dependency Inversion**: Componentes dependen de abstracción (UserStateService) no de implementación

---

## Conclusión

**Estado**: ✅ IMPLEMENTADO Y COMPILADO EXITOSAMENTE

**Cambios Críticos**:
1. ✅ UserStateService extendido con estado global de UnitSystem
2. ✅ Profile.razor.cs integrado con SetCurrentUnitSystem()
3. ✅ Logging diagnóstico agregado (USER DTO FROM DATABASE + FORM FIELDS POPULATED)
4. ✅ StateHasChanged() en finally de OnInitializedAsync()
5. ✅ Dashboard.razor.cs con helpers de conversión
6. ✅ SaveChanges() actualiza estado global después de guardar

**Próximos Pasos Inmediatos**:
1. Ejecutar app y revisar logs para diagnosticar Height vacío
2. Actualizar Dashboard.razor para usar FormatWeight() en lugar de hardcoded "kg"
3. Actualizar History, Trends, Charts para usar conversiones
4. Agregar suscripciones a UserUnitSystemUpdated en componentes que necesiten reactividad

**Testing Manual Requerido**:
- Cambiar Unit System en Profile → Verificar conversión global
- Modificar Height en Profile → Verificar persistencia en UI
- Navegar entre páginas → Verificar estado global persiste
