CREATE TABLE dbo.Rooms
(
    Id UNIQUEIDENTIFIER NOT NULL,
    RoomNumber NVARCHAR(50) NOT NULL,

    CONSTRAINT PK_Rooms
        PRIMARY KEY (Id),

    CONSTRAINT UQ_Rooms_RoomNumber
        UNIQUE (RoomNumber)
);