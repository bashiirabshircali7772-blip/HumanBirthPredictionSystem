/* =====================================================================
   Human Birth Prediction and Gender Distribution System
   Manual SQL Server schema (fallback if EF Core Migrations are not used)

   Usage:
     1. Open SQL Server Management Studio, connect to localhost\SQLEXPRESS01
     2. Run this entire script against a new database named
        HumanBirthPredictionDB (the CREATE DATABASE line below does this).

   NOTE: If you use "dotnet ef database update" (recommended, see README),
   you do NOT need to run this file — EF Core will create an equivalent
   schema automatically from the model classes and migrations.
   ===================================================================== */

IF DB_ID('HumanBirthPredictionDB') IS NULL
BEGIN
    CREATE DATABASE HumanBirthPredictionDB;
END
GO

USE HumanBirthPredictionDB;
GO

CREATE TABLE Users (
    Id            INT IDENTITY PRIMARY KEY,
    Username      NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(MAX) NOT NULL,
    FullName      NVARCHAR(150) NULL,
    CreatedAt     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Countries (
    Id           INT IDENTITY PRIMARY KEY,
    CountryName  NVARCHAR(100) NOT NULL,
    CountryCode  NVARCHAR(10)  NOT NULL UNIQUE,
    Continent    NVARCHAR(50)  NULL,
    CreatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Cities (
    Id         INT IDENTITY PRIMARY KEY,
    CountryId  INT NOT NULL FOREIGN KEY REFERENCES Countries(Id) ON DELETE CASCADE,
    CityName   NVARCHAR(100) NOT NULL,
    CreatedAt  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_City_PerCountry UNIQUE (CountryId, CityName)
);

CREATE TABLE BirthRecords (
    Id              INT IDENTITY PRIMARY KEY,
    CountryId       INT NOT NULL FOREIGN KEY REFERENCES Countries(Id),
    CityId          INT NULL FOREIGN KEY REFERENCES Cities(Id),
    Year            INT NOT NULL CHECK (Year BETWEEN 1900 AND 2100),
    TotalBirths     INT NOT NULL CHECK (TotalBirths >= 0),
    MaleBirths      INT NOT NULL CHECK (MaleBirths >= 0),
    FemaleBirths    INT NOT NULL CHECK (FemaleBirths >= 0),
    DataSource      NVARCHAR(150) NULL,
    SourceReference NVARCHAR(300) NULL,
    RecordType      INT NOT NULL,  -- 0=Official 1=Historical 2=Estimated 3=Predicted
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_BirthRecord UNIQUE (CountryId, CityId, Year, RecordType),
    CONSTRAINT CK_BirthRecord_Sum CHECK (MaleBirths + FemaleBirths = TotalBirths)
);
CREATE INDEX IX_BirthRecords_Year ON BirthRecords(Year);

CREATE TABLE Predictions (
    Id                     INT IDENTITY PRIMARY KEY,
    CountryId              INT NOT NULL FOREIGN KEY REFERENCES Countries(Id),
    CityId                 INT NULL FOREIGN KEY REFERENCES Cities(Id),
    Year                   INT NOT NULL CHECK (Year BETWEEN 2000 AND 2100),
    PredictedTotalBirths   INT NOT NULL,
    PredictedMaleBirths    INT NOT NULL,
    PredictedFemaleBirths  INT NOT NULL,
    PredictionModel        NVARCHAR(100) NOT NULL DEFAULT 'Linear Regression',
    PredictionDate         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_Predictions_Lookup ON Predictions(CountryId, CityId, Year, PredictionModel);

-- Seed the initial administrator account.
-- Password: sakariye21  (hashed with PBKDF2-SHA256, 100,000 iterations,
-- matching Data/PasswordHasher.cs — format: iterations.salt.hash)
-- NOTE: If you use EF Core + DbSeeder.cs instead, this INSERT is not
-- needed — the app seeds the same account automatically on first run.
-- To insert the admin account manually, run the application once (it
-- seeds automatically via Data/DbSeeder.cs), or generate a hash using
-- the PasswordHasher.Hash() method and insert it here.
