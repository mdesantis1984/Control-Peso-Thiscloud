# Guía de rotación de credenciales

Procedimiento para reemplazar las credenciales de Control Peso Thiscloud cuando se sospecha una exposición, cuando alguien con acceso deja el proyecto o de forma periódica (recomendado: cada 90 días). Nunca se escriben valores reales en la documentación ni en Git; ver [SECRETS_MANAGEMENT.md](SECRETS_MANAGEMENT.md).

## Credenciales y dónde viven

| Credencial | Dónde se usa | Cómo se rota |
|------------|--------------|--------------|
| Cliente OAuth de Google (`ClientId` + `ClientSecret`) | `docker-compose.override.yml` en el host de producción, User Secrets o `appsettings.Development.json` en desarrollo | Sección 1 |
| Contraseña del rol de PostgreSQL | `CONTROLPESO_DB_CONNECTION` (`.env` del host de producción), `POSTGRES_PASSWORD` en local | Sección 2 |
| Claves de Data Protection | Volumen `controlpeso-keys` (`/app/keys`) | Sección 3 |

## 1. Google OAuth

La forma más limpia es **crear un cliente nuevo**, cambiar la aplicación y recién entonces borrar el anterior (así no hay corte de servicio).

1. En <https://console.cloud.google.com/apis/credentials> crear un **OAuth client ID** de tipo *Web application*.
2. Orígenes JavaScript autorizados:
   - `https://controlpeso.thiscloud.com.ar`
   - `http://localhost:8080`, `https://localhost:7065`, `http://localhost:5212` (solo en el cliente de desarrollo)
3. URIs de redirección autorizadas: las mismas URLs con el sufijo `/signin-google`.
4. Guardar el nuevo `ClientId` y `ClientSecret` en el gestor de contraseñas.
5. Actualizar la aplicación:

   ```bash
   # Desarrollo con User Secrets
   cd src/ControlPeso.Web
   dotnet user-secrets set "Authentication:Google:ClientId" "<client-id>"
   dotnet user-secrets set "Authentication:Google:ClientSecret" "<client-secret>"

   # Producción: editar docker-compose.override.yml (Authentication__Google__*) y recrear el contenedor
   cd /opt/controlpeso
   docker compose -f docker-compose.production.yml -f docker-compose.override.yml up -d --force-recreate controlpeso-web
   ```

6. Probar el login (<https://controlpeso.thiscloud.com.ar/login> y el entorno local).
7. Eliminar el cliente anterior en Google Cloud Console. Desde ese momento sus secretos dejan de ser válidos.

## 2. Contraseña de PostgreSQL

1. En el servidor de base de datos, con un rol administrador:

   ```sql
   ALTER ROLE <rol_de_la_aplicacion> WITH PASSWORD '<nueva-contraseña>';
   ```

2. Actualizar `CONTROLPESO_DB_CONNECTION` en el `.env` del host de la aplicación y recrear el contenedor:

   ```bash
   docker compose -f docker-compose.production.yml -f docker-compose.override.yml up -d --force-recreate controlpeso-web
   curl https://controlpeso.thiscloud.com.ar/health/ready
   ```

3. Actualizar también el gestor de contraseñas y cualquier script de copias de seguridad que use la contraseña.
4. En local, la contraseña del volumen `controlpeso-pgdata` queda fijada al crearlo: para cambiarla usar el mismo `ALTER ROLE` dentro del contenedor, o recrear el volumen (se pierden los datos locales).

## 3. Claves de Data Protection (invalidar sesiones)

Solo si hay que forzar el cierre de todas las sesiones (por ejemplo, ante sospecha de robo de cookies):

```bash
docker compose -f docker-compose.production.yml -f docker-compose.override.yml stop controlpeso-web
docker run --rm -v controlpeso-keys:/keys alpine sh -c 'rm -f /keys/*'
docker compose -f docker-compose.production.yml -f docker-compose.override.yml up -d controlpeso-web
```

Todos los usuarios tendrán que iniciar sesión de nuevo. Los datos no se ven afectados.

## Checklist

- [ ] Nuevo cliente OAuth creado y guardado en el gestor de contraseñas
- [ ] Desarrollo y producción actualizados y login probado
- [ ] Cliente OAuth anterior eliminado
- [ ] Contraseña de PostgreSQL rotada (si corresponde) y `/health/ready` en 200
- [ ] Claves de Data Protection reiniciadas (si corresponde)
- [ ] Ningún secreto en `git diff`, en logs ni en capturas de pantalla

## Si un secreto llegó a Git

1. Rotar primero (secciones 1 a 3): se considera comprometido aunque se borre el commit.
2. Reescribir el historial (por ejemplo con `git filter-repo`) solo coordinando con el propietario del repositorio, y pedir que se vuelvan a clonar los repositorios locales.
3. Revisar el resultado con gitleaks (`gitleaks detect`); el CI ya lo ejecuta sobre el diff de cada pull request.

> Contexto: el repositorio público actual se recreó el 2026-11-19 con el historial reescrito para eliminar credenciales antiguas. El repositorio anterior pasó a privado con el nombre `Control-Peso-Thiscloud-old`; cualquier credencial que haya estado en él debe seguir considerándose comprometida y rotada.
