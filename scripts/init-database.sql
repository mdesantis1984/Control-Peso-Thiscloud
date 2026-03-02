-- ============================================
-- Control Peso Thiscloud - SQL Server Schema
-- Database First approach
-- ============================================
-- Author: ThisCloud Development Team
-- Created: 2026-02-25
-- Version: 1.1.0
-- Description: Schema sincronizado con SQL Server Express local funcionando
-- ============================================

USE master;
GO

-- Create database if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ControlPeso')
BEGIN
    CREATE DATABASE ControlPeso
    COLLATE SQL_Latin1_General_CP1_CI_AS;
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
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
        [GoogleId] NVARCHAR(200) NULL,
        [LinkedInId] NVARCHAR(200) NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Email] NVARCHAR(320) NOT NULL,
        [Role] INT NOT NULL DEFAULT 0,
        [AvatarUrl] NVARCHAR(MAX) NULL,
        [MemberSince] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [Height] DECIMAL(5,2) NOT NULL DEFAULT 170.0,
        [UnitSystem] INT NOT NULL DEFAULT 0,
        [DateOfBirth] DATE NULL,
        [Language] NVARCHAR(10) NOT NULL DEFAULT 'es',
        [Status] INT NOT NULL DEFAULT 0,
        [GoalWeight] DECIMAL(5,2) NULL,
        [StartingWeight] DECIMAL(5,2) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        -- Constraints
        CONSTRAINT CK_Users_Name CHECK (LEN([Name]) >= 1 AND LEN([Name]) <= 200),
        CONSTRAINT CK_Users_Email CHECK (LEN([Email]) >= 5 AND LEN([Email]) <= 320),
        CONSTRAINT CK_Users_Role CHECK ([Role] IN (0, 1)),
        CONSTRAINT CK_Users_Height CHECK ([Height] >= 50.0 AND [Height] <= 300.0),
        CONSTRAINT CK_Users_UnitSystem CHECK ([UnitSystem] IN (0, 1)),
        CONSTRAINT CK_Users_Language CHECK ([Language] IN ('es', 'en')),
        CONSTRAINT CK_Users_Status CHECK ([Status] IN (0, 1, 2)),
        CONSTRAINT CK_Users_GoalWeight CHECK ([GoalWeight] IS NULL OR ([GoalWeight] >= 20.0 AND [GoalWeight] <= 500.0)),
        CONSTRAINT CK_Users_StartingWeight CHECK ([StartingWeight] IS NULL OR ([StartingWeight] >= 20.0 AND [StartingWeight] <= 500.0))
    );

    -- Indexes
    CREATE UNIQUE INDEX IX_Users_Email ON [Users]([Email]);
    CREATE UNIQUE INDEX IX_Users_GoogleId ON [Users]([GoogleId]) WHERE [GoogleId] IS NOT NULL;
    CREATE UNIQUE INDEX IX_Users_LinkedInId ON [Users]([LinkedInId]) WHERE [LinkedInId] IS NOT NULL;
END
GO

-- ============================================
-- Table: WeightLogs
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WeightLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[WeightLogs] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [Date] DATE NOT NULL,
        [Time] TIME NOT NULL,
        [Weight] DECIMAL(5,2) NOT NULL,
        [DisplayUnit] INT NOT NULL DEFAULT 0,
        [Note] NVARCHAR(500) NULL,
        [Trend] INT NOT NULL DEFAULT 2,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        -- Constraints
        CONSTRAINT CK_WeightLogs_Weight CHECK ([Weight] >= 20.0 AND [Weight] <= 500.0),
        CONSTRAINT CK_WeightLogs_DisplayUnit CHECK ([DisplayUnit] IN (0, 1)),
        CONSTRAINT CK_WeightLogs_Trend CHECK ([Trend] IN (0, 1, 2)),
        CONSTRAINT FK_WeightLogs_Users FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
    );

    -- Indexes
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
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
        [UserId] UNIQUEIDENTIFIER NOT NULL UNIQUE,
        [DarkMode] BIT NOT NULL DEFAULT 1,
        [NotificationsEnabled] BIT NOT NULL DEFAULT 1,
        [TimeZone] NVARCHAR(100) NOT NULL DEFAULT 'America/Argentina/Buenos_Aires',
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        -- Constraints
        CONSTRAINT FK_UserPreferences_Users FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
    );

    -- Indexes
    CREATE UNIQUE INDEX IX_UserPreferences_UserId ON [UserPreferences]([UserId]);
END
GO

-- ============================================
-- Table: UserNotifications
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserNotifications]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserNotifications] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [Type] NVARCHAR(50) NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Message] NVARCHAR(1000) NOT NULL,
        [IsRead] BIT NOT NULL DEFAULT 0,
        [ReadAt] DATETIME2 NULL,
        [RelatedEntityType] NVARCHAR(100) NULL,
        [RelatedEntityId] UNIQUEIDENTIFIER NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        -- Constraints
        CONSTRAINT FK_UserNotifications_Users FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
    );

    -- Indexes
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
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [Action] NVARCHAR(100) NOT NULL,
        [EntityType] NVARCHAR(100) NOT NULL,
        [EntityId] UNIQUEIDENTIFIER NOT NULL,
        [OldValue] NVARCHAR(MAX) NULL,
        [NewValue] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        -- Constraints
        CONSTRAINT FK_AuditLog_Users FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE NO ACTION
    );

    -- Indexes
    CREATE INDEX IX_AuditLog_UserId ON [AuditLog]([UserId]);
    CREATE INDEX IX_AuditLog_Action ON [AuditLog]([Action]);
    CREATE INDEX IX_AuditLog_EntityType_EntityId ON [AuditLog]([EntityType], [EntityId]);
    CREATE INDEX IX_AuditLog_CreatedAt ON [AuditLog]([CreatedAt] DESC);
END
GO

PRINT 'Control Peso database schema created successfully!';
GO
