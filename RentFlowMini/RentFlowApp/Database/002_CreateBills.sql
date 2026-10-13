CREATE TABLE dbo.Bills
(
    Id UNIQUEIDENTIFIER NOT NULL,
    RoomId UNIQUEIDENTIFIER NOT NULL,
    BillingPeriod NVARCHAR(7) NOT NULL,
    RentAmount DECIMAL(18, 2) NOT NULL,
    WaterAmount DECIMAL(18, 2) NOT NULL,
    ElectricityAmount DECIMAL(18, 2) NOT NULL,
    TotalAmount DECIMAL(18, 2) NOT NULL,

    CONSTRAINT PK_Bills
        PRIMARY KEY (Id),

    CONSTRAINT FK_Bills_Rooms
        FOREIGN KEY (RoomId) REFERENCES dbo.Rooms (Id),

    CONSTRAINT UQ_Bills_RoomId_BillingPeriod
        UNIQUE (RoomId, BillingPeriod)
);
