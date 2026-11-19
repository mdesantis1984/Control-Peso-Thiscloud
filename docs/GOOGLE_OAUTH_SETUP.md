# Configuración de Google OAuth 2.0

El inicio de sesión de Control Peso Thiscloud usa únicamente Google OAuth 2.0 (sin contraseñas locales). La aplicación solicita los scopes `profile` y `email`, y el proveedor solo se registra si `Authentication:Google:ClientId` tiene un valor.

## 1. Crear el cliente OAuth

1. Entrar en <https://console.cloud.google.com> y crear o elegir un proyecto.
2. En **APIs & Services → OAuth consent screen** configurar la pantalla de consentimiento:
   - Tipo **External** (o **Internal** con Google Workspace).
   - Nombre de la aplicación, correo de soporte y correo de contacto del desarrollador.
   - Scopes: `openid`, `email`, `profile`.
   - En modo *Testing*, agregar los correos de prueba como **Test users**.
3. En **APIs & Services → Credentials** elegir **+ CREATE CREDENTIALS → OAuth client ID**, tipo **Web application**.
4. Completar los orígenes y las URIs de redirección (ver la tabla siguiente) y crear el cliente.
5. Copiar el **Client ID** (termina en `.apps.googleusercontent.com`) y el **Client Secret**, y guardarlos en un gestor de contraseñas.

### URIs a registrar

La URI de redirección es siempre `<origen>/signin-google`. Debe coincidir exactamente (esquema, host, puerto y ruta).

| Entorno | Origen autorizado | URI de redirección autorizada |
|---------|-------------------|-------------------------------|
| Producción | `https://controlpeso.thiscloud.com.ar` | `https://controlpeso.thiscloud.com.ar/signin-google` |
| `dotnet run` (HTTPS) | `https://localhost:7065` | `https://localhost:7065/signin-google` |
| `dotnet run` (HTTP) | `http://localhost:5212` | `http://localhost:5212/signin-google` |
| Docker local | `http://localhost:8080` | `http://localhost:8080/signin-google` |

Los puertos de desarrollo salen de `src/ControlPeso.Web/Properties/launchSettings.json`; si se cambian, hay que actualizar Google Cloud Console. Se recomienda usar un cliente distinto para producción y para desarrollo.

## 2. Configurar la aplicación

Los valores reales nunca se versionan. Elegir una opción:

**`appsettings.Development.json`** (ignorado por Git; se crea con `pwsh scripts/setup-config.ps1`):

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "<client-id>.apps.googleusercontent.com",
      "ClientSecret": "<client-secret>"
    }
  }
}
```

**User Secrets**:

```bash
cd src/ControlPeso.Web
dotnet user-secrets set "Authentication:Google:ClientId" "<client-id>.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<client-secret>"
```

**Docker** (`docker-compose.override.yml`, ignorado por Git, o variables de entorno en producción):

```yaml
services:
  controlpeso-web:
    environment:
      - Authentication__Google__ClientId=<client-id>.apps.googleusercontent.com
      - Authentication__Google__ClientSecret=<client-secret>
```

Más detalle en [CONFIG_MANAGEMENT.md](CONFIG_MANAGEMENT.md) y [SECRETS_MANAGEMENT.md](SECRETS_MANAGEMENT.md).

## 3. Probar

1. Iniciar la aplicación (`dotnet run --project src/ControlPeso.Web` o `docker compose up -d`).
2. Abrir `/login` (por ejemplo `https://localhost:7065/login`) y usar el botón de Google.
3. Tras autorizar, Google redirige a `/signin-google` y la aplicación vuelve a la página de inicio. La cookie de sesión es persistente (30 días con renovación deslizante).

## Problemas frecuentes

### `redirect_uri_mismatch` o `invalid_client`

La URI que envía la aplicación no está registrada en el cliente OAuth.

1. Comparar la URI del mensaje de error con la tabla de arriba y agregarla exactamente en **Authorized redirect URIs**.
2. Esperar entre 1 y 5 minutos a que Google propague el cambio y reiniciar la aplicación.
3. Detrás de un proxy inverso, si el error muestra una URI `http://` en lugar de `https://`, la aplicación no está confiando en las cabeceras `X-Forwarded-*`: verificar `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` y la red del proxy en `ForwardedHeaders:KnownNetworks` ([DEPLOYMENT.md](DEPLOYMENT.md)).

### `Access blocked: This app's request is invalid`

La pantalla de consentimiento está incompleta. Completar los campos obligatorios y, en modo *Testing*, agregar el correo en **Test users**.

### `This app isn't verified`

Es normal con la pantalla de consentimiento en *Testing*. Usar **Advanced → Go to Control Peso (unsafe)** con un usuario de prueba; en producción hay que publicar la aplicación (*In production*) y, si Google lo exige, completar la verificación.

### El botón de Google no aparece o el login no inicia

- `ClientId` o `ClientSecret` vacíos o con espacios o comillas de más: el proveedor no se registra o falla el intercambio.
- En Docker: `docker exec controlpeso-web env | grep -c '^Authentication__Google__'` debe mostrar `2`.
- Revisar los logs (ver [soporte.md](soporte.md)).

### Un usuario inicia sesión pero queda sin acceso

Las cuentas nuevas se crean con el rol `User`. El primer administrador se asigna directamente en la base de datos (`"Users"."Role"` = 1, ver [DATABASE.md](DATABASE.md)); después se gestionan los roles desde el panel de administración.

## Producción

1. Publicar la pantalla de consentimiento (*In production*).
2. Registrar el origen y la URI de `https://controlpeso.thiscloud.com.ar` (tabla de arriba).
3. Entregar las credenciales por variables de entorno, nunca en archivos versionados.
4. Para cambiar las credenciales, seguir [CREDENTIAL_ROTATION_GUIDE.md](CREDENTIAL_ROTATION_GUIDE.md).

## Referencias

- [Google OAuth 2.0](https://developers.google.com/identity/protocols/oauth2)
- [Inicio de sesión con Google en ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins)
