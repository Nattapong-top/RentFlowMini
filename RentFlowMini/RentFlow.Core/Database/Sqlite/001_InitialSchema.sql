CREATE TABLE Rooms (
    Id              TEXT    NOT NULL PRIMARY KEY,
    RoomNumber      TEXT    NOT NULL UNIQUE,
    MonthlyRent     TEXT    NOT NULL,
    IsCableTvExempt INTEGER NOT NULL CHECK (IsCableTvExempt IN (0, 1))
) STRICT;

CREATE TABLE Tenants (
    Id                 TEXT NOT NULL PRIMARY KEY,
    FirstName          TEXT NOT NULL,
    LastName           TEXT NOT NULL,
    PhoneNumber        TEXT NULL,
    RoomId             TEXT NULL REFERENCES Rooms (Id),
    MoveInDate         TEXT NULL,
    SecurityDeposit    TEXT NULL,
    MovedOutOn         TEXT NULL,
    MovedOutFromRoomId TEXT NULL REFERENCES Rooms (Id)
) STRICT;

-- ห้องละ 1 คน ส่วนผู้ที่ย้ายออกแล้วไม่มีห้อง จึงไม่ติดกฎนี้
CREATE UNIQUE INDEX UQ_Tenants_RoomId
    ON Tenants (RoomId)
    WHERE RoomId IS NOT NULL;

CREATE TABLE MonthlyMeterReadings (
    RoomId                   TEXT NOT NULL REFERENCES Rooms (Id),
    BillingPeriod            TEXT NOT NULL,
    PreviousWaterMeter       TEXT NOT NULL,
    CurrentWaterMeter        TEXT NOT NULL,
    PreviousElectricityMeter TEXT NOT NULL,
    CurrentElectricityMeter  TEXT NOT NULL,
    PRIMARY KEY (RoomId, BillingPeriod)
) STRICT;

CREATE TABLE Bills (
    Id                       TEXT    NOT NULL PRIMARY KEY,
    RoomId                   TEXT    NOT NULL REFERENCES Rooms (Id),
    BillingPeriod            TEXT    NOT NULL,
    InvoiceNumber            TEXT    NOT NULL,
    CreatedAt                TEXT    NOT NULL,
    DueDate                  TEXT    NOT NULL,

    RentAmount               TEXT    NOT NULL,
    WaterAmount              TEXT    NOT NULL,
    ElectricityAmount        TEXT    NOT NULL,
    MotorcycleParkingAmount  TEXT    NOT NULL,
    CarParkingAmount         TEXT    NOT NULL,
    CableTvAmount            TEXT    NOT NULL,
    TotalAmount              TEXT    NOT NULL,
    IsCableTvExempt          INTEGER NOT NULL CHECK (IsCableTvExempt IN (0, 1)),

    -- มิเตอร์และอัตราตอนออกบิล มีครบทุกค่าหรือไม่มีเลย
    PreviousWaterMeter       TEXT    NULL,
    CurrentWaterMeter        TEXT    NULL,
    PreviousElectricityMeter TEXT    NULL,
    CurrentElectricityMeter  TEXT    NULL,
    WaterRate                TEXT    NULL,
    ElectricityRate          TEXT    NULL,

    -- ผู้เช่าตอนออกบิล เก็บชื่อไว้ เพื่อให้บิลเก่าไม่เปลี่ยนเมื่อแก้ชื่อภายหลัง
    TenantId                 TEXT    NULL REFERENCES Tenants (Id),
    TenantFullName           TEXT    NULL,

    PaidOn                   TEXT    NULL,
    PaymentMethod            TEXT    NULL CHECK (PaymentMethod IN ('Cash', 'BankTransfer')),

    UNIQUE (RoomId, BillingPeriod),
    UNIQUE (BillingPeriod, InvoiceNumber),

    CHECK ((TenantId IS NULL) = (TenantFullName IS NULL)),
    CHECK ((PaidOn IS NULL) = (PaymentMethod IS NULL)),
    CHECK (
        (PreviousWaterMeter IS NULL) = (CurrentWaterMeter IS NULL)
        AND (PreviousWaterMeter IS NULL) = (PreviousElectricityMeter IS NULL)
        AND (PreviousWaterMeter IS NULL) = (CurrentElectricityMeter IS NULL)
        AND (PreviousWaterMeter IS NULL) = (WaterRate IS NULL)
        AND (PreviousWaterMeter IS NULL) = (ElectricityRate IS NULL)
    )
) STRICT;

-- มีได้แถวเดียว ถ้ายังไม่มีแถว แอปจะใช้ค่าเริ่มต้น
CREATE TABLE BillingSettings (
    Id                    INTEGER NOT NULL PRIMARY KEY CHECK (Id = 1),
    WaterRate             TEXT    NOT NULL,
    ElectricityRate       TEXT    NOT NULL,
    MotorcycleParkingRate TEXT    NOT NULL,
    CarParkingRate        TEXT    NOT NULL,
    CableTvRate           TEXT    NOT NULL
) STRICT;

CREATE TABLE InvoiceSettings (
    Id                 INTEGER NOT NULL PRIMARY KEY CHECK (Id = 1),
    PropertyName       TEXT    NOT NULL,
    Address            TEXT    NOT NULL,
    BankAccountDetails TEXT    NOT NULL
) STRICT;

PRAGMA user_version = 1;
