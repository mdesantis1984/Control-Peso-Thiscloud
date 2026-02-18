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
COPY ["src/ControlPeso.Web/ControlPeso.Web.csproj", "src/ControlPeso.Web/"]

# Restore dependencies
RUN dotnet restore "src/ControlPeso.Web/ControlPeso.Web.csproj"

# Copy all source code
COPY . .

# Build application
WORKDIR "/src/src/ControlPeso.Web"
RUN dotnet build "ControlPeso.Web.csproj" -c Release -o /app/build

# ============================================
# Stage 2: Publish
# ============================================
FROM build AS publish
RUN dotnet publish "ControlPeso.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ============================================
# Stage 3: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install SQLite (for Database First workflow)
RUN apt-get update && \
    apt-get install -y sqlite3 && \
    rm -rf /var/lib/apt/lists/*

# Create directories for database and logs
RUN mkdir -p /app/data /app/logs

# Copy published application
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Run application
ENTRYPOINT ["dotnet", "ControlPeso.Web.dll"]
