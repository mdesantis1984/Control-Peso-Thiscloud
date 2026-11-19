# Database - Control Peso Thiscloud

> Production and local development use **PostgreSQL 15** through Npgsql. There is no SQL Server and no SQLite at runtime (SQLite only exists inside some test projects).

## Overview

| Item | Value |
|------|-------|
| Engine | PostgreSQL 15 (`postgres:15` in Docker Compose) |
| Provider | `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.0 on Entity Framework Core 10 |
| Schema source of truth | EF Core migrations in `src/ControlPeso.Infrastructure/Migrations` (initial migration `20261118214026_InitialPostgreSql`) |
| Generated script | [`docs/schema/postgresql_schema_v1.sql`](schema/postgresql_schema_v1.sql) (idempotent) |
| Applied by | [`scripts/migrate-postgres.sh`](../scripts/migrate-postgres.sh); **the application never migrates at startup** |
| Connection string | `ConnectionStrings:DefaultConnection` (environment variable `ConnectionStrings__DefaultConnection`) |
| DbContext | `ControlPesoDbContext` (Infrastructure), registered with `AddDbContextFactory` and `EnableRetryOnFailure()` |

Connection string format (never commit a real password):

```
Host=<host>;Port=5432;Database=controlpeso;Username=<role>;Password=<secret>
```

In production the value comes from `CONTROLPESO_DB_CONNECTION` in `docker-compose.production.yml`.

## Tables

All ids are `uuid` (default `gen_random_uuid()`); all timestamps are `timestamp without time zone` holding UTC (default `now() at time zone 'utc'`). Enums are stored as `integer`.

### Users

| Column | Type | Notes |
|--------|------|-------|
| `Id` | uuid | PK |
| `GoogleId`, `LinkedInId` | text, nullable | OAuth subject ids (LinkedIn login is disabled in the UI) |
| `Name` | text | |
| `Email` | **citext**, unique | Case-insensitive; requires the `citext` extension |
| `Role` | integer | 0 = User, 1 = Administrator |
| `AvatarUrl` | text, nullable | |
| `MemberSince`, `CreatedAt`, `UpdatedAt` | timestamp | UTC |
| `Height` | numeric | cm, default 170 |
| `UnitSystem` | integer | 0 = Metric, 1 = Imperial |
| `DateOfBirth` | date, nullable | |
| `Language` | text | `es` (default) or `en` |
| `Status` | integer | 0 = Active, 1 = Inactive, 2 = Pending |
| `GoalWeight`, `StartingWeight` | numeric, nullable | kg |

### WeightLogs

| Column | Type | Notes |
|--------|------|-------|
| `Id` | uuid | PK |
| `UserId` | uuid | FK → `Users(Id)` `ON DELETE CASCADE` |
| `Date` | date | |
| `Time` | time | |
| `Weight` | numeric | Always kg; pounds are display-only |
| `DisplayUnit` | integer | 0 = Kg, 1 = Lb |
| `Note` | text, nullable | |
| `Trend` | integer | 0 = Up, 1 = Down, 2 = Neutral (default) |
| `CreatedAt` | timestamp | UTC |

Unique index `IX_WeightLogs_UserId_Date_Time` on `(UserId, Date, Time)`.

### UserPreferences

`Id`, `UserId` (unique FK, cascade), `DarkMode` (default true), `NotificationsEnabled` (default true), `TimeZone` (default `America/Argentina/Buenos_Aires`), `UpdatedAt`.

### UserNotifications

`Id`, `UserId` (FK, cascade), `Type`, `Title`, `Message`, `IsRead`, `CreatedAt`.

### AuditLog

`Id`, `UserId` (FK, cascade), `Action`, `EntityType`, `EntityId`, `OldValue`, `NewValue` (JSON text), `CreatedAt`.

### Enums (`src/ControlPeso.Domain/Enums`)

```csharp
public enum UserRole { User = 0, Administrator = 1 }
public enum UserStatus { Active = 0, Inactive = 1, Pending = 2 }
public enum UnitSystem { Metric = 0, Imperial = 1 }
public enum WeightUnit { Kg = 0, Lb = 1 }
public enum WeightTrend { Up = 0, Down = 1, Neutral = 2 }
```

## Changing the schema

The schema is **code first with migrations**. Domain entities live in `src/ControlPeso.Domain/Entities` and the mapping in `ControlPesoDbContext`.

```bash
# Install dotnet-ef outside the repo (once)
dotnet tool install dotnet-ef --version 10.0.3 --tool-path "$TMPDIR/tools"

# 1. Change entities / ControlPesoDbContext, then create the migration.
#    The design-time factory (ControlPesoDbContextFactory) needs no database connection.
"$TMPDIR/tools/dotnet-ef" migrations add <Name> --project src/ControlPeso.Infrastructure

# 2. Regenerate the idempotent script kept in the repository
"$TMPDIR/tools/dotnet-ef" migrations script --idempotent \
  --project src/ControlPeso.Infrastructure -o docs/schema/postgresql_schema_v1.sql

# 3. Update DTOs, services and tests, then run the tests
dotnet test -c Release
```

Rules: never edit an applied migration; keep the application free of startup migrations; changes must be additive and safe to run on a database with data. Set `CONTROLPESO_DESIGN_CONNECTION` only if you need a non-default design-time connection string.

## Applying the schema

```bash
export PGHOST=<host> PGDATABASE=controlpeso PGUSER=<role> PGPASSWORD=<secret>
scripts/migrate-postgres.sh --dry-run   # print the idempotent SQL only
scripts/migrate-postgres.sh             # generate and apply it with psql
```

The role must be able to create tables and the `citext` extension. A manual alternative is running the checked-in script, which is also safe to repeat:

```bash
psql "host=<host> dbname=controlpeso user=<role>" -f docs/schema/postgresql_schema_v1.sql
```

See [DEPLOYMENT.md](DEPLOYMENT.md#database-migrations-postgresql) for where this fits in a release.

## Tests

`tests/ControlPeso.Infrastructure.Tests/Postgres` starts a `postgres:15` container with Testcontainers, applies every migration and runs the services against it. Without Docker those tests are reported as skipped. Other test projects use in-memory or SQLite test doubles.

## Backup and restore

Daily `pg_dump` backups run on the database host; see [BACKUP_SYSTEM.md](BACKUP_SYSTEM.md).

## Query guidelines

- Use `AsNoTracking()` for read-only queries and `.Select()` projections instead of loading whole entities.
- Services create short-lived contexts from `IDbContextFactory` (Blazor Server runs components concurrently; a shared scoped context is not safe).
- Avoid raw SQL (`FromSqlRaw`); input is validated with FluentValidation in the Application layer.
- Weight is stored in kg; convert to pounds only for display.

## References

- [Npgsql EF Core provider](https://www.npgsql.org/efcore/)
- [EF Core migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [PostgreSQL `citext`](https://www.postgresql.org/docs/15/citext.html)
