# ============================================
# Stage 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["ControlPeso.slnx", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/ControlPeso.Domain/ControlPeso.Domain.csproj", "src/ControlPeso.Domain/"]
COPY ["src/ControlPeso.Application/ControlPeso.Application.csproj", "src/ControlPeso.Application/"]
COPY ["src/ControlPeso.Infrastructure/ControlPeso.Infrastructure.csproj", "src/ControlPeso.Infrastructure/"]
COPY ["src/ControlPeso.Shared.Resources/ControlPeso.Shared.Resources.csproj", "src/ControlPeso.Shared.Resources/"]
COPY ["src/ControlPeso.Web/ControlPeso.Web.csproj", "src/ControlPeso.Web/"]

# Restore dependencies
RUN dotnet restore "src/ControlPeso.Web/ControlPeso.Web.csproj"

# Copy all source code
COPY . .

# Build application
WORKDIR "/src/src/ControlPeso.Web"

# ============================================
# Stage 2: Publish (publish compiles the project; no separate build step)
# ============================================
FROM build AS publish
# The app version is derived from the git release tags by MinVer, which reads the .git folder
# of the build context (kept out of the final image: only /app/publish is copied below).
# When the context has no tag history (shallow clone, archive export, git worktree), pass the
# version explicitly: docker build --build-arg APP_VERSION=9.0.0 .
ARG APP_VERSION=""
RUN dotnet publish "ControlPeso.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false \
    ${APP_VERSION:+/p:MinVerVersionOverride=$APP_VERSION}

# ============================================
# Stage 3: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install curl for health checks (no recommended packages, apt lists cleaned)
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Remove static SEO files if they exist (served dynamically by Minimal APIs)
# and create the writable directories owned by the non-root runtime user
# (the "app" user, UID 1654, ships with the .NET 10 images)
RUN rm -f /app/wwwroot/robots.txt* /app/wwwroot/sitemap.xml* && \
    mkdir -p /app/data /app/logs /app/keys /app/wwwroot/uploads && \
    chown -R app:app /app/data /app/logs /app/keys /app/wwwroot/uploads

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Data Protection keys live under /app/keys (mount a volume there to persist them)
ENV DataProtection__Path=/app/keys

# Drop root: run as the built-in non-root user (UID 1654)
USER app

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD [ "$(curl -s -o /dev/null -w '%{http_code}' -H 'X-Forwarded-Proto: https' http://localhost:8080/health)" = "200" ] || exit 1

# Run application
ENTRYPOINT ["dotnet", "ControlPeso.Web.dll"]
