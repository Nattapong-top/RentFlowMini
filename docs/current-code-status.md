# Current Code Status

เอกสารนี้สรุปสถานะโค้ดปัจจุบันของ RentFlowMini จาก repository, การ build, test ที่ป๋ายืนยัน และ GitHub Actions

**วันที่สรุป:** 2026-10-13
**สถานะโดยรวม:** ฟีเจอร์จัดการห้องพักและสร้างบิลประจำเดือน (UC-01 MVP) พร้อมใช้งานครบวงจร ทั้ง Domain, Application, SQL Server Persistence, WinForms UI และ CI พร้อม automated tests 54 รายการ

## 1. โครงสร้างโปรเจกต์

```text
RentFlowMini/
├── RentFlowMini/RentFlowApp/
│   ├── Domain/
│   │   ├── Billing/
│   │   │   ├── Bill.cs
│   │   │   ├── BillingCalculator.cs
│   │   │   └── InvalidMeterReadingException.cs
│   │   └── ValueObjects/
│   │       ├── BillId.cs
│   │       ├── BillingPeriod.cs
│   │       ├── MeterReading.cs
│   │       ├── Money.cs
│   │       ├── RoomId.cs
│   │       └── RoomNumber.cs
│   ├── Application/
│   │   ├── Billing/
│   │   │   ├── CreateMonthlyBillRequest.cs
│   │   │   ├── CreateMonthlyBillService.cs
│   │   │   └── IBillRepository.cs
│   │   └── Rooms/
│   │       ├── CreateRoomService.cs
│   │       └── IRoomRepository.cs
│   ├── Infrastructure/
│   │   ├── Billing/
│   │   │   ├── InMemoryBillRepository.cs
│   │   │   └── SqlServerBillRepository.cs
│   │   └── Rooms/
│   │       ├── InMemoryRoomRepository/
│   │       │   └── InMemoryRoomRepository.cs
│   │       └── SqlServerRoomRepository.cs
│   ├── Database/
│   │   ├── 001_CreateRooms.sql
│   │   └── 002_CreateBills.sql
│   ├── UI/
│   │   ├── RoomForm.cs
│   │   └── RoomForm.resx
│   ├── Room.cs
│   ├── Program.cs
│   └── RentFlowApp.csproj
├── RentFlowApp.Tests/
│   ├── Domain/
│   │   ├── Billing/
│   │   │   ├── BillTests.cs
│   │   │   ├── BillingCalculatorTests.cs
│   │   │   └── BillingPeriodTests.cs
│   │   └── ValueObjects/
│   │       ├── MeterReadingTests.cs
│   │       └── MoneyTests.cs
│   ├── Application/
│   │   ├── Billing/
│   │   │   └── CreateMonthlyBillServiceTests.cs
│   │   └── Rooms/
│   │       └── CreateRoomServiceTests.cs
│   ├── Infrastructure/
│   │   └── Billing/
│   │       ├── InMemoryBillRepositoryTests.cs
│   │       └── SqlServerBillRepositoryTests.cs
│   ├── Infrastucture/
│   │   └── Rooms/
│   │       ├── InMemoryRoomRepositoryTests.cs
│   │       ├── SqlServerConnectionTests.cs
│   │       └── SqlServerRoomRepositoryTests.cs
│   ├── UI/
│   │   └── RoomFormTests.cs
│   ├── RoomIdTests.cs
│   ├── RoomNumberTests.cs
│   ├── RoomTests.cs
│   └── RentFlowApp.Tests.csproj
├── .github/workflows/dotnet.yml
├── docs/
│   ├── current-code-status.md
│   ├── implementation-plan.md
│   └── uc-01-create-monthly-bill.md
└── README.md
```

## 2. ฟังก์ชันที่มีในปัจจุบัน

- Domain billing MVP มี `BillingCalculator`, `Bill`, `BillingPeriod` และ `Money`
- มี `CreateMonthlyBillRequest` และ `CreateMonthlyBillService` สำหรับสร้าง Bill จาก Request
- ใช้ `MeterReading` Value Object กับมิเตอร์น้ำและมิเตอร์ไฟ
- มี `BillId`, `IBillRepository`, `InMemoryBillRepository` และ `SqlServerBillRepository`
- มี SQL script `002_CreateBills.sql` สำหรับตาราง `dbo.Bills`
- หน้าจอ WinForms `RoomForm` มีแท็บ "ห้องพัก" และแท็บ "สร้างบิลประจำเดือน" (UC-01) เชื่อมต่อ Use Case
- Test ล่าสุดผ่านทั้งหมด 54 รายการด้วย `dotnet test`
 
- เพิ่มห้องพักผ่าน `CreateRoomService`
- ตรวจสอบและ normalize เลขห้องด้วย `RoomNumber`
- สร้างและตรวจสอบ `RoomId` ที่ไม่เป็นค่า GUID ว่าง
- ป้องกันเลขห้องซ้ำทั้งระดับ service, In-Memory Repository และ SQL Server
- แสดงรายการห้องพักใน WinForms
- จัดเก็บข้อมูลใน SQL Server ผ่าน ADO.NET
- รองรับการใช้ Connection String จาก environment variable `RENTFLOW_CONNECTION_STRING`
- แสดงข้อความที่ผู้ใช้เข้าใจได้เมื่อโหลดข้อมูลจาก SQL Server ไม่สำเร็จ
 
## 3. Flow การทำงานหลัก
 
```text
[จัดการห้องพัก]
WinForms RoomForm (แท็บห้องพัก)
    ↓
CreateRoomService
    ↓
IRoomRepository → SqlServerRoomRepository / InMemoryRoomRepository → SQL Server / memory

[สร้างบิลประจำเดือน]
WinForms RoomForm (แท็บสร้างบิล)
    ↓
CreateMonthlyBillService
    ↓
BillingCalculator → Domain Models (Bill, Money, BillingPeriod)
    ↓
IBillRepository → SqlServerBillRepository / InMemoryBillRepository → SQL Server / memory
```
 
เมื่อเพิ่มห้อง ระบบจะตรวจสอบเลขซ้ำ บันทึก และ refresh รายการ
เมื่อสร้างบิล ระบบจะอ่านห้อง รอบบิล ค่าเช่า และมิเตอร์ คำนวณยอด และบันทึกผ่าน repository พร้อมแสดงผลลัพธ์และข้อความสถานะภาษาไทย

## 4. Database
 
ตารางหลักประกอบด้วย:

1. `dbo.Rooms`:
   - `Id UNIQUEIDENTIFIER` เป็น primary key
   - `RoomNumber NVARCHAR(50)` เป็นข้อมูลบังคับและมี unique constraint
2. `dbo.Bills`:
   - `Id UNIQUEIDENTIFIER` เป็น primary key
   - `RoomId UNIQUEIDENTIFIER` เป็น foreign key อ้างอิง `dbo.Rooms(Id)`
   - `BillingPeriod NVARCHAR(7)` รูปแบบ `YYYY-MM`
   - `RentAmount DECIMAL(18, 2)`
   - `WaterAmount DECIMAL(18, 2)`
   - `ElectricityAmount DECIMAL(18, 2)`
   - `TotalAmount DECIMAL(18, 2)`
   - Unique constraint บน `(RoomId, BillingPeriod)` ป้องกันบิลซ้ำ
 
SQL Server Repository ใช้ parameter ที่ระบุชนิดและขนาดตรงกับ schema แล้ว
 
## 5. การทดสอบและ Build
 
- มี automated tests ทั้งหมด 54 รายการ
- รัน test ผ่านครบ 54 รายการด้วยคำสั่ง `dotnet test`
- Release build ของ App ผ่าน
- Release build ของ Test project ผ่าน
- มี unit tests สำหรับ Domain, Application, In-Memory Repository และ UI
- มี integration tests สำหรับ SQL Server connection และ repository (Rooms และ Bills)
 
## 6. GitHub Actions
 
Workflow อยู่ที่ `.github/workflows/dotnet.yml` และทำงานตามลำดับดังนี้:
 
1. Checkout repository
2. ติดตั้ง .NET 10
3. ติดตั้ง SQL Server บน runner
4. สร้างฐานข้อมูล `RentFlowDb`
5. สร้างตารางจาก `001_CreateRooms.sql` และ `002_CreateBills.sql`
6. Restore
7. Build แบบ Release
8. Run tests
 
รหัสผ่าน SQL Server ของ CI ถูกอ่านจาก GitHub Actions Secret ชื่อ `RENTFLOWMINI_SECRET` และไม่เก็บค่าไว้ใน workflow
 
ข้อสังเกต: GitHub-hosted runner เป็นเครื่องชั่วคราว จึงต้องติดตั้ง SQL Server ใหม่ทุกครั้ง ทำให้ขั้นตอน CI ใช้เวลาหลายนาที
 
## 7. สถานะการแก้ไขล่าสุด
 
- แก้ path ของ SQL script ใน CI
- จัดการกรณี SQL Server เชื่อมต่อไม่ได้ตอนเปิดหน้าจอ
- รองรับการ override Connection String ผ่าน environment variable
- แยก test dependencies ออกจาก App project
- เปลี่ยน SQL parameters ให้ระบุชนิดตรงกับ schema
- เปลี่ยน CI ให้ใช้ GitHub Secret แทน credential ที่ฝังใน workflow
- เพิ่ม `BillingCalculator`, `Bill`, `BillingPeriod` และ `Money` สำหรับ Domain ของ UC-01 MVP
- แยก Test ของ Billing, BillingPeriod และ Money ตามความรับผิดชอบ
- เพิ่ม `CreateMonthlyBillService` และ `CreateMonthlyBillRequest`
- เพิ่ม `MeterReading` Value Object และ refactor BillingCalculator ให้รับ MeterReading
- เพิ่ม `BillId`, `IBillRepository` และ `InMemoryBillRepository`
- เพิ่ม `002_CreateBills.sql` สร้างตาราง `dbo.Bills` พร้อม Unique Constraint
- เพิ่ม `SqlServerBillRepository` และ Integration Tests (ผ่านครบ 50 tests)
- ปรับปรุง `RoomForm` เพิ่มแท็บสร้างบิลประจำเดือน (UC-01) พร้อมอัตราค่าน้ำ 19 บาท และค่าไฟ 8 บาท
- เพิ่ม UI Unit Tests สำหรับ RoomForm flow (ผ่านครบ 54 tests)
- อัปเดต `Program.cs` เพื่อเชื่อมต่อ Use Case กับ Form จริง
- ตรวจสอบความถูกต้องของ CI workflow และยืนยัน Release Build / Tests ผ่านครบ 54 รายการ (Phase 6)
 
## 8. สิ่งที่ยังไม่อยู่ในขอบเขตปัจจุบัน
 
- UC-01 รุ่น MVP จะเริ่มจากค่าเช่า ค่าน้ำ และค่าไฟเท่านั้น ดูรายละเอียดที่ [UC-01: สร้างบิลประจำเดือน](uc-01-create-monthly-bill.md)
- การจัดการผู้เช่า
- การแก้ไขและลบห้องพัก
- การจัดการมิเตอร์น้ำและไฟ
- การ Recalculate บิลเดิม

## 9. ความเสี่ยงและข้อจำกัด

- Integration tests ต้องใช้ SQL Server และฐานข้อมูลที่เตรียมไว้
- CI ใช้เวลานานจากการติดตั้ง SQL Server ใหม่บน runner ทุกครั้ง
- Credential เดิมเคยอยู่ใน Git history และถูกเปลี่ยนแล้ว; workflow ปัจจุบันใช้ GitHub Secret แทน
- ยังไม่มี license อย่างเป็นทางการ
