# RentFlowMini

โปรแกรมจัดการห้องพักขนาดเล็ก พัฒนาด้วย C# WinForms, .NET 10 และ SQL Server

## ภาพรวมโปรเจกต์

RentFlowMini เป็นโปรเจกต์สำหรับฝึกพัฒนาซอฟต์แวร์ โดยเน้นการเขียนโปรแกรมเชิงวัตถุ (Object-Oriented Programming: OOP), Clean Architecture และการพัฒนาแบบทดสอบนำ (Test-Driven Development: TDD)

ปัจจุบันโปรแกรมรองรับการจัดการห้องพักและผู้เช่า ตั้งค่าราคากลาง บันทึกมิเตอร์น้ำ-ไฟรายเดือน และสร้างบิลประจำเดือน ดูรายละเอียดล่าสุดที่ [docs/current-code-status.md](docs/current-code-status.md)

## เทคโนโลยีที่ใช้

- C#
- .NET 10
- Windows Forms (WinForms)
- SQL Server 2025 Express
- ADO.NET (`Microsoft.Data.SqlClient`)
- xUnit สำหรับทดสอบโปรแกรม

## สถาปัตยกรรมระบบ

```text
WinForms UI (RoomForm + Tab Controls)
    |
    v
Application Services
(CreateRoomService, UpdateRoomRentService, CreateTenantService,
 SaveMonthlyMeterReadingService, UpdateBillingSettingsService,
 CreateMonthlyBillService)
    |
    v
Domain (Room, Tenant, Bill, BillingCalculator, MonthlyMeterReading,
        BillingSettings, Value Objects)
    |
    v
Repository Interfaces
    |
    v
Infrastructure (In-Memory / SQL Server)
```

### ส่วนประกอบหลัก

- **Domain** — โมเดล `Room`, `Tenant`, `Bill`, `MonthlyMeterReading`, `BillingSettings`, `BillingCalculator` และ Value Objects เช่น `RoomNumber`, `Money`, `MeterReading`, `BillingPeriod`
- **Application** — Use Case ของห้องพัก ผู้เช่า มิเตอร์ ตั้งค่าราคา และสร้างบิล พร้อมสัญญา repository
- **Infrastructure** — In-Memory Repository สำหรับทุกส่วน และ SQL Server Repository สำหรับห้องพัก ผู้เช่า และบิล
- **UI** — WinForms แบ่งเป็นแท็บ: ห้องพัก, บันทึกมิเตอร์, สร้างบิลประจำเดือน, ผู้เช่า และตั้งค่าพื้นฐาน
- **Tests** — Unit Tests, UI Tests และ Integration Tests สำหรับ SQL Server

## ความสามารถในปัจจุบัน

- เพิ่มห้องพักพร้อมค่าเช่ารายเดือนและสถานะยกเว้น Cable TV, แก้ไขค่าเช่า และป้องกันเลขห้องซ้ำ
- เพิ่มผู้เช่าและผูกกับห้อง (หนึ่งห้องต่อผู้เช่าหนึ่งราย)
- ตั้งค่าราคากลาง: ค่าน้ำ ค่าไฟ ค่าจอดมอเตอร์ไซค์ ค่าจอดรถยนต์ และค่า Cable TV
- บันทึกมิเตอร์น้ำ-ไฟรายเดือน โดยดึงค่าก่อนหน้าจากรอบก่อนอัตโนมัติ และแก้ไขย้อนได้
- สร้างบิลประจำเดือน 6 รายการ (ค่าเช่า, น้ำ, ไฟ, จอดมอเตอร์ไซค์, จอดรถยนต์, Cable TV) พร้อมเลขใบแจ้งหนี้และป้องกันบิลซ้ำ
- เลือกรอบบิลเป็นเดือนภาษาไทยและปี พ.ศ.
- มีข้อมูลตัวอย่าง (10 ห้อง พร้อมผู้เช่าและมิเตอร์) เมื่อเปิดโปรแกรมครั้งแรก

> หมายเหตุ: แอปปัจจุบันใช้ In-Memory Repository ทั้งหมด ข้อมูลจะหายเมื่อปิดโปรแกรม SQL Server Repository มีอยู่และผ่าน integration tests แต่ยังไม่ได้เชื่อมเข้ากับแอป

## สิ่งที่ต้องเตรียมก่อนใช้งาน

- ระบบปฏิบัติการ Windows
- .NET 10 SDK
- SQL Server
- SQL Server Management Studio (SSMS)

## การตั้งค่าฐานข้อมูล

ขั้นตอนนี้จำเป็นเฉพาะการรัน SQL Server Integration Tests เพราะแอปปัจจุบันยังใช้ In-Memory Repository

สร้างฐานข้อมูลชื่อ `RentFlowDb` แล้วรัน script ใน `RentFlowMini/RentFlowApp/Database/` ตามลำดับ:

1. `001_CreateRooms.sql` — ตาราง `dbo.Rooms`
2. `002_CreateBills.sql` — ตาราง `dbo.Bills`
3. `003_CreateTenants.sql` — ตาราง `dbo.Tenants`

ตัวอย่างด้วย `sqlcmd` (ปรับชื่อ server ให้ตรงกับเครื่องของคุณ):

```powershell
sqlcmd -S .\SQLEXPRESS -E -C -Q "IF DB_ID('RentFlowDb') IS NULL CREATE DATABASE RentFlowDb;"
sqlcmd -S .\SQLEXPRESS -E -C -d RentFlowDb -i .\RentFlowMini\RentFlowApp\Database\001_CreateRooms.sql
sqlcmd -S .\SQLEXPRESS -E -C -d RentFlowDb -i .\RentFlowMini\RentFlowApp\Database\002_CreateBills.sql
sqlcmd -S .\SQLEXPRESS -E -C -d RentFlowDb -i .\RentFlowMini\RentFlowApp\Database\003_CreateTenants.sql
```

Integration Tests อ่าน Connection String จาก environment variable ชื่อ `RENTFLOW_TEST_CONNECTION` หากไม่กำหนดจะใช้ `localhost\SQLEXPRESS` ฐานข้อมูล `RentFlowDb` แบบ Windows Authentication ห้ามใส่รหัสผ่านลงในโค้ดหรือเอกสาร

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

ฟีเจอร์หลักของรอบบิลรายเดือนทำงานได้ครบใน In-Memory (ผ่าน automated tests 144 รายการ) ดูลำดับงานถัดไปที่ [docs/roadmap.md](docs/roadmap.md)

ฟีเจอร์ที่วางแผนจะพัฒนาเพิ่มเติม ได้แก่

- เชื่อม SQL Server เข้ากับแอป (รวม schema ของบิล 6 รายการ, มิเตอร์ และตั้งค่าราคา)
- การแก้ไขและลบข้อมูลห้องพักและผู้เช่า
- ค่าตั้งต้นการจอดรถรายห้อง
- การแก้ไข ยกเลิก และพิมพ์ใบแจ้งหนี้

## สัญญาอนุญาต (License)

ปัจจุบันโปรเจกต์นี้จัดทำขึ้นเพื่อการเรียนรู้และพัฒนา และยังไม่ได้กำหนด License อย่างเป็นทางการ
