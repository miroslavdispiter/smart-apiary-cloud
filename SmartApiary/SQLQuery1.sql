-- ================================================================
-- SmartApiary - LocalDB Setup Script
-- Pokreni u: SQL Server Object Explorer -> New Query
-- ili: sqlcmd -S "(localdb)\mssqllocaldb" -i CreateDatabase.sql
-- ================================================================

-- Kreiraj bazu ako ne postoji
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SmartApiary')
BEGIN
    CREATE DATABASE SmartApiary;
END
GO

USE SmartApiary;
GO

-- ================================================================
-- Apiaries tabela
-- ================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Apiaries')
BEGIN
    CREATE TABLE Apiaries (
        Id            NVARCHAR(36)    NOT NULL PRIMARY KEY,
        OwnerId       NVARCHAR(36)    NOT NULL,
        Name          NVARCHAR(200)   NOT NULL,
        Latitude      FLOAT           NOT NULL,
        Longitude     FLOAT           NOT NULL,
        Description   NVARCHAR(1000)  NULL,
        ImageUrl      NVARCHAR(500)   NULL,
        ThumbnailUrl  NVARCHAR(500)   NULL,
        CreatedAt     DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        Location      GEOGRAPHY       NULL
    );

    -- Index za brzo pronalazenje pcelinjaka po vlasniku
    CREATE INDEX IX_Apiaries_OwnerId ON Apiaries(OwnerId);

    -- Spatial index za STDistance upite (pesticidi - radijus 5km)
    CREATE SPATIAL INDEX SIX_Apiaries_Location ON Apiaries(Location)
        USING GEOGRAPHY_GRID
        WITH (GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM));

    PRINT 'Tabela Apiaries kreirana.';
END
ELSE
    PRINT 'Tabela Apiaries vec postoji.';
GO

-- ================================================================
-- Hives tabela
-- ================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Hives')
BEGIN
    CREATE TABLE Hives (
        Id        NVARCHAR(36)    NOT NULL PRIMARY KEY,
        ApiaryId  NVARCHAR(36)    NOT NULL,
        Label     NVARCHAR(100)   NOT NULL,
        Type      INT             NOT NULL,   -- HiveType enum (0=LR, 1=DB, 2=Poloski...)
        BoxColor  NVARCHAR(50)    NULL,
        QueenAge  INT             NULL,
        Note      NVARCHAR(1000)  NULL,
        CreatedAt DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_Hives_Apiaries FOREIGN KEY (ApiaryId)
            REFERENCES Apiaries(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_Hives_ApiaryId ON Hives(ApiaryId);

    PRINT 'Tabela Hives kreirana.';
END
ELSE
    PRINT 'Tabela Hives vec postoji.';
GO

--Devices:
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Devices')
BEGIN
    CREATE TABLE Devices (
        Id NVARCHAR(36) NOT NULL PRIMARY KEY,
        HiveId NVARCHAR(36) NOT NULL,
        SerialNumber NVARCHAR(100) NOT NULL,
        DeviceUuid NVARCHAR(100) NULL,
        AccessToken NVARCHAR(500) NULL,
        Status INT NOT NULL,
        RegisteredAt DATETIME2 NOT NULL,
        PairedAt DATETIME2 NULL,
        CONSTRAINT FK_Devices_Hives FOREIGN KEY (HiveId) REFERENCES Hives(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_Devices_HiveId ON Devices(HiveId);

    CREATE UNIQUE INDEX UIX_Devices_SerialNumber ON Devices(SerialNumber);
    PRINT 'Table Devices created successfully.';
END
ELSE
    PRINT 'Table Devices already exists.';
GO

DROP TABLE IF EXISTS TelemetrySamples;

PRINT 'SmartApiary database is ready.';
GO
-- ================================================================
-- HiveInspections tabela 
-- ================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HiveInspections')
BEGIN
    CREATE TABLE HiveInspections (
        Id               NVARCHAR(36)    NOT NULL PRIMARY KEY,
        HiveId           NVARCHAR(36)    NOT NULL,
        InspectionDate   DATETIME2       NOT NULL,
        BottomBoardColor NVARCHAR(50)    NOT NULL,
        HoneyFrames      INT             NOT NULL,
        HoneyKg          FLOAT           NOT NULL,
        BroodFrames      INT             NOT NULL,
        QueenPresent     BIT             NOT NULL,
        Notes            NVARCHAR(1000)  NULL,
        CreatedAt        DATETIME2       NOT NULL,
        CONSTRAINT FK_HiveInspections_Hives FOREIGN KEY (HiveId)
            REFERENCES Hives(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_HiveInspections_HiveId ON HiveInspections(HiveId);
    PRINT 'Tabela HiveInspections kreirana.';
END
ELSE
    PRINT 'Tabela HiveInspections vec postoji.';
GO
-- ================================================================
-- Plots tabela (Clan 5)
-- ================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Plots')
BEGIN
    CREATE TABLE Plots (
        Id          NVARCHAR(36)    NOT NULL PRIMARY KEY,
        OwnerId     NVARCHAR(36)    NOT NULL,
        Name        NVARCHAR(200)   NOT NULL,
        Latitude    FLOAT           NOT NULL,
        Longitude   FLOAT           NOT NULL,
        CreatedAt   DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        Location    GEOGRAPHY       NULL
    );
    CREATE INDEX IX_Plots_OwnerId ON Plots(OwnerId);
    CREATE SPATIAL INDEX SIX_Plots_Location ON Plots(Location)
        USING GEOGRAPHY_GRID
        WITH (GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM));
    PRINT 'Tabela Plots kreirana.';
END
ELSE
    PRINT 'Tabela Plots vec postoji.';
GO

-- ================================================================
-- Cultures tabela (Clan 5)
-- ================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Cultures')
BEGIN
    CREATE TABLE Cultures (
        Id              NVARCHAR(36)    NOT NULL PRIMARY KEY,
        PlotId          NVARCHAR(36)    NOT NULL,
        Type            INT             NOT NULL,   -- CultureType enum
        BloomingStart   DATETIME2       NOT NULL,
        BloomingEnd     DATETIME2       NOT NULL,
        AdditionalInfo  NVARCHAR(500)   NULL,
        CreatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Cultures_Plots FOREIGN KEY (PlotId)
            REFERENCES Plots(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_Cultures_PlotId ON Cultures(PlotId);
    PRINT 'Tabela Cultures kreirana.';
END
ELSE
    PRINT 'Tabela Cultures vec postoji.';
GO

-- ================================================================
-- SprayingSchedules tabela (Clan 5)
-- ================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SprayingSchedules')
BEGIN
    CREATE TABLE SprayingSchedules (
        Id                      NVARCHAR(36)    NOT NULL PRIMARY KEY,
        PlotId                  NVARCHAR(36)    NOT NULL,
        ScheduledAt             DATETIME2       NOT NULL,
        DurationHours           INT             NOT NULL,
        Preparation             NVARCHAR(200)   NULL,
        Status                  INT             NOT NULL DEFAULT 1,  -- SprayingStatus enum (1=Scheduled)
        NotifiedBeekeeperCount  INT             NOT NULL DEFAULT 0,
        CreatedAt               DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_SprayingSchedules_Plots FOREIGN KEY (PlotId)
            REFERENCES Plots(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_SprayingSchedules_PlotId ON SprayingSchedules(PlotId);
    CREATE INDEX IX_SprayingSchedules_ScheduledAt ON SprayingSchedules(ScheduledAt);
    PRINT 'Tabela SprayingSchedules kreirana.';
END
ELSE
    PRINT 'Tabela SprayingSchedules vec postoji.';
GO