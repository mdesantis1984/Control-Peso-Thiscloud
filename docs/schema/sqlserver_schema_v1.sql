-- =====================================================================
-- SQL SERVER MIGRATION SCRIPT
-- Control Peso Thiscloud - Database Creation
-- Target: SQL Server Express 2019+ / Azure SQL Database
-- =====================================================================

USE master;
GO

-- Drop database if exists (CAUTION: Only for fresh setup)
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'ControlPeso')
BEGIN
    ALTER DATABASE ControlPeso SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ControlPeso;
END
GO

-- Create database
CREATE DATABASE ControlPeso
    COLLATE SQL_Latin1_General_CP1_CI_AS;
GO

USE ControlPeso;
GO

-- =====================================================================
-- TABLE: Users
-- =====================================================================
CREATE TABLE Users (
    Id                  UNIQUEIDENTIFIER    NOT NULL    PRIMARY KEY DEFAULT NEWID(),
    GoogleId            NVARCHAR(200)       NULL        UNIQUE,
    LinkedInId          NVARCHAR(200)       NULL        UNIQUE,
    Name                NVARCHAR(200)       NOT NULL    CHECK(LEN(Name) >= 1 AND LEN(Name) <= 200),
    Email               NVARCHAR(320)       NOT NULL    UNIQUE CHECK(LEN(Email) >= 5 AND LEN(Email) <= 320),
    Role                INT                 NOT NULL    DEFAULT 0 CHECK(Role IN (0, 1)),
    AvatarUrl           NVARCHAR(MAX)       NULL,
    MemberSince         DATETIME2(7)        NOT NULL    DEFAULT GETUTCDATE(),
    Height              DECIMAL(5,2)        NOT NULL    DEFAULT 170.0 CHECK(Height >= 50.0 AND Height <= 300.0),
    UnitSystem          INT                 NOT NULL    DEFAULT 0 CHECK(UnitSystem IN (0, 1)),
    DateOfBirth         DATE                NULL,
    Language            NVARCHAR(10)        NOT NULL    DEFAULT 'es' CHECK(Language IN ('es', 'en')),
    Status              INT                 NOT NULL    DEFAULT 0 CHECK(Status IN (0, 1, 2)),
    GoalWeight          DECIMAL(5,2)        NULL        CHECK(GoalWeight IS NULL OR (GoalWeight >= 20.0 AND GoalWeight <= 500.0)),
    StartingWeight      DECIMAL(5,2)        NULL        CHECK(StartingWeight IS NULL OR (StartingWeight >= 20.0 AND StartingWeight <= 500.0)),
    CreatedAt           DATETIME2(7)        NOT NULL    DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2(7)        NOT NULL    DEFAULT GETUTCDATE()
);
GO

CREATE INDEX IX_Users_GoogleId ON Users(GoogleId) WHERE GoogleId IS NOT NULL;
CREATE INDEX IX_Users_LinkedInId ON Users(LinkedInId) WHERE LinkedInId IS NOT NULL;
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_Status ON Users(Status);
GO

-- =====================================================================
-- TABLE: WeightLogs
-- =====================================================================
CREATE TABLE WeightLogs (
    Id                  UNIQUEIDENTIFIER    NOT NULL    PRIMARY KEY DEFAULT NEWID(),
    UserId              UNIQUEIDENTIFIER    NOT NULL    FOREIGN KEY REFERENCES Users(Id) ON DELETE CASCADE,
    Date                DATE                NOT NULL,
    Time                TIME(0)             NOT NULL,
    Weight              DECIMAL(5,2)        NOT NULL    CHECK(Weight >= 20.0 AND Weight <= 500.0),
    DisplayUnit         INT                 NOT NULL    DEFAULT 0 CHECK(DisplayUnit IN (0, 1)),
    Note                NVARCHAR(500)       NULL,
    Trend               INT                 NOT NULL    DEFAULT 2 CHECK(Trend IN (0, 1, 2)),
    CreatedAt           DATETIME2(7)        NOT NULL    DEFAULT GETUTCDATE()
);
GO

CREATE INDEX IX_WeightLogs_UserId ON WeightLogs(UserId);
CREATE INDEX IX_WeightLogs_Date ON WeightLogs(Date DESC);
CREATE INDEX IX_WeightLogs_UserId_Date ON WeightLogs(UserId, Date DESC);
CREATE UNIQUE INDEX UQ_WeightLogs_UserId_Date_Time ON WeightLogs(UserId, Date, Time);
GO

-- =====================================================================
-- TABLE: UserPreferences
-- =====================================================================
CREATE TABLE UserPreferences (
    Id                  UNIQUEIDENTIFIER    NOT NULL    PRIMARY KEY DEFAULT NEWID(),
    UserId              UNIQUEIDENTIFIER    NOT NULL    UNIQUE FOREIGN KEY REFERENCES Users(Id) ON DELETE CASCADE,
    DarkMode            BIT                 NOT NULL    DEFAULT 1,
    NotificationsEnabled BIT                NOT NULL    DEFAULT 1,
    TimeZone            NVARCHAR(100)       NOT NULL    DEFAULT 'America/Argentina/Buenos_Aires',
    UpdatedAt           DATETIME2(7)        NOT NULL    DEFAULT GETUTCDATE()
);
GO

CREATE INDEX IX_UserPreferences_UserId ON UserPreferences(UserId);
GO

-- =====================================================================
-- TABLE: AuditLog
-- =====================================================================
CREATE TABLE AuditLog (
    Id                  UNIQUEIDENTIFIER    NOT NULL    PRIMARY KEY DEFAULT NEWID(),
    UserId              UNIQUEIDENTIFIER    NOT NULL    FOREIGN KEY REFERENCES Users(Id) ON DELETE CASCADE,
    Action              NVARCHAR(100)       NOT NULL,
    EntityType          NVARCHAR(100)       NOT NULL,
    EntityId            NVARCHAR(450)       NOT NULL,
    OldValue            NVARCHAR(MAX)       NULL,
    NewValue            NVARCHAR(MAX)       NULL,
    CreatedAt           DATETIME2(7)        NOT NULL    DEFAULT GETUTCDATE()
);
GO

CREATE INDEX IX_AuditLog_UserId ON AuditLog(UserId);
CREATE INDEX IX_AuditLog_CreatedAt ON AuditLog(CreatedAt DESC);
CREATE INDEX IX_AuditLog_EntityType_EntityId ON AuditLog(EntityType, EntityId);
GO

PRINT 'Database ControlPeso created successfully!';
PRINT 'Tables: Users, WeightLogs, UserPreferences, AuditLog';
PRINT 'Indexes created for optimal performance';
GO
