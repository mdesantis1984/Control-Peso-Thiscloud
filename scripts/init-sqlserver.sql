-- ============================================
-- Control Peso Thiscloud - SQL Server Schema
-- Database First approach
-- ============================================
-- Author: ThisCloud Development Team
-- Created: 2026-02-25
-- Version: 1.0.0
-- Description: Initial schema for Control Peso application using SQL Server
-- ============================================

USE master;
GO

-- Create database if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ControlPeso')
BEGIN
    CREATE DATABASE ControlPeso
    COLLATE Modern_Spanish_CI_AS;
END
GO

USE ControlPeso;
GO

-- ============================================
-- Table: Users
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
        [GoogleId] NVARCHAR(200) NULL,
        [LinkedInId] NVARCHAR(200) NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Email] NVARCHAR(320) NOT NULL,
        [Role] INT NOT NULL DEFAULT 0 CHECK ([Role] IN (0, 1)),
        [AvatarUrl] NVARCHAR(2048) NULL,
        [MemberSince] NVARCHAR(50) NOT NULL,
        [Height] FLOAT NOT NULL DEFAULT 170.0 CHECK ([Height] >= 50 AND [Height] <= 300),
        [UnitSystem] INT NOT NULL DEFAULT 0 CHECK ([UnitSystem] IN (0, 1)),
        [DateOfBirth] NVARCHAR(50) NULL,
        [Language] NVARCHAR(10) NOT NULL DEFAULT 'es',
        [Status] INT NOT NULL DEFAULT 0 CHECK ([Status] IN (0, 1, 2)),
        [GoalWeight] FLOAT NULL CHECK ([GoalWeight] IS NULL OR ([GoalWeight] >= 20 AND [GoalWeight] <= 500)),
        [StartingWeight] FLOAT NULL CHECK ([StartingWeight] IS NULL OR ([StartingWeight] >= 20 AND [StartingWeight] <= 500)),
        [CreatedAt] NVARCHAR(50) NOT NULL,
        [UpdatedAt] NVARCHAR(50) NOT NULL
    );

    CREATE UNIQUE INDEX IX_Users_Email ON [Users]([Email]);
    CREATE INDEX IX_Users_GoogleId ON [Users]([GoogleId]);
    CREATE INDEX IX_Users_LinkedInId ON [Users]([LinkedInId]);
    CREATE INDEX IX_Users_Language ON [Users]([Language]);
    CREATE INDEX IX_Users_Role ON [Users]([Role]);
    CREATE INDEX IX_Users_Status ON [Users]([Status]);
END
GO

-- ============================================
-- Table: WeightLogs
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WeightLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[WeightLogs] (
        [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(450) NOT NULL,
        [Date] NVARCHAR(50) NOT NULL,
        [Time] NVARCHAR(50) NOT NULL,
        [Weight] FLOAT NOT NULL CHECK ([Weight] >= 20 AND [Weight] <= 500),
        [DisplayUnit] INT NOT NULL DEFAULT 0 CHECK ([DisplayUnit] IN (0, 1)),
        [Note] NVARCHAR(500) NULL,
        [Trend] INT NOT NULL DEFAULT 2 CHECK ([Trend] IN (0, 1, 2)),
        [CreatedAt] NVARCHAR(50) NOT NULL,
        FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX IX_WeightLogs_UserId ON [WeightLogs]([UserId]);
    CREATE INDEX IX_WeightLogs_Date ON [WeightLogs]([Date] DESC);
    CREATE INDEX IX_WeightLogs_UserId_Date ON [WeightLogs]([UserId], [Date] DESC);
END
GO

-- ============================================
-- Table: UserPreferences
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserPreferences]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserPreferences] (
        [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(450) NOT NULL UNIQUE,
        [DarkMode] BIGINT NOT NULL DEFAULT 1,
        [NotificationsEnabled] BIGINT NOT NULL DEFAULT 1,
        [TimeZone] NVARCHAR(100) NOT NULL DEFAULT 'America/Argentina/Buenos_Aires',
        [UpdatedAt] NVARCHAR(50) NOT NULL,
        FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX IX_UserPreferences_UserId ON [UserPreferences]([UserId]);
END
GO

-- ============================================
-- Table: UserNotifications
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserNotifications]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserNotifications] (
        [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(450) NOT NULL,
        [Type] NVARCHAR(50) NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Message] NVARCHAR(1000) NOT NULL,
        [IsRead] BIT NOT NULL DEFAULT 0,
        [ReadAt] NVARCHAR(50) NULL,
        [RelatedEntityType] NVARCHAR(100) NULL,
        [RelatedEntityId] NVARCHAR(450) NULL,
        [CreatedAt] NVARCHAR(50) NOT NULL,
        FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX IX_UserNotifications_UserId ON [UserNotifications]([UserId]);
    CREATE INDEX IX_UserNotifications_IsRead ON [UserNotifications]([IsRead]);
    CREATE INDEX IX_UserNotifications_UserId_IsRead ON [UserNotifications]([UserId], [IsRead]);
    CREATE INDEX IX_UserNotifications_CreatedAt ON [UserNotifications]([CreatedAt] DESC);
END
GO

-- ============================================
-- Table: AuditLog
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditLog]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AuditLog] (
        [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(450) NOT NULL,
        [Action] NVARCHAR(100) NOT NULL,
        [EntityType] NVARCHAR(100) NOT NULL,
        [EntityId] NVARCHAR(450) NOT NULL,
        [OldValue] NVARCHAR(MAX) NULL,
        [NewValue] NVARCHAR(MAX) NULL,
        [CreatedAt] NVARCHAR(50) NOT NULL,
        FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE NO ACTION
    );

    CREATE INDEX IX_AuditLog_UserId ON [AuditLog]([UserId]);
    CREATE INDEX IX_AuditLog_Action ON [AuditLog]([Action]);
    CREATE INDEX IX_AuditLog_EntityType_EntityId ON [AuditLog]([EntityType], [EntityId]);
    CREATE INDEX IX_AuditLog_CreatedAt ON [AuditLog]([CreatedAt] DESC);
END
GO

PRINT 'Control Peso database schema created successfully!';
GO
