-- ============================================================================
-- FIX COMPLETO BASE DE DATOS - Control Peso Thiscloud
-- ============================================================================
-- Ejecutar en SQL Server Management Studio (SSMS)
-- Database: ControlPeso
-- ============================================================================

USE ControlPeso;
GO

-- ============================================================================
-- 1. CREAR TABLAS FALTANTES
-- ============================================================================

-- Tabla UserNotifications (CRÍTICO - falta completamente)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserNotifications]') AND type in (N'U'))
BEGIN
    CREATE TABLE UserNotifications (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        UserId UNIQUEIDENTIFIER NOT NULL,
        [Type] NVARCHAR(50) NOT NULL,  -- CRITICAL: Brackets - SQL reserved word
        Title NVARCHAR(200) NOT NULL,
        Message NVARCHAR(1000) NOT NULL,
        IsRead BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_UserNotifications_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_UserNotifications_UserId ON UserNotifications(UserId);
    PRINT '✅ Tabla UserNotifications creada';
END
ELSE
    PRINT '⚠️ Tabla UserNotifications ya existe';
GO

-- Tabla WeightLogs (probablemente falta)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WeightLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE WeightLogs (
        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
        UserId NVARCHAR(450) NOT NULL,
        [Date] NVARCHAR(10) NOT NULL,
        [Time] NVARCHAR(5) NOT NULL,
        Weight FLOAT NOT NULL CHECK (Weight BETWEEN 20 AND 500),
        DisplayUnit INT NOT NULL DEFAULT 0 CHECK (DisplayUnit IN (0, 1)),
        Note NVARCHAR(500) NULL,
        Trend INT NOT NULL DEFAULT 2 CHECK (Trend IN (0, 1, 2)),
        CreatedAt NVARCHAR(100) NOT NULL,
        CONSTRAINT FK_WeightLogs_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_WeightLogs_UserId ON WeightLogs(UserId);
    PRINT '✅ Tabla WeightLogs creada';
END
ELSE
    PRINT '⚠️ Tabla WeightLogs ya existe';
GO

-- Tabla AuditLog (probablemente falta)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditLog]') AND type in (N'U'))
BEGIN
    CREATE TABLE AuditLog (
        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
        UserId NVARCHAR(450) NOT NULL,
        Action NVARCHAR(100) NOT NULL,
        EntityType NVARCHAR(100) NOT NULL,
        EntityId NVARCHAR(450) NOT NULL,
        OldValue NVARCHAR(MAX) NULL,
        NewValue NVARCHAR(MAX) NULL,
        CreatedAt NVARCHAR(100) NOT NULL,
        CONSTRAINT FK_AuditLog_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_AuditLog_UserId ON AuditLog(UserId);
    PRINT '✅ Tabla AuditLog creada';
END
ELSE
    PRINT '⚠️ Tabla AuditLog ya existe';
GO

-- ============================================================================
-- 2. INSERTAR USUARIO DE DESARROLLO (Si no existe)
-- ============================================================================

-- Usuario: marco.desantis@thiscloud.com (el que usa DevelopmentAuthMiddleware)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'marco.desantis@thiscloud.com')
BEGIN
    INSERT INTO Users (
        Id, 
        GoogleId, 
        Name, 
        Email, 
        Role, 
        AvatarUrl, 
        MemberSince, 
        Height, 
        UnitSystem, 
        DateOfBirth, 
        Language, 
        Status, 
        GoalWeight, 
        StartingWeight, 
        CreatedAt, 
        UpdatedAt
    )
    VALUES (
        '550e8400-e29b-41d4-a716-446655440001',  -- UserId (mismo que usa DevelopmentAuth)
        'google_demo_admin_001',                  -- GoogleId
        'Marco De Santis',                        -- Name
        'marco.desantis@thiscloud.com',          -- Email
        0,                                        -- Role (0 = User)
        NULL,                                     -- AvatarUrl
        FORMAT(GETUTCDATE(), 'yyyy-MM-ddTHH:mm:ss.fffffffZ'), -- MemberSince
        170.0,                                    -- Height (cm)
        0,                                        -- UnitSystem (0 = Metric)
        NULL,                                     -- DateOfBirth
        'es',                                     -- Language
        0,                                        -- Status (0 = Active)
        NULL,                                     -- GoalWeight
        NULL,                                     -- StartingWeight
        FORMAT(GETUTCDATE(), 'yyyy-MM-ddTHH:mm:ss.fffffffZ'), -- CreatedAt
        FORMAT(GETUTCDATE(), 'yyyy-MM-ddTHH:mm:ss.fffffffZ')  -- UpdatedAt
    );
    PRINT '✅ Usuario marco.desantis@thiscloud.com creado';
END
ELSE
    PRINT '⚠️ Usuario marco.desantis@thiscloud.com ya existe';
GO

-- ============================================================================
-- 3. INSERTAR UserPreferences (Si no existe)
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM UserPreferences WHERE UserId = '550e8400-e29b-41d4-a716-446655440001')
BEGIN
    INSERT INTO UserPreferences (
        Id,
        UserId,
        DarkMode,
        NotificationsEnabled,
        TimeZone,
        UpdatedAt
    )
    VALUES (
        NEWID(),
        '550e8400-e29b-41d4-a716-446655440001',
        1,                                        -- DarkMode (1 = true)
        1,                                        -- NotificationsEnabled (1 = true)
        'America/Argentina/Buenos_Aires',        -- TimeZone
        FORMAT(GETUTCDATE(), 'yyyy-MM-ddTHH:mm:ss.fffffffZ')
    );
    PRINT '✅ UserPreferences creadas para marco.desantis@thiscloud.com';
END
ELSE
    PRINT '⚠️ UserPreferences ya existen para este usuario';
GO

-- ============================================================================
-- 4. VERIFICACIÓN FINAL
-- ============================================================================

PRINT '';
PRINT '=== VERIFICACIÓN FINAL ===';
PRINT 'Tablas creadas:';
SELECT name FROM sys.tables WHERE name IN ('Users', 'UserPreferences', 'UserNotifications', 'WeightLogs', 'AuditLog') ORDER BY name;

PRINT '';
PRINT 'Usuario de desarrollo:';
SELECT Id, Email, Name, Role, Status FROM Users WHERE Email = 'marco.desantis@thiscloud.com';

PRINT '';
PRINT '✅ FIX COMPLETADO - Reiniciar aplicación para aplicar cambios';
