# 📸 Avatar Architecture - ControlPeso.Thiscloud

> **Versión**: 1.0 - Producción Estable  
> **Fecha**: 2026-02-28  
> **Estado**: ✅ FUNCIONANDO  
> **Autor**: Sistema documentado después de 3 días debugging

---

## 🎯 Propósito de Este Documento

Este documento existe porque pasamos **3 días debugging** un problema de sincronización de avatares. Este archivo es tu **copia de seguridad** si el bug vuelve a aparecer. Documenta:

1. La arquitectura correcta que funciona
2. El problema que tuvimos y por qué apareció
3. La solución definitiva implementada
4. Cómo debuggear si vuelve a fallar

---

## ✅ Arquitectura Correcta (FUNCIONANDO)

### Flujo Completo de Upload

```
USER (Profile.razor) 
  ↓ 
1. Click "Guardar" en ImageCropper
  ↓
2. Profile.HandleCroppedImageAsync()
   - Genera GUID único → 0cb4a1d8-13a0-4b02-b069-247e433be802.webp
   - Guarda archivo a wwwroot/uploads/avatars/{guid}.webp
   - Crea UpdateUserProfileDto con nueva AvatarUrl
  ↓
3. await UserService.UpdateProfileAsync(userId, dto)
   ⚡ CRITICAL: UserService.UpdateProfileAsync INVALIDA CACHE
   - DbContext update → SaveChangesAsync() → DB actualizada ✅
   - _cache.Remove(cacheKeyById) ✅
   - _cache.Remove(cacheKeyByEmail) ✅
  ↓
4. UserStateService.NotifyUserProfileUpdated(updatedUser)
   ⚡ Evento dispara → MainLayout.OnUserProfileUpdated
  ↓
5. MainLayout.OnUserProfileUpdated()
   - _currentUser = updatedUser (tiene nueva AvatarUrl)
   - _avatarVersion = DateTime.UtcNow.Ticks (cache bust browser)
   - StateHasChanged() → re-render
  ↓
6. MainLayout.GetAvatarUrl()
   - Retorna _currentUser.AvatarUrl + ?v={_avatarVersion}
   - Browser carga imagen nueva ✅
```

### Componentes Críticos

#### 1️⃣ **UserService.UpdateProfileAsync** (AUTHORITATIVE SOURCE)
📁 `src/ControlPeso.Application/Services/UserService.cs` (líneas 410-456)

```csharp
// CRITICAL: Invalida cache después del UPDATE
var affectedRows = await context.SaveChangesAsync(ct);

var updatedDto = UserMapper.ToDto(user);

// CRITICAL FIX: Invalidate cache after update
var cacheKeyById = $"{CacheKeyPrefixById}{id}";
var cacheKeyByEmail = $"{CacheKeyPrefixByEmail}{updatedDto.Email.ToLowerInvariant()}";

_cache.Remove(cacheKeyById);   // ← SIN ESTO, EL BUG VUELVE
_cache.Remove(cacheKeyByEmail); // ← SIN ESTO, EL BUG VUELVE
```

**⚠️ ADVERTENCIA**: Si remueves estas líneas de invalidación de cache, el bug de avatar viejo volverá porque:
- GetByIdAsync() lee del cache (TTL 5 minutos)
- Cache tiene datos VIEJOS después del UPDATE
- MainLayout renderiza avatar VIEJO del cache

#### 2️⃣ **MainLayout.LoadUserDataAsync** (SIMPLIFIED)
📁 `src/ControlPeso.Web/Components/Layout/MainLayout.razor.cs` (líneas 88-118)

```csharp
// SIMPLIFIED: Cache invalidation in UserService ensures fresh data
await LoadCurrentUserAsync(); // ← Lee DB con cache INVALIDADO

// Update cache busting version for avatar
_avatarVersion = DateTime.UtcNow.Ticks; // ← Browser reload forzado
```

**Antes (INCORRECTO)**: Intentaba sincronizar localStorage con DB manualmente → race conditions, desincronización.  
**Ahora (CORRECTO)**: Confía en que UserService invalida cache → siempre datos frescos.

#### 3️⃣ **GetAvatarUrl** (RENDERING)
📁 `src/ControlPeso.Web/Components/Layout/MainLayout.razor.cs` (líneas 420-446)

```csharp
private string GetAvatarUrl()
{
    if (_currentUser is null || string.IsNullOrWhiteSpace(_currentUser.AvatarUrl))
        return string.Empty;

    // Add cache busting to force browser reload
    var separator = _currentUser.AvatarUrl.Contains('?') ? '&' : '?';
    return $"{_currentUser.AvatarUrl}{separator}v={_avatarVersion}";
}
```

**IMPORTANTE**: `_avatarVersion` se actualiza en `OnUserProfileUpdated()` para forzar al **browser** a recargar la imagen (browser cache bypass).

---

## 🔴 El Problema que Tuvimos (3 DÍAS DEBUGGING)

### Síntomas

1. Usuario sube avatar → archivo se guarda correctamente en `/uploads/avatars/`
2. DB se actualiza correctamente (verificado en SQL Server)
3. **PERO**: MainLayout seguía mostrando avatar VIEJO (o inicial "M")
4. F5 refresh NO solucionaba el problema

### Root Cause (Encontrado)

**IMemoryCache** en `UserService` con TTL de **5 minutos** (línea 25):

```csharp
private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
```

**Flujo del Bug**:

```
1. Usuario sube avatar → UpdateProfileAsync actualiza DB ✅
2. MainLayout llama LoadCurrentUserAsync
   ↓
3. LoadCurrentUserAsync llama UserService.GetByIdAsync
   ↓
4. GetByIdAsync lee del CACHE (líneas 48-56) ← ❌ AQUÍ EL BUG
   if (_cache.TryGetValue(cacheKey, out UserDto? cachedUser))
   {
       return cachedUser; // ← DATOS VIEJOS POR 5 MINUTOS
   }
   ↓
5. MainLayout renderiza avatar VIEJO del cache
```

### Intentos Fallidos (Lo que NO Funcionó)

1. **localStorage caching**: Agregamos localStorage para "instant render" → Complejidad innecesaria, race conditions
2. **Sincronización manual**: LoadUserDataAsync intentaba sincronizar localStorage con DB → Lógica frágil
3. **Multiple re-renders**: StateHasChanged() llamado múltiples veces → Performance hit sin solución

### La Solución Correcta (SIMPLE)

**Invalidar cache en UpdateProfileAsync** (2 líneas de código):

```csharp
_cache.Remove(cacheKeyById);
_cache.Remove(cacheKeyByEmail);
```

**Por qué funciona**:
- Cuando Profile actualiza avatar → cache se invalida INMEDIATAMENTE
- Siguiente GetByIdAsync() → cache miss → lee DB con datos frescos ✅
- MainLayout recibe usuario actualizado → renderiza avatar nuevo ✅

---

## 🛠️ Debugging Si Vuelve a Fallar

### 1. Verificar que Cache se Invalida

Buscar en logs de Serilog:

```
✅ CORRECTO:
"User profile updated successfully: ... - Cache invalidated ✅"

❌ INCORRECTO (bug volvió):
"User profile updated successfully: ..." (SIN "Cache invalidated")
```

Si el log NO muestra "Cache invalidated", alguien removió las líneas de invalidación.

### 2. Verificar DB Directamente

```sql
-- Query SQL Server directamente
SELECT Id, Name, Email, AvatarUrl, UpdatedAt
FROM Users
WHERE Id = 'TU-USER-GUID-AQUI'
ORDER BY UpdatedAt DESC;
```

**Verifica**:
- ¿AvatarUrl tiene el GUID correcto del archivo subido?
- ¿UpdatedAt cambió después del upload?

Si DB tiene URL vieja → problema en UpdateProfileAsync (línea 433).  
Si DB tiene URL nueva pero render viejo → problema de cache (falta invalidación).

### 3. Verificar Archivo Físico

```powershell
# PowerShell - listar archivos en avatars
Get-ChildItem "src\ControlPeso.Web\wwwroot\uploads\avatars\" -Filter *.webp | 
  Sort-Object LastWriteTime -Descending | 
  Select-Object Name, LastWriteTime -First 5
```

**Verifica**:
- ¿El archivo con GUID de DB existe?
- ¿Es el último archivo modificado?

Si archivo NO existe → problema en Profile.HandleCroppedImageAsync (línea 787).

### 4. Verificar Logs de MainLayout

```
✅ CORRECTO (secuencia esperada):
1. "User profile updated - UserId: ..., AvatarUrl: /uploads/avatars/NEW-GUID.webp"
2. "GetAvatarUrl ✅ returning - URL: /uploads/avatars/NEW-GUID.webp?v=..."

❌ INCORRECTO:
1. "User profile updated - UserId: ..., AvatarUrl: /uploads/avatars/NEW-GUID.webp"
2. "GetAvatarUrl ✅ returning - URL: /uploads/avatars/OLD-GUID.webp?v=..."
   ↑ Significa que _currentUser tiene datos viejos del cache
```

### 5. Test Manual (Reproducción)

```
1. Login
2. Navigate to Profile
3. Upload avatar
4. Click "Guardar"
5. INMEDIATAMENTE buscar en logs:
   - "UpdateProfileAsync - DB UPDATE successful - Affected rows: 1"
   - "Cache invalidated ✅"
   - "User profile updated - ... AvatarUrl: /uploads/avatars/NEW-GUID.webp"
6. Verificar AppBar: ¿Avatar cambió?
7. F5 refresh: ¿Avatar persiste?
```

---

## 🔧 Rollback Procedure (Si Algo se Rompe)

### Escenario 1: Avatar No Actualiza Después de Upload

**Síntoma**: Usuario sube avatar, pero MainLayout muestra viejo o initial.

**Fix**:
1. Verificar `UserService.UpdateProfileAsync` (líneas 410-456)
2. Buscar las líneas de invalidación de cache:
   ```csharp
   _cache.Remove(cacheKeyById);
   _cache.Remove(cacheKeyByEmail);
   ```
3. Si NO existen → alguien las removió → agregarlas de nuevo DESPUÉS de `SaveChangesAsync()`

**Código correcto**:
```csharp
var affectedRows = await context.SaveChangesAsync(ct);
var updatedDto = UserMapper.ToDto(user);

// CRITICAL: Invalidate cache
var cacheKeyById = $"{CacheKeyPrefixById}{id}";
var cacheKeyByEmail = $"{CacheKeyPrefixByEmail}{updatedDto.Email.ToLowerInvariant()}";
_cache.Remove(cacheKeyById);
_cache.Remove(cacheKeyByEmail);
```

### Escenario 2: Complejidad de localStorage Vuelve a Aparecer

**Síntoma**: Código tiene `GetAvatarFromLocalStorageAsync`, `SaveAvatarToLocalStorageAsync`, `ClearUserDataFromLocalStorageAsync`.

**Fix**:
1. **ELIMINAR** esos métodos de `MainLayout.razor.cs`
2. **ELIMINAR** inyección de `IStorageService` en MainLayout
3. **SIMPLIFICAR** `LoadUserDataAsync()` para que solo llame `LoadCurrentUserAsync()`
4. Confiar en la invalidación de cache en UserService

**Razón**: localStorage para avatar es **innecesario** si cache funciona correctamente. Agrega complejidad, race conditions y bugs.

### Escenario 3: Cache Nunca se Invalida

**Síntoma**: Logs muestran "User retrieved from cache" después de UPDATE.

**Fix**:
1. Verificar que `UpdateProfileAsync` retorna `updatedDto` DESPUÉS de invalidar cache
2. Verificar que `GetByIdAsync` NO tiene lógica que impida cache miss
3. **NUCLEAR OPTION**: Reducir `CacheDuration` a 1 minuto en `UserService` (línea 25)

```csharp
// ANTES (5 minutos):
private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

// DESPUÉS (1 minuto - más seguro):
private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);
```

---

## 📊 Métricas de Éxito

### ✅ Sistema Funcionando Correctamente

- Usuario sube avatar → AppBar actualiza **INMEDIATAMENTE** (sin F5)
- F5 refresh → avatar persiste correctamente
- Logs muestran "Cache invalidated ✅" después de UPDATE
- DB query directa muestra AvatarUrl correcta
- Archivo físico existe en `/uploads/avatars/`
- NO hay excepciones en logs relacionadas con avatar
- NO hay localStorage logic en MainLayout

### ❌ Sistema con Problemas

- Avatar NO actualiza después de upload (muestra viejo o initial)
- F5 necesario para ver cambios (síntoma de cache stale)
- Logs NO muestran "Cache invalidated"
- DB tiene URL correcta pero render muestra vieja (cache no invalidado)
- Archivo existe pero no se muestra (browser cache + server cache doble bug)
- Excepciones `InvalidOperationException` relacionadas con localStorage (complejidad innecesaria)

---

## 🎓 Lecciones Aprendidas

1. **Cache invalidation is hard**: El 90% del bug era NO invalidar cache después de UPDATE
2. **Simplicidad > Complejidad**: localStorage era innecesario si cache funciona
3. **Debug from DB up**: Siempre verificar DB primero, luego cache, luego render
4. **Logs are critical**: Sin logs detallados, 3 días de debugging
5. **Document for future self**: Este archivo existe porque lo necesitamos

---

## 📞 Contacto y Soporte

Si este documento te salvó de 3 días de debugging, compraste una cerveza al equipo.  
Si el bug volvió y este documento NO ayudó, actualiza este archivo con la nueva solución.

**Fecha de última actualización**: 2026-02-28  
**Versión de código estable**: Git commit `[AGREGAR COMMIT HASH DESPUÉS DE MERGE]`  
**Estado**: ✅ Producción - Funcionando

---

## 🔗 Referencias

- **UserService**: `src/ControlPeso.Application/Services/UserService.cs`
- **MainLayout**: `src/ControlPeso.Web/Components/Layout/MainLayout.razor.cs`
- **Profile**: `src/ControlPeso.Web/Pages/Profile.razor.cs`
- **UserMapper**: `src/ControlPeso.Application/Mapping/UserMapper.cs`

---

**END OF DOCUMENT** - Si llegaste aquí, ya sabés todo lo que necesitás saber sobre avatares.
