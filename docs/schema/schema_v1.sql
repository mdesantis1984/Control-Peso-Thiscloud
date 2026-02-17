-- =====================================================================
-- CONTRATO MAESTRO: ControlPeso.Thiscloud v1.0
-- Engine: SQLite 3.x (compatible con swap a SQL Server)
-- Mode: Database First → EF Core scaffold
-- Encoding: UTF-8
-- 
-- REGLA: Todo cambio de estructura EMPIEZA aquí.
--        Luego se aplica SQL y se re-scaffold.
--        NUNCA se modifican las entidades C# manualmente.
-- =====================================================================

PRAGMA journal_mode = WAL;
PRAGMA foreign_keys = ON;
PRAGMA encoding = 'UTF-8';

-- =====================================================================
-- TABLA: Users
-- Propósito: Almacena usuarios autenticados vía Google OAuth.
-- Gobierno de datos: SQL define todos los tipos, restricciones y defaults.
-- =====================================================================
CREATE TABLE IF NOT EXISTS Users (
    -- PK: GUID almacenado como TEXT (SQLite no tiene tipo GUID nativo)
    -- En SQL Server: uniqueidentifier NOT NULL DEFAULT NEWID()
    Id                TEXT        NOT NULL    PRIMARY KEY,

    -- Identificador único de Google OAuth (sub claim del token)
    -- Restricción UNIQUE garantiza un usuario por cuenta Google
    GoogleId          TEXT        NOT NULL    UNIQUE,

    -- Nombre completo del usuario (tomado de Google profile)
    -- CHECK: mínimo 1 carácter, máximo 200
    Name              TEXT        NOT NULL
        CHECK(length(Name) >= 1 AND length(Name) <= 200),

    -- Email del usuario (tomado de Google profile)
    -- CHECK: mínimo 5 caracteres (a@b.c), máximo 320 (RFC 5321)
    -- UNIQUE: un email = un usuario
    Email             TEXT        NOT NULL    UNIQUE
        CHECK(length(Email) >= 5 AND length(Email) <= 320),

    -- Rol del usuario
    -- 0 = User (default), 1 = Administrator
    -- CHECK: solo valores válidos del enum UserRole
    Role              INTEGER     NOT NULL    DEFAULT 0
        CHECK(Role IN (0, 1)),

    -- URL del avatar (tomada de Google profile, puede ser NULL si no tiene)
    -- CHECK: máximo 2048 caracteres (límite práctico de URL)
    AvatarUrl         TEXT        NULL
        CHECK(AvatarUrl IS NULL OR length(AvatarUrl) <= 2048),

    -- Fecha de registro del usuario (ISO 8601: YYYY-MM-DDTHH:MM:SSZ)
    -- Se establece al crear el usuario y NO se modifica
    MemberSince       TEXT        NOT NULL
        CHECK(length(MemberSince) >= 10 AND length(MemberSince) <= 30),

    -- Altura en centímetros (siempre cm, independiente de UnitSystem)
    -- CHECK: rango razonable para humanos adultos
    Height            REAL        NOT NULL    DEFAULT 170.0
        CHECK(Height >= 50.0 AND Height <= 300.0),

    -- Sistema de unidades preferido para display
    -- 0 = Metric (kg, cm), 1 = Imperial (lb, ft/in)
    UnitSystem        INTEGER     NOT NULL    DEFAULT 0
        CHECK(UnitSystem IN (0, 1)),

    -- Fecha de nacimiento (ISO 8601: YYYY-MM-DD, opcional)
    DateOfBirth       TEXT        NULL
        CHECK(DateOfBirth IS NULL OR length(DateOfBirth) = 10),

    -- Idioma preferido (código ISO 639-1)
    -- 'es' = Español, 'en' = English
    Language          TEXT        NOT NULL    DEFAULT 'es'
        CHECK(Language IN ('es', 'en')),

    -- Estado del usuario
    -- 0 = Active, 1 = Inactive, 2 = Pending
    Status            INTEGER     NOT NULL    DEFAULT 0
        CHECK(Status IN (0, 1, 2)),

    -- Peso objetivo en kilogramos (siempre kg, opcional)
    -- CHECK: rango razonable para humanos
    GoalWeight        REAL        NULL
        CHECK(GoalWeight IS NULL OR (GoalWeight >= 20.0 AND GoalWeight <= 500.0)),

    -- Peso inicial en kilogramos (siempre kg, opcional)
    -- Se establece al primer registro de peso
    StartingWeight    REAL        NULL
        CHECK(StartingWeight IS NULL OR (StartingWeight >= 20.0 AND StartingWeight <= 500.0)),

    -- Timestamps de auditoría (ISO 8601)
    CreatedAt         TEXT        NOT NULL
        CHECK(length(CreatedAt) >= 10 AND length(CreatedAt) <= 30),
    UpdatedAt         TEXT        NOT NULL
        CHECK(length(UpdatedAt) >= 10 AND length(UpdatedAt) <= 30)
);

-- Índices para Users
CREATE INDEX IF NOT EXISTS IX_Users_GoogleId   ON Users(GoogleId);
CREATE INDEX IF NOT EXISTS IX_Users_Email      ON Users(Email);
CREATE INDEX IF NOT EXISTS IX_Users_Status     ON Users(Status);
CREATE INDEX IF NOT EXISTS IX_Users_Role       ON Users(Role);
CREATE INDEX IF NOT EXISTS IX_Users_Language   ON Users(Language);

-- =====================================================================
-- TABLA: WeightLogs
-- Propósito: Registros de peso diarios/múltiples por usuario.
-- Regla de negocio: Weight siempre en kg. Conversión a lb en Application.
-- =====================================================================
CREATE TABLE IF NOT EXISTS WeightLogs (
    -- PK: GUID como TEXT
    Id                TEXT        NOT NULL    PRIMARY KEY,

    -- FK al usuario dueño del registro
    -- ON DELETE CASCADE: si se elimina el usuario, se eliminan sus registros
    UserId            TEXT        NOT NULL,

    -- Fecha del registro (ISO 8601 date: YYYY-MM-DD)
    -- CHECK: formato exacto de 10 caracteres
    Date              TEXT        NOT NULL
        CHECK(length(Date) = 10),

    -- Hora del registro (formato 24h: HH:MM)
    -- CHECK: formato exacto de 5 caracteres
    Time              TEXT        NOT NULL
        CHECK(length(Time) = 5),

    -- Peso en kilogramos (SIEMPRE kg, independiente del display)
    -- CHECK: rango razonable para humanos (incluye niños y extremos)
    -- REAL en SQLite → En SQL Server: DECIMAL(6,2) NOT NULL
    Weight            REAL        NOT NULL
        CHECK(Weight >= 20.0 AND Weight <= 500.0),

    -- Unidad de display al momento del registro
    -- 0 = Kg, 1 = Lb
    -- Nota: el valor Weight siempre está en kg; esto indica cómo lo vio el usuario
    DisplayUnit       INTEGER     NOT NULL    DEFAULT 0
        CHECK(DisplayUnit IN (0, 1)),

    -- Nota opcional del usuario sobre el registro
    -- CHECK: máximo 500 caracteres
    Note              TEXT        NULL
        CHECK(Note IS NULL OR length(Note) <= 500),

    -- Tendencia respecto al registro anterior
    -- 0 = Up (subió), 1 = Down (bajó), 2 = Neutral (igual o primer registro)
    -- Se calcula automáticamente en Application al crear el registro
    Trend             INTEGER     NOT NULL    DEFAULT 2
        CHECK(Trend IN (0, 1, 2)),

    -- Timestamp de creación (ISO 8601)
    CreatedAt         TEXT        NOT NULL
        CHECK(length(CreatedAt) >= 10 AND length(CreatedAt) <= 30),

    -- FK constraint
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Índices para WeightLogs
-- Índice compuesto UserId+Date DESC: consulta principal de historial por usuario
CREATE INDEX IF NOT EXISTS IX_WeightLogs_UserId          ON WeightLogs(UserId);
CREATE INDEX IF NOT EXISTS IX_WeightLogs_UserId_Date     ON WeightLogs(UserId, Date DESC);
CREATE INDEX IF NOT EXISTS IX_WeightLogs_Date            ON WeightLogs(Date DESC);

-- =====================================================================
-- TABLA: UserPreferences
-- Propósito: Preferencias de UI/UX por usuario (1:1 con Users).
-- =====================================================================
CREATE TABLE IF NOT EXISTS UserPreferences (
    -- PK: GUID como TEXT
    Id                TEXT        NOT NULL    PRIMARY KEY,

    -- FK al usuario (UNIQUE: relación 1:1)
    UserId            TEXT        NOT NULL    UNIQUE,

    -- Modo oscuro habilitado
    -- SQLite no tiene BOOLEAN; se usa INTEGER (0=false, 1=true)
    -- En SQL Server: BIT NOT NULL DEFAULT 1
    DarkMode          INTEGER     NOT NULL    DEFAULT 1
        CHECK(DarkMode IN (0, 1)),

    -- Notificaciones habilitadas
    NotificationsEnabled INTEGER  NOT NULL    DEFAULT 1
        CHECK(NotificationsEnabled IN (0, 1)),

    -- Zona horaria (IANA timezone, ej: 'America/Argentina/Buenos_Aires')
    -- CHECK: máximo 100 caracteres (los IANA tznames más largos ~30 chars)
    TimeZone          TEXT        NOT NULL    DEFAULT 'America/Argentina/Buenos_Aires'
        CHECK(length(TimeZone) >= 1 AND length(TimeZone) <= 100),

    -- Timestamp de última actualización
    UpdatedAt         TEXT        NOT NULL
        CHECK(length(UpdatedAt) >= 10 AND length(UpdatedAt) <= 30),

    -- FK constraint
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Índice para UserPreferences
CREATE INDEX IF NOT EXISTS IX_UserPreferences_UserId ON UserPreferences(UserId);

-- =====================================================================
-- TABLA: AuditLog
-- Propósito: Registro de auditoría para acciones administrativas.
-- No es para logging general — solo acciones de cambio en entidades.
-- =====================================================================
CREATE TABLE IF NOT EXISTS AuditLog (
    -- PK: GUID como TEXT
    Id                TEXT        NOT NULL    PRIMARY KEY,

    -- FK al usuario que ejecutó la acción
    -- NO CASCADE: mantener audit trail aunque se elimine el usuario
    UserId            TEXT        NOT NULL,

    -- Acción ejecutada (ej: 'UserRoleChanged', 'UserStatusChanged', 'WeightLogDeleted')
    -- CHECK: máximo 100 caracteres
    Action            TEXT        NOT NULL
        CHECK(length(Action) >= 1 AND length(Action) <= 100),

    -- Tipo de entidad afectada (ej: 'User', 'WeightLog')
    -- CHECK: máximo 100 caracteres
    EntityType        TEXT        NOT NULL
        CHECK(length(EntityType) >= 1 AND length(EntityType) <= 100),

    -- ID de la entidad afectada (GUID como TEXT)
    EntityId          TEXT        NOT NULL
        CHECK(length(EntityId) >= 1),

    -- Snapshot JSON del estado ANTES del cambio (NULL si es creación)
    OldValue          TEXT        NULL,

    -- Snapshot JSON del estado DESPUÉS del cambio (NULL si es eliminación)
    NewValue          TEXT        NULL,

    -- Timestamp de la acción
    CreatedAt         TEXT        NOT NULL
        CHECK(length(CreatedAt) >= 10 AND length(CreatedAt) <= 30),

    -- FK constraint (NO CASCADE — preservar audit trail)
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON UPDATE NO ACTION ON DELETE NO ACTION
);

-- Índices para AuditLog
CREATE INDEX IF NOT EXISTS IX_AuditLog_UserId                   ON AuditLog(UserId);
CREATE INDEX IF NOT EXISTS IX_AuditLog_CreatedAt                ON AuditLog(CreatedAt DESC);
CREATE INDEX IF NOT EXISTS IX_AuditLog_EntityType_EntityId      ON AuditLog(EntityType, EntityId);
CREATE INDEX IF NOT EXISTS IX_AuditLog_Action                   ON AuditLog(Action);
