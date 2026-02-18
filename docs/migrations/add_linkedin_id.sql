-- =====================================================================
-- MIGRATION: Add LinkedInId column to Users table
-- Date: 2026-02-18
-- Purpose: Support LinkedIn OAuth alongside Google OAuth
-- =====================================================================

PRAGMA foreign_keys = OFF;

BEGIN TRANSACTION;

-- 1. Crear tabla temporal con nueva estructura
CREATE TABLE Users_new (
    Id                TEXT        NOT NULL    PRIMARY KEY,
    GoogleId          TEXT        NULL        UNIQUE,
    LinkedInId        TEXT        NULL        UNIQUE,
    Name              TEXT        NOT NULL
        CHECK(length(Name) >= 1 AND length(Name) <= 200),
    Email             TEXT        NOT NULL    UNIQUE
        CHECK(length(Email) >= 5 AND length(Email) <= 320),
    Role              INTEGER     NOT NULL    DEFAULT 0
        CHECK(Role IN (0, 1)),
    AvatarUrl         TEXT        NULL
        CHECK(AvatarUrl IS NULL OR length(AvatarUrl) <= 2048),
    MemberSince       TEXT        NOT NULL
        CHECK(length(MemberSince) >= 10 AND length(MemberSince) <= 30),
    Height            REAL        NOT NULL    DEFAULT 170.0
        CHECK(Height >= 50.0 AND Height <= 300.0),
    UnitSystem        INTEGER     NOT NULL    DEFAULT 0
        CHECK(UnitSystem IN (0, 1)),
    DateOfBirth       TEXT        NULL
        CHECK(DateOfBirth IS NULL OR length(DateOfBirth) = 10),
    Language          TEXT        NOT NULL    DEFAULT 'es'
        CHECK(Language IN ('es', 'en')),
    Status            INTEGER     NOT NULL    DEFAULT 0
        CHECK(Status IN (0, 1, 2)),
    GoalWeight        REAL        NULL
        CHECK(GoalWeight IS NULL OR (GoalWeight >= 20.0 AND GoalWeight <= 500.0)),
    StartingWeight    REAL        NULL
        CHECK(StartingWeight IS NULL OR (StartingWeight >= 20.0 AND StartingWeight <= 500.0)),
    CreatedAt         TEXT        NOT NULL
        CHECK(length(CreatedAt) >= 10 AND length(CreatedAt) <= 30),
    UpdatedAt         TEXT        NOT NULL
        CHECK(length(UpdatedAt) >= 10 AND length(UpdatedAt) <= 30),
    CHECK((GoogleId IS NOT NULL) OR (LinkedInId IS NOT NULL))
);

-- 2. Copiar datos de tabla vieja a nueva (LinkedInId será NULL para registros existentes)
INSERT INTO Users_new (
    Id, GoogleId, LinkedInId, Name, Email, Role, AvatarUrl, MemberSince,
    Height, UnitSystem, DateOfBirth, Language, Status, GoalWeight,
    StartingWeight, CreatedAt, UpdatedAt
)
SELECT 
    Id, GoogleId, NULL as LinkedInId, Name, Email, Role, AvatarUrl, MemberSince,
    Height, UnitSystem, DateOfBirth, Language, Status, GoalWeight,
    StartingWeight, CreatedAt, UpdatedAt
FROM Users;

-- 3. Eliminar tabla vieja
DROP TABLE Users;

-- 4. Renombrar tabla nueva
ALTER TABLE Users_new RENAME TO Users;

-- 5. Recrear índices
CREATE INDEX IF NOT EXISTS IX_Users_GoogleId   ON Users(GoogleId);
CREATE INDEX IF NOT EXISTS IX_Users_LinkedInId ON Users(LinkedInId);
CREATE INDEX IF NOT EXISTS IX_Users_Email      ON Users(Email);

COMMIT;

PRAGMA foreign_keys = ON;

-- Verificar migración
SELECT 'Migration completed successfully. Total users:', COUNT(*) FROM Users;
