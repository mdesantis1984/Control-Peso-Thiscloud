#!/bin/bash
###############################################################################
# Script: migrate-sqlite-to-sqlserver.sh
# Purpose: Migrar datos de SQLite a SQL Server Express Linux
# Usage: ./migrate-sqlite-to-sqlserver.sh
###############################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
SQLITE_DB_PATH="${SQLITE_DB_PATH:-./controlpeso.db}"
SQLSERVER_CONNECTION="${SQLSERVER_CONNECTION:-Server=localhost,1433;Database=ControlPeso;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True}"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Control Peso - SQLite → SQL Server Migration${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Verificar que existe SQLite DB
if [ ! -f "$SQLITE_DB_PATH" ]; then
    echo -e "${RED}ERROR: SQLite database not found at: $SQLITE_DB_PATH${NC}"
    exit 1
fi

echo -e "${YELLOW}[1/5] Verificando conexión a SQL Server...${NC}"
# Test SQL Server connection usando dotnet script
cat > /tmp/test-connection.csx <<'EOF'
using Microsoft.Data.SqlClient;
var connectionString = Args[0];
try {
    using var connection = new SqlConnection(connectionString);
    connection.Open();
    Console.WriteLine("✓ Conexión exitosa a SQL Server");
    return 0;
} catch (Exception ex) {
    Console.WriteLine($"✗ Error: {ex.Message}");
    return 1;
}
EOF

if ! dotnet script /tmp/test-connection.csx "$SQLSERVER_CONNECTION" 2>/dev/null; then
    echo -e "${RED}ERROR: No se puede conectar a SQL Server${NC}"
    echo -e "${YELLOW}Verifica que el servidor esté corriendo y las credenciales sean correctas${NC}"
    exit 1
fi

echo ""
echo -e "${YELLOW}[2/5] Exportando datos de SQLite...${NC}"
# Exportar SQLite a SQL dump
sqlite3 "$SQLITE_DB_PATH" <<'SQL_EXPORT' > /tmp/sqlite-export.sql
.mode insert
-- Export Users
SELECT 'INSERT INTO Users VALUES' || '(' || 
  quote(Id) || ',' ||
  quote(GoogleId) || ',' ||
  quote(Name) || ',' ||
  quote(Email) || ',' ||
  Role || ',' ||
  COALESCE(quote(AvatarUrl), 'NULL') || ',' ||
  quote(MemberSince) || ',' ||
  Height || ',' ||
  UnitSystem || ',' ||
  COALESCE(quote(DateOfBirth), 'NULL') || ',' ||
  quote(Language) || ',' ||
  Status || ',' ||
  COALESCE(GoalWeight, 'NULL') || ',' ||
  COALESCE(StartingWeight, 'NULL') || ',' ||
  quote(CreatedAt) || ',' ||
  quote(UpdatedAt) || ');'
FROM Users;

-- Export WeightLogs
SELECT 'INSERT INTO WeightLogs VALUES' || '(' || 
  quote(Id) || ',' ||
  quote(UserId) || ',' ||
  quote(Date) || ',' ||
  quote(Time) || ',' ||
  Weight || ',' ||
  DisplayUnit || ',' ||
  COALESCE(quote(Note), 'NULL') || ',' ||
  Trend || ',' ||
  quote(CreatedAt) || ');'
FROM WeightLogs;

-- Export UserPreferences
SELECT 'INSERT INTO UserPreferences VALUES' || '(' || 
  quote(Id) || ',' ||
  quote(UserId) || ',' ||
  DarkMode || ',' ||
  NotificationsEnabled || ',' ||
  quote(TimeZone) || ',' ||
  quote(UpdatedAt) || ');'
FROM UserPreferences;

-- Export AuditLog
SELECT 'INSERT INTO AuditLog VALUES' || '(' || 
  quote(Id) || ',' ||
  quote(UserId) || ',' ||
  quote(Action) || ',' ||
  quote(EntityType) || ',' ||
  quote(EntityId) || ',' ||
  COALESCE(quote(OldValue), 'NULL') || ',' ||
  COALESCE(quote(NewValue), 'NULL') || ',' ||
  quote(CreatedAt) || ');'
FROM AuditLog;

-- Export UserNotifications
SELECT 'INSERT INTO UserNotifications VALUES' || '(' || 
  quote(Id) || ',' ||
  quote(UserId) || ',' ||
  quote(Type) || ',' ||
  quote(Title) || ',' ||
  quote(Message) || ',' ||
  IsRead || ',' ||
  quote(CreatedAt) || ',' ||
  COALESCE(quote(ReadAt), 'NULL') || ');'
FROM UserNotifications;
SQL_EXPORT

echo -e "${GREEN}✓ Datos exportados correctamente${NC}"

echo ""
echo -e "${YELLOW}[3/5] Creando schema en SQL Server...${NC}"
# Crear schema usando EF Core scaffolded SQL
cat > /tmp/create-schema.sql <<'SQL_SCHEMA'
-- ============================================
-- Control Peso - SQL Server Schema
-- Database First Migration from SQLite
-- ============================================

-- Create Tables
CREATE TABLE [Users] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [GoogleId] NVARCHAR(200) NOT NULL UNIQUE,
    [Name] NVARCHAR(200) NOT NULL,
    [Email] NVARCHAR(320) NOT NULL UNIQUE,
    [Role] INT NOT NULL DEFAULT 0 CHECK ([Role] IN (0, 1)),
    [AvatarUrl] NVARCHAR(2048) NULL,
    [MemberSince] NVARCHAR(100) NOT NULL,
    [Height] REAL NOT NULL DEFAULT 170.0 CHECK ([Height] >= 50 AND [Height] <= 300),
    [UnitSystem] INT NOT NULL DEFAULT 0 CHECK ([UnitSystem] IN (0, 1)),
    [DateOfBirth] NVARCHAR(100) NULL,
    [Language] NVARCHAR(10) NOT NULL DEFAULT 'es',
    [Status] INT NOT NULL DEFAULT 0 CHECK ([Status] IN (0, 1, 2)),
    [GoalWeight] REAL NULL CHECK ([GoalWeight] >= 20 AND [GoalWeight] <= 500),
    [StartingWeight] REAL NULL CHECK ([StartingWeight] >= 20 AND [StartingWeight] <= 500),
    [CreatedAt] NVARCHAR(100) NOT NULL,
    [UpdatedAt] NVARCHAR(100) NOT NULL
);

CREATE TABLE [WeightLogs] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [Date] NVARCHAR(100) NOT NULL,
    [Time] NVARCHAR(100) NOT NULL,
    [Weight] REAL NOT NULL CHECK ([Weight] >= 20 AND [Weight] <= 500),
    [DisplayUnit] INT NOT NULL DEFAULT 0 CHECK ([DisplayUnit] IN (0, 1)),
    [Note] NVARCHAR(500) NULL,
    [Trend] INT NOT NULL DEFAULT 2 CHECK ([Trend] IN (0, 1, 2)),
    [CreatedAt] NVARCHAR(100) NOT NULL,
    FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserPreferences] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL UNIQUE,
    [DarkMode] BIT NOT NULL DEFAULT 1,
    [NotificationsEnabled] BIT NOT NULL DEFAULT 1,
    [TimeZone] NVARCHAR(100) NOT NULL DEFAULT 'America/Argentina/Buenos_Aires',
    [UpdatedAt] NVARCHAR(100) NOT NULL,
    FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
);

CREATE TABLE [AuditLog] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [Action] NVARCHAR(100) NOT NULL,
    [EntityType] NVARCHAR(100) NOT NULL,
    [EntityId] NVARCHAR(450) NOT NULL,
    [OldValue] NVARCHAR(MAX) NULL,
    [NewValue] NVARCHAR(MAX) NULL,
    [CreatedAt] NVARCHAR(100) NOT NULL,
    FOREIGN KEY ([UserId]) REFERENCES [Users]([Id])
);

CREATE TABLE [UserNotifications] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [Type] NVARCHAR(50) NOT NULL,
    [Title] NVARCHAR(200) NOT NULL,
    [Message] NVARCHAR(500) NOT NULL,
    [IsRead] BIT NOT NULL DEFAULT 0,
    [CreatedAt] NVARCHAR(100) NOT NULL,
    [ReadAt] NVARCHAR(100) NULL,
    FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
);

-- Create Indexes (matching SQLite schema)
CREATE INDEX [IX_WeightLogs_UserId] ON [WeightLogs]([UserId]);
CREATE INDEX [IX_WeightLogs_Date] ON [WeightLogs]([Date] DESC);
CREATE INDEX [IX_WeightLogs_UserId_Date] ON [WeightLogs]([UserId], [Date] DESC);

CREATE INDEX [IX_UserPreferences_UserId] ON [UserPreferences]([UserId]);

CREATE INDEX [IX_AuditLog_UserId] ON [AuditLog]([UserId]);
CREATE INDEX [IX_AuditLog_Action] ON [AuditLog]([Action]);
CREATE INDEX [IX_AuditLog_EntityType_EntityId] ON [AuditLog]([EntityType], [EntityId]);
CREATE INDEX [IX_AuditLog_CreatedAt] ON [AuditLog]([CreatedAt] DESC);

CREATE INDEX [IX_UserNotifications_UserId] ON [UserNotifications]([UserId]);
CREATE INDEX [IX_UserNotifications_IsRead] ON [UserNotifications]([IsRead]);
CREATE INDEX [IX_UserNotifications_UserId_IsRead] ON [UserNotifications]([UserId], [IsRead]);
CREATE INDEX [IX_UserNotifications_CreatedAt] ON [UserNotifications]([CreatedAt] DESC);
SQL_SCHEMA

# Ejecutar schema con sqlcmd (instalado en contenedor SQL Server)
docker exec controlpeso-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -C \
  -Q "IF EXISTS (SELECT name FROM sys.databases WHERE name = 'ControlPeso') DROP DATABASE ControlPeso; CREATE DATABASE ControlPeso;"

docker exec controlpeso-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -C \
  -d ControlPeso -i /tmp/create-schema.sql

echo -e "${GREEN}✓ Schema creado correctamente${NC}"

echo ""
echo -e "${YELLOW}[4/5] Importando datos a SQL Server...${NC}"
# Importar datos
docker exec controlpeso-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -C \
  -d ControlPeso -i /tmp/sqlite-export.sql

echo -e "${GREEN}✓ Datos importados correctamente${NC}"

echo ""
echo -e "${YELLOW}[5/5] Verificando integridad de datos...${NC}"
# Verificar conteos de registros
cat > /tmp/verify-counts.sql <<'SQL_VERIFY'
SELECT 'Users' AS Table_Name, COUNT(*) AS Row_Count FROM Users
UNION ALL
SELECT 'WeightLogs', COUNT(*) FROM WeightLogs
UNION ALL
SELECT 'UserPreferences', COUNT(*) FROM UserPreferences
UNION ALL
SELECT 'AuditLog', COUNT(*) FROM AuditLog
UNION ALL
SELECT 'UserNotifications', COUNT(*) FROM UserNotifications;
SQL_VERIFY

echo -e "${GREEN}Conteos de registros en SQL Server:${NC}"
docker exec controlpeso-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -C \
  -d ControlPeso -i /tmp/verify-counts.sql

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}✓ Migración completada exitosamente${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}Próximos pasos:${NC}"
echo "1. Actualizar connection string en appsettings.Production.json"
echo "2. Re-scaffold DbContext para SQL Server:"
echo "   dotnet ef dbcontext scaffold \"$SQLSERVER_CONNECTION\" \\"
echo "     Microsoft.EntityFrameworkCore.SqlServer --force"
echo "3. Actualizar docker-compose.production.yml con SQL Server"
echo "4. Test de aplicación con nueva BD"
echo ""
