# Sistema de unidades y estado global del usuario

> **Verificado contra el código**: 2026-11-19 (versión 1.3.0)

El peso y la altura se guardan **siempre** en kg y cm en la base de datos. El sistema de unidades del usuario (Métrico/Imperial) solo afecta a la **visualización** y a la entrada de datos en la UI.

## Componente central: `UserStateService`

`src/ControlPeso.Web/Services/UserStateService.cs`, registrado como `AddScoped` (un estado por circuito Blazor).

| Miembro | Función |
|---------|---------|
| `CurrentUnitSystem` | Sistema de unidades actual (`Metric` por defecto). |
| `SetCurrentUnitSystem(UnitSystem)` | Actualiza el valor y dispara `UserUnitSystemUpdated` si cambió. |
| `ConvertWeight(kg)` | kg → lb (`× 2.20462`) en Imperial; sin cambios en Métrico. |
| `ConvertHeight(cm)` | cm → in (`÷ 2.54`) en Imperial; sin cambios en Métrico. |
| `GetWeightUnitLabel()` / `GetHeightUnitLabel()` | `kg`/`lb` y `cm`/`in`. |
| `NotifyUserProfileUpdated(UserDto)` | Dispara `UserProfileUpdated` (nombre/avatar en el layout). |
| `NotifyUserThemeUpdated(bool)` | Dispara `UserThemeUpdated`. |

## Flujo

1. `MainLayout` carga el usuario al iniciar el circuito y llama a `SetCurrentUnitSystem(user.UnitSystem)`.
2. Cuando el usuario guarda el perfil, `Profile.razor.cs` llama a `SetCurrentUnitSystem(...)` y `NotifyUserProfileUpdated(...)`.
3. Los componentes que muestran pesos leen `ConvertWeight`/`GetWeightUnitLabel` y se suscriben a `UserUnitSystemUpdated` para re-renderizar: `Dashboard`, `Trends`, `WeightChart`. `History` convierte con las mismas funciones. `AddWeightDialog` y `EditWeightDialog` leen `CurrentUnitSystem` al abrirse para decidir si el campo es kg o lb.
4. Al guardar un registro, la UI convierte de vuelta a kg antes de llamar a los servicios de Application.

Los componentes que se suscriben a eventos deben desuscribirse en `Dispose` (lo hacen `Dashboard`, `Trends` y `WeightChart`).

## Reglas

- La conversión vive en `UserStateService`; no duplicar factores de conversión en páginas o componentes.
- Nunca persistir valores convertidos: la base de datos y los servicios de Application trabajan en kg/cm.
- Un nuevo componente que muestre pesos debe usar `ConvertWeight` + `GetWeightUnitLabel` y suscribirse a `UserUnitSystemUpdated`.

## Diagnóstico

Si un campo del perfil (por ejemplo la altura) aparece vacío en la UI aunque esté en la base de datos, comprobar en este orden:

1. El valor llega en el `UserDto` (logs de `UserService`/`Profile` en nivel `Debug`).
2. El componente invoca `StateHasChanged` después de cargar datos de forma asíncrona.
3. La conversión usa la unidad actual (`CurrentUnitSystem`), no un valor cacheado.

Relacionado: [ARCHITECTURE.md](../ARCHITECTURE.md), [BLAZOR_PRERENDERING_JS_INTEROP.md](./BLAZOR_PRERENDERING_JS_INTEROP.md).
