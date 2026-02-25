# Docker Configuration - Control Peso Thiscloud

## Dockerfile para Blazor Server (.NET 10)

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["ControlPeso.sln", "./"]
COPY ["src/ControlPeso.Domain/ControlPeso.Domain.csproj", "src/ControlPeso.Domain/"]
COPY ["src/ControlPeso.Application/ControlPeso.Application.csproj", "src/ControlPeso.Application/"]
COPY ["src/ControlPeso.Infrastructure/ControlPeso.Infrastructure.csproj", "src/ControlPeso.Infrastructure/"]
COPY ["src/ControlPeso.Web/ControlPeso.Web.csproj", "src/ControlPeso.Web/"]

# Restore dependencies
RUN dotnet restore "ControlPeso.sln"

# Copy source code
COPY . .

# Build and publish
WORKDIR "/src/src/ControlPeso.Web"
RUN dotnet publish "ControlPeso.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create directories for persistent volumes
RUN mkdir -p /app/wwwroot/uploads/avatars && \
    mkdir -p /app/data && \
    mkdir -p /app/logs

# Copy published app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Run app
ENTRYPOINT ["dotnet", "ControlPeso.Web.dll"]
```

## docker-compose.yml

```yaml
version: '3.8'

services:
  controlpeso-web:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: controlpeso-web
    ports:
      - "8080:8080"
    environment:
      # Connection Strings
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/controlpeso.db
      
      # Google OAuth
      - Authentication__Google__ClientId=${GOOGLE_CLIENT_ID}
      - Authentication__Google__ClientSecret=${GOOGLE_CLIENT_SECRET}
      
      # Photo Storage
      - PhotoStorage__WebRootPath=/app/wwwroot
      - PhotoStorage__AvatarsFolder=uploads/avatars
      
      # Logging
      - ThisCloud__Loggings__IsEnabled=true
      - ThisCloud__Loggings__MinimumLevel=Information
      - ThisCloud__Loggings__File__Path=/app/logs/controlpeso-.ndjson
      - ThisCloud__Loggings__File__Enabled=true
      
      # ASP.NET Core
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    
    volumes:
      # Volumen persistente para fotos de usuario (CRÍTICO - NO perder al actualizar contenedor)
      - ./volumes/uploads:/app/wwwroot/uploads
      
      # Volumen persistente para base de datos SQLite
      - ./volumes/data:/app/data
      
      # Volumen persistente para logs
      - ./volumes/logs:/app/logs
    
    restart: unless-stopped
    
    networks:
      - controlpeso-network

networks:
  controlpeso-network:
    driver: bridge

volumes:
  uploads:
    driver: local
  data:
    driver: local
  logs:
    driver: local
```

## .dockerignore

```
# Build artifacts
**/bin/
**/obj/
**/out/

# User-specific files
*.user
*.suo
.vs/
.vscode/

# Database & uploads (managed by volumes)
*.db
*.db-shm
*.db-wal
wwwroot/uploads/

# Logs
logs/
*.log

# Environment files
.env
appsettings.Development.json
appsettings.*.json

# Git
.git/
.gitignore
.gitattributes

# Documentation
README.md
docs/

# Tests
**/*Tests/
**/*.Tests/

# Node modules
node_modules/
```

## Comandos Docker

### Construir imagen
```bash
docker build -t controlpeso:latest .
```

### Ejecutar con docker-compose
```bash
# Iniciar servicios
docker-compose up -d

# Ver logs en tiempo real
docker-compose logs -f controlpeso-web

# Detener servicios
docker-compose down

# Detener Y eliminar volúmenes (⚠️ PELIGRO - borra fotos y BD)
docker-compose down -v
```

### Ejecutar con docker run (sin compose)
```bash
docker run -d \
  --name controlpeso-web \
  -p 8080:8080 \
  -v $(pwd)/volumes/uploads:/app/wwwroot/uploads \
  -v $(pwd)/volumes/data:/app/data \
  -v $(pwd)/volumes/logs:/app/logs \
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/controlpeso.db" \
  -e Authentication__Google__ClientId="YOUR_CLIENT_ID" \
  -e Authentication__Google__ClientSecret="YOUR_CLIENT_SECRET" \
  controlpeso:latest
```

## Estructura de Volúmenes Persistentes

```
volumes/
├── uploads/                  # CRÍTICO: Fotos de usuarios
│   └── avatars/
│       ├── 663252fd-..._1740359478_foto_perfil.jpg
│       ├── 89ab12cd-..._1740360123_mi_imagen.png
│       └── ...
├── data/                     # CRÍTICO: Base de datos SQLite
│   ├── controlpeso.db
│   ├── controlpeso.db-shm   # Shared memory (temp)
│   └── controlpeso.db-wal   # Write-Ahead Log (temp)
└── logs/                     # Logs persistentes
    ├── controlpeso-20260220.ndjson
    ├── controlpeso-20260221.ndjson
    └── ...
```

## Variables de Entorno (.env file)

```env
# Google OAuth (obtener en https://console.cloud.google.com)
GOOGLE_CLIENT_ID=your_client_id_here.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your_secret_here

# LinkedIn OAuth (opcional)
LINKEDIN_CLIENT_ID=your_linkedin_client_id
LINKEDIN_CLIENT_SECRET=your_linkedin_secret

# Google Analytics (opcional)
GOOGLE_ANALYTICS_MEASUREMENT_ID=G-XXXXXXXXXX

# Telegram notifications (opcional)
TELEGRAM_ENABLED=false
TELEGRAM_BOT_TOKEN=
TELEGRAM_CHAT_ID=
```

## Backup & Restore

### Backup automático (cron job recomendado)
```bash
#!/bin/bash
# backup.sh - Ejecutar diariamente con cron

DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="backups/$DATE"

mkdir -p "$BACKUP_DIR"

# Backup base de datos
cp volumes/data/controlpeso.db "$BACKUP_DIR/"

# Backup fotos (tar con compresión)
tar -czf "$BACKUP_DIR/uploads.tar.gz" volumes/uploads/

# Backup logs (opcional)
tar -czf "$BACKUP_DIR/logs.tar.gz" volumes/logs/

echo "Backup completado: $BACKUP_DIR"
```

### Restore
```bash
#!/bin/bash
# restore.sh - Restaurar desde backup

BACKUP_DIR="backups/20260220_123456"  # Ajustar fecha

# Detener contenedor
docker-compose down

# Restaurar base de datos
cp "$BACKUP_DIR/controlpeso.db" volumes/data/

# Restaurar fotos
tar -xzf "$BACKUP_DIR/uploads.tar.gz"

# Reiniciar contenedor
docker-compose up -d

echo "Restore completado desde: $BACKUP_DIR"
```

## Actualización de la Aplicación (sin perder datos)

```bash
# 1. Hacer backup (por seguridad)
./backup.sh

# 2. Pull latest code
git pull origin main

# 3. Rebuild imagen
docker-compose build

# 4. Detener contenedor viejo (volúmenes NO se eliminan)
docker-compose down

# 5. Iniciar nuevo contenedor (usa los MISMOS volúmenes)
docker-compose up -d

# 6. Verificar logs
docker-compose logs -f controlpeso-web
```

## Migración SQLite → SQL Server (Futuro)

Cuando escale a SQL Server, solo cambiar:

```yaml
# docker-compose.yml
services:
  controlpeso-web:
    environment:
      # Cambiar connection string a SQL Server
      - ConnectionStrings__DefaultConnection=Server=sql-server;Database=ControlPeso;User Id=sa;Password=${SQL_PASSWORD};TrustServerCertificate=True
    depends_on:
      - sql-server
  
  sql-server:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=${SQL_PASSWORD}
    volumes:
      - sqldata:/var/opt/mssql
    ports:
      - "1433:1433"

volumes:
  sqldata:
```

Luego en código:
1. Cambiar `services.AddDbContext` de `UseSqlite` a `UseSqlServer`
2. Re-scaffold entidades (estructura idéntica, solo cambia provider)
3. Los volúmenes de `uploads/` siguen iguales (fotos permanecen en file system)

## Health Checks

```yaml
# Agregar a docker-compose.yml
services:
  controlpeso-web:
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

Agregar endpoint de health check en `Program.cs`:
```csharp
app.MapHealthChecks("/health");
```

## Monitoreo con Prometheus (Opcional)

```yaml
# docker-compose.yml
services:
  prometheus:
    image: prom/prometheus:latest
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus-data:/prometheus
    ports:
      - "9090:9090"
  
  grafana:
    image: grafana/grafana:latest
    volumes:
      - grafana-data:/var/lib/grafana
    ports:
      - "3000:3000"
    depends_on:
      - prometheus

volumes:
  prometheus-data:
  grafana-data:
```

## Seguridad en Producción

1. **Secrets Management**: Usar Docker Secrets o Azure Key Vault
2. **HTTPS**: Reverse proxy con Let's Encrypt (Nginx/Traefik)
3. **Firewall**: Exponer solo puertos necesarios
4. **User Permissions**: Contenedor NO debe correr como root
5. **Image Scanning**: `docker scan controlpeso:latest`

## Notas Importantes

- ✅ **Volúmenes son persistentes** entre actualizaciones de contenedor
- ✅ **Fotos NO se pierden** al hacer `docker-compose down` (solo con `-v` flag)
- ✅ **Base de datos SQLite persiste** en volumen `/app/data`
- ⚠️ **SQLite tiene límites** - migrar a SQL Server si escala (>100 usuarios concurrentes)
- 🔒 **Backup automático** es crítico para producción
- 🔐 **Secrets NUNCA en imagen** - usar variables de entorno o secrets management

## Referencias

- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [ASP.NET Core Docker Images](https://hub.docker.com/_/microsoft-dotnet-aspnet)
- [Docker Volumes](https://docs.docker.com/storage/volumes/)
- [Docker Compose Networking](https://docs.docker.com/compose/networking/)
