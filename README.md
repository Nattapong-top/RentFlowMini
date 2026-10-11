# RentFlowMini

โปรแกรมจัดการห้องพักขนาดเล็ก พัฒนาด้วย C# WinForms, .NET 10 และ SQL Server

## ภาพรวมโปรเจกต์

RentFlowMini เป็นโปรเจกต์สำหรับฝึกพัฒนาซอฟต์แวร์ โดยเน้นการเขียนโปรแกรมเชิงวัตถุ (Object-Oriented Programming: OOP), Clean Architecture และการพัฒนาแบบทดสอบนำ (Test-Driven Development: TDD)

ปัจจุบันโปรแกรมรองรับการเพิ่มห้องพัก แสดงรายการห้อง และป้องกันการใช้เลขห้องซ้ำ

## เทคโนโลยีที่ใช้

- C#
- .NET 10
- Windows Forms (WinForms)
- SQL Server 2025 Express
- ADO.NET (`Microsoft.Data.SqlClient`)
- xUnit สำหรับทดสอบโปรแกรม

## สถาปัตยกรรมระบบ

```text
WinForms UI
    |
    v
Application
(CreateRoomService)
    |
    v
IRoomRepository
    |
    v
Infrastructure
(SqlServerRoomRepository)
    |
    v
SQL Server
```

### ส่วนประกอบหลัก

- **Domain** — จัดการโมเดล `Room`, `RoomId` และ `RoomNumber`
- **Application** — จัดการ Use Case ผ่าน `CreateRoomService` และกำหนดสัญญาการเข้าถึงข้อมูลผ่าน `IRoomRepository`
- **Infrastructure** — จัดการการจัดเก็บข้อมูลผ่าน In-Memory Repository และ SQL Server Repository
- **UI** — ส่วนติดต่อผู้ใช้ด้วย WinForms สำหรับจัดการห้องพัก
- **Tests** — ชุดทดสอบ Unit Tests และ Integration Tests สำหรับ SQL Server

## ความสามารถในปัจจุบัน

- เพิ่มห้องพัก
- แสดงรายการห้องพัก
- ตรวจสอบความถูกต้องของเลขห้อง
- ป้องกันเลขห้องซ้ำ
- บันทึกข้อมูลห้องพักลง SQL Server
- ทดสอบการทำงานของ Repository ด้วย xUnit

## สิ่งที่ต้องเตรียมก่อนใช้งาน

- ระบบปฏิบัติการ Windows
- .NET 10 SDK
- SQL Server
- SQL Server Management Studio (SSMS)

## การตั้งค่าฐานข้อมูล

สร้างฐานข้อมูลชื่อ `RentFlowDb` และตาราง `dbo.Rooms` ด้วยคำสั่ง SQL ต่อไปนี้

```sql
CREATE DATABASE RentFlowDb;
GO

USE RentFlowDb;
GO

CREATE TABLE dbo.Rooms
(
    Id UNIQUEIDENTIFIER NOT NULL,
    RoomNumber NVARCHAR(50) NOT NULL,

    CONSTRAINT PK_Rooms PRIMARY KEY (Id),
    CONSTRAINT UQ_Rooms_RoomNumber UNIQUE (RoomNumber)
);
GO
```

ก่อนเปิดใช้งานโปรแกรม ให้ตั้งค่า Connection String ให้ตรงกับ SQL Server ในเครื่องของคุณ

## วิธีเปิดใช้งานโปรแกรม

เปิด PowerShell ที่ Root ของ Repository แล้วรันคำสั่ง:

```powershell
dotnet run --project .\RentFlowMini\RentFlowApp\RentFlowApp.csproj
```

หากไฟล์ `.csproj` อยู่ในตำแหน่งอื่น ให้ปรับเส้นทางให้ตรงกับโครงสร้างโปรเจกต์จริง

## วิธีรันทดสอบ

รันชุดทดสอบทั้งหมดด้วยคำสั่ง:

```powershell
dotnet test
```

สำหรับ SQL Server Integration Tests ต้องมี SQL Server ที่เชื่อมต่อได้ และต้องตั้งค่าฐานข้อมูลสำหรับการทดสอบให้ถูกต้อง

## สถานะของโปรเจกต์

ปัจจุบันโปรเจกต์มุ่งเน้นการจัดการห้องพักและการจัดเก็บข้อมูลด้วย SQL Server

ฟีเจอร์ที่วางแผนจะพัฒนาเพิ่มเติม ได้แก่

- การจัดการผู้เช่า
- การแก้ไขและลบข้อมูลห้องพัก
- การจัดการมิเตอร์น้ำและไฟ
- การคำนวณและจัดทำบิลรายเดือน

## สัญญาอนุญาต (License)

ปัจจุบันโปรเจกต์นี้จัดทำขึ้นเพื่อการเรียนรู้และพัฒนา และยังไม่ได้กำหนด License อย่างเป็นทางการ