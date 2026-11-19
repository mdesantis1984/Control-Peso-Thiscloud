CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261118214026_InitialPostgreSql') THEN
    CREATE EXTENSION IF NOT EXISTS citext;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261118214026_InitialPostgreSql') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "GoogleId" text,
        "LinkedInId" text,
        "Name" text NOT NULL,
        "Email" citext NOT NULL,
        "Role" integer NOT NULL,
        "AvatarUrl" text,
        "MemberSince" timestamp without time zone NOT NULL DEFAULT ((now() at time zone 'utc')),
        "Height" numeric NOT NULL DEFAULT 170.0,
        "UnitSystem" integer NOT NULL,
        "DateOfBirth" date,
        "Language" text NOT NULL DEFAULT 'es',
        "Status" integer NOT NULL,
        "GoalWeight" numeric,
        "StartingWeight" numeric,
        "CreatedAt" timestamp without time zone NOT NULL DEFAULT ((now() at time zone 'utc')),
        "UpdatedAt" timestamp without time zone NOT NULL DEFAULT ((now() at time zone 'utc')),
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261118214026_InitialPostgreSql') THEN
    CREATE TABLE "AuditLog" (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "UserId" uuid NOT NULL,
        "Action" text NOT NULL,
        "EntityType" text NOT NULL,
        "EntityId" text NOT NULL,
        "OldValue" text,
        "NewValue" text,
        "CreatedAt" timestamp without time zone NOT NULL DEFAULT ((now() at time zone 'utc')),
        CONSTRAINT "PK_AuditLog" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AuditLog_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261118214026_InitialPostgreSql') THEN
    CREATE TABLE "UserNotifications" (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "UserId" uuid NOT NULL,
        "Type" text NOT NULL,
        "Title" text NOT NULL,
        "Message" text NOT NULL,
        "IsRead" boolean NOT NULL,
        "CreatedAt" timestamp without time zone NOT NULL DEFAULT ((now() at time zone 'utc')),
        CONSTRAINT "PK_UserNotifications" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserNotifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261118214026_InitialPostgreSql') THEN
    CREATE TABLE "UserPreferences" (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "UserId" uuid NOT NULL,
        "DarkMode" boolean NOT NULL DEFAULT TRUE,
        "NotificationsEnabled" boolean NOT NULL DEFAULT TRUE,
        "TimeZone" text NOT NULL DEFAULT 'America/Argentina/Buenos_Aires',
        "UpdatedAt" timestamp without time zone NOT NULL DEFAULT ((now() at time zone 'utc')),
        CONSTRAINT "PK_UserPreferences" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserPreferences_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261118214026_InitialPostgreSql') THEN
    CREATE TABLE "WeightLogs" (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "UserId" uuid NOT NULL,
        "Date" date NOT NULL,
        "Time" time without time zone NOT NULL,
        "Weight" numeric NOT NULL,
        "DisplayUnit" integer NOT NULL,
        "Note" text,
        "Trend" integer NOT NULL DEFAULT 2,
        "CreatedAt" timestamp without time zone NOT NULL DEFAULT ((now() at time zone 'utc')),
        CONSTRAINT "PK_WeightLogs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_WeightLogs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261118214026_InitialPostgreSql') THEN
    CREATE INDEX "IX_AuditLog_UserId" ON "AuditLog" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261118214026_InitialPostgreSql') THEN
    CREATE INDEX "IX_UserNotifications_UserId" ON "UserNotifications" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261118214026_InitialPostgreSql') THEN
    CREATE UNIQUE INDEX "IX_UserPreferences_UserId" ON "UserPreferences" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261118214026_InitialPostgreSql') THEN
    CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261118214026_InitialPostgreSql') THEN
    CREATE UNIQUE INDEX "IX_WeightLogs_UserId_Date_Time" ON "WeightLogs" ("UserId", "Date", "Time");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261118214026_InitialPostgreSql') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261118214026_InitialPostgreSql', '10.0.3');
    END IF;
END $EF$;
COMMIT;

