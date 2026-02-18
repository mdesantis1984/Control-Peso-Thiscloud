# Database Documentation - Control Peso Thiscloud

## Overview

- **Paradigm**: Database First (SQL schema is source of truth)
- **Dev/MVP**: SQLite (`controlpeso.db`)
- **Production**: SQL Server ready (swap connection string + provider)
- **ORM**: Entity Framework Core 9.0.1

## Schema (docs/schema/schema_v1.sql)

### Tables

#### Users
- **Primary Key**: `Id` TEXT (GUID as string)
- **Unique**: `GoogleId`, `Email`
- **Columns**: GoogleId, Name, Email, Role (0=User,1=Admin), AvatarUrl, MemberSince, Height, UnitSystem (0=Metric,1=Imperial), DateOfBirth, Language, Status (0=Active,1=Inactive,2=Pending), GoalWeight, StartingWeight, CreatedAt, UpdatedAt
- **Constraints**: Height CHECK (50-300cm), Role/UnitSystem/Status CHECK (valid enum values)

#### WeightLogs
- **Primary Key**: `Id` TEXT (GUID)
- **Foreign Key**: `UserId` → Users(Id) ON DELETE CASCADE
- **Columns**: UserId, Date (YYYY-MM-DD), Time (HH:MM), Weight REAL (kg), DisplayUnit (0=Kg,1=Lb), Note, Trend (0=Up,1=Down,2=Neutral), CreatedAt
- **Constraints**: Weight CHECK (20-500kg), Unique(UserId, Date, Time)

#### UserPreferences
- **Primary Key**: `Id` TEXT
- **Foreign Key**: `UserId` UNIQUE → Users(Id) ON DELETE CASCADE
- **Columns**: UserId, DarkMode INTEGER (bool), NotificationsEnabled INTEGER, TimeZone, UpdatedAt

#### AuditLog
- **Primary Key**: `Id` TEXT
- **Foreign Key**: `UserId` → Users(Id) ON DELETE CASCADE
- **Columns**: UserId, Action, EntityType, EntityId, OldValue (JSON), NewValue (JSON), CreatedAt

## Database First Workflow

### Modify Schema

```bash
# 1. Edit SQL file
vim docs/schema/schema_v1.sql

# 2. Apply to SQLite
sqlite3 controlpeso.db < docs/schema/schema_v1.sql

# 3. Scaffold entities
cd src/ControlPeso.Infrastructure
dotnet ef dbcontext scaffold \
  "Data Source=../../controlpeso.db" \
  Microsoft.EntityFrameworkCore.Sqlite \
  --context ControlPesoDbContext \
  --output-dir ../ControlPeso.Domain/Entities \
  --context-dir . \
  --force
```

**DO NOT** modify scaffolded entities manually. All changes in SQL → re-scaffold.

### Type Conversions

SQLite stores:
- **TEXT**: GUIDs, DateTimes, Dates, Times
- **INTEGER**: Enums, Booleans (0/1)
- **REAL**: Decimals, Doubles

Application Layer converts:
- `string` → `Guid` (Parse)
- `string` → `DateTime/DateOnly/TimeOnly` (Parse)
- `int` → `enum` (Cast)
- `long` → `bool` (Convert)

## Enums (Domain/Enums/)

```csharp
public enum UserRole { User = 0, Administrator = 1 }
public enum UserStatus { Active = 0, Inactive = 1, Pending = 2 }
public enum UnitSystem { Metric = 0, Imperial = 1 }
public enum WeightUnit { Kg = 0, Lb = 1 }
public enum WeightTrend { Up = 0, Down = 1, Neutral = 2 }
```

## Migration to SQL Server

```csharp
// appsettings.Production.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:controlpeso.database.windows.net,1433;Database=ControlPeso;User Id=admin;Password=***;Encrypt=true;"
  }
}

// Infrastructure/Extensions/ServiceCollectionExtensions.cs
services.AddDbContext<ControlPesoDbContext>(options =>
{
    if (environment.IsDevelopment())
        options.UseSqlite(connectionString);
    else
        options.UseSqlServer(connectionString);
});
```

**Steps**:
1. Create SQL Server database
2. Run `schema_v1.sql` (convert SQLite→SQL Server syntax)
3. Update connection string
4. Deploy
5. Re-scaffold entities if needed

## Backup & Restore

### SQLite Backup
```bash
# Backup
sqlite3 controlpeso.db ".backup backup_$(date +%Y%m%d).db"

# Restore
cp backup_20260218.db controlpeso.db
```

### SQL Server Backup
```sql
BACKUP DATABASE ControlPeso 
TO DISK = 'C:\Backups\ControlPeso_20260218.bak'
WITH COMPRESSION;
```

## Query Performance

- **Indexes**: Add to `schema_v1.sql` for frequently queried columns (UserId, Date, etc.)
- **AsNoTracking()**: Use for read-only queries
- **Projections**: `.Select()` only needed columns (avoid over-fetching)

## References

- [EF Core Database First](https://docs.microsoft.com/en-us/ef/core/managing-schemas/scaffolding)
- [SQLite Syntax](https://www.sqlite.org/lang.html)
- [SQL Server Migration](https://docs.microsoft.com/en-us/sql/database-engine/install-windows/upgrade-sql-server)
