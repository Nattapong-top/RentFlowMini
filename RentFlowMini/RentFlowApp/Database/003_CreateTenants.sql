SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

IF OBJECT_ID(N'dbo.Tenants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tenants
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        PhoneNumber NVARCHAR(20) NULL,
        RoomId UNIQUEIDENTIFIER NULL,

        CONSTRAINT PK_Tenants
            PRIMARY KEY (Id),

        CONSTRAINT FK_Tenants_Rooms
            FOREIGN KEY (RoomId) REFERENCES dbo.Rooms (Id)
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UQ_Tenants_RoomId'
      AND object_id = OBJECT_ID(N'dbo.Tenants')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Tenants_RoomId
        ON dbo.Tenants (RoomId)
        WHERE RoomId IS NOT NULL;
END;
