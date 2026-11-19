# Arquitectura de avatares

Flujo real de subida, almacenamiento y refresco del avatar de usuario. Todo el procesamiento de imagen ocurre en el servidor: lo que envía el navegador se trata como entrada no confiable.

## Flujo de subida

```
Navegador                    Servidor (Blazor Server)
---------                    ------------------------
1. Elige JPG/PNG/WebP (<= 5 MB)
2. ImageCropperDialog (Cropper.js)
   recorta a 256x256 -> data URL
3. Un solo mensaje SignalR  -> hub (MaximumReceiveMessageSize = 2 MB)
                              4. Profile.HandleCroppedImageAsync
                              5. AvatarUploadService.SaveAsync
                              6. UserService.UpdateProfileAsync
                              7. AvatarUploadService.TryDeleteLocalAvatar(anterior)
                              8. UserStateService.NotifyUserProfileUpdated -> MainLayout
```

### 5. `AvatarUploadService.SaveAsync`

`src/ControlPeso.Web/Services/AvatarUploadService.cs`. En este orden:

1. Quita el prefijo `data:...;base64,` si existe.
2. Rechaza el contenido vacío (`Empty`).
3. Comprueba la longitud del texto base64 **antes de decodificar** (`MaxEncodedLength`, equivalente a 1 MB decodificado) → `TooLarge`.
4. Decodifica; base64 inválido → `InvalidEncoding`. El resultado decodificado debe ser ≤ 1 MB (`MaxImageBytes`).
5. `ImageProcessingService` (SkiaSharp) detecta el formato por **contenido**, no por extensión ni `Content-Type`: JPEG, PNG, GIF, BMP o WebP. Exige mínimo 32 px por lado y máximo 25 megapíxeles, aplica la orientación EXIF, recorta al centro y re-codifica a **WebP con pérdida, 256x256, calidad 85**. Los bytes originales nunca se guardan. Si no es una imagen válida → `InvalidImage`.
6. Escribe `wwwroot/uploads/avatars/{guid}.webp` con un nombre generado por el servidor y devuelve la URL `/uploads/avatars/{guid}.webp`.

Los rechazos lanzan `AvatarRejectedException` (con `AvatarRejectionReason`) sin escribir nada en disco. `Profile` muestra un aviso genérico (tamaño excedido o formato inválido); el detalle técnico queda solo en el log.

### 6-7. Perfil y limpieza del avatar anterior

- `Profile.HandleCroppedImageAsync` llama a `UserService.UpdateProfileAsync` con la nueva `AvatarUrl`. Si esa llamada falla, el archivo nuevo se elimina para no dejar huérfanos.
- Solo después de actualizar el perfil se llama a `TryDeleteLocalAvatar(avatarAnterior)`. Solo borra URLs que cumplan `^/uploads/avatars/[A-Za-z0-9_\-.]+\.webp\z` **y** cuya ruta resuelta sea un archivo directo de la carpeta de avatares (sin recorridos de directorio). Las URLs externas (por ejemplo la foto de Google) y cualquier otra ruta se ignoran.

## Persistencia

En Docker, `/app/wwwroot/uploads` es un volumen (`controlpeso-uploads` en `docker-compose.production.yml`), por lo que los avatares sobreviven a los redespliegues. La carpeta debe ser escribible por el usuario `app` del contenedor (no root).

## Refresco en la interfaz (caché)

- `UserService.GetByIdAsync` y `GetByEmailAsync` cachean el `UserDto` en `IMemoryCache` durante 5 minutos (`CacheDuration`).
- `UserService.UpdateProfileAsync` invalida ambas claves (por id y por email) justo después de `SaveChangesAsync`. Sin esa invalidación, `MainLayout` leería el avatar viejo desde la caché hasta cinco minutos.
- `Profile` llama a `UserStateService.NotifyUserProfileUpdated(usuario)`; `MainLayout.OnUserProfileUpdated` guarda el usuario recibido, actualiza `_avatarVersion` y re-renderiza.
- `MainLayout.GetAvatarUrl()` devuelve `AvatarUrl` con `?v={_avatarVersion}` (o `&v=` si ya hay query) para que el navegador no use su caché de la imagen anterior.
- `MainLayout` no guarda el avatar en `localStorage`: confía en la invalidación de caché del servicio.

## Si el avatar no se actualiza

1. Revisar en los logs de Serilog (`/app/logs`) la línea `User profile updated successfully ... Cache invalidated`. Si falta, alguien quitó la invalidación en `UpdateProfileAsync`.
2. Consultar la columna `AvatarUrl` de la tabla `Users` en PostgreSQL: debe apuntar al `{guid}.webp` nuevo.
3. Comprobar que el archivo existe en el volumen (`/app/wwwroot/uploads/avatars/`).

## Referencias

- `src/ControlPeso.Web/Services/AvatarUploadService.cs`: validación, re-codificación y borrado seguro.
- `src/ControlPeso.Infrastructure/Services/ImageProcessingService.cs`: procesamiento con SkiaSharp.
- `src/ControlPeso.Web/Pages/Profile.razor.cs`: `HandleCroppedImageAsync`.
- `src/ControlPeso.Web/Components/Shared/ImageCropperDialog.razor.cs`: selección y recorte en el navegador.
- `src/ControlPeso.Application/Services/UserService.cs`: caché e invalidación.
- `src/ControlPeso.Web/Components/Layout/MainLayout.razor.cs`: refresco del avatar.
