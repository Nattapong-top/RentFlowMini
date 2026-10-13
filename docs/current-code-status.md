# Current Code Status

เอกสารนี้สรุปสถานะโค้ดปัจจุบันของ RentFlowMini จาก repository, การ build, test ที่ป๋ายืนยัน และ GitHub Actions

**วันที่สรุป:** 2026-10-13
**สถานะโดยรวม:** ฟีเจอร์จัดการห้องพักพื้นฐานพร้อมใช้งานในระดับโปรเจกต์ฝึกพัฒนา และมี CI สำหรับ build/test กับ SQL Server

## 1. โครงสร้างโปรเจกต์

```text
RentFlowMini/
├── RentFlowMini/RentFlowApp/
│   ├── Domain/
│   │   └── ValueObjects/
│   │       ├── RoomId.cs
│   │       └── RoomNumber.cs
│   ├── Application/Rooms/
│   │   ├── CreateRoomService.cs
│   │   └── IRoomRepository.cs
│   ├── Infrastructure/Rooms/
│   │   ├── InMemoryRoomRepository/
│   │   └── SqlServerRoomRepository.cs
│   ├── UI/RoomForm.cs
│   ├── Database/001_CreateRooms.sql
│   ├── Program.cs
│   └── RentFlowApp.csproj
├── RentFlowApp.Tests/
│   ├── Domain tests
│   ├── Application tests
│   ├── In-Memory Repository tests
│   └── SQL Server integration tests
├── .github/workflows/dotnet.yml
└── README.md
```

## 2. ฟังก์ชันที่มีในปัจจุบัน

- Domain billing MVP มี `BillingCalculator`, `Bill`, `BillingPeriod` และ `Money`
- Test ล่าสุดผ่านทั้งหมด 41 รายการด้วย `dotnet test`

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
WinForms RoomForm
    ↓
CreateRoomService
    ↓
IRoomRepository
    ↓
SqlServerRoomRepository หรือ InMemoryRoomRepository
    ↓
SQL Server / memory
```

เมื่อเพิ่มห้อง ระบบจะสร้าง `RoomNumber`, ตรวจสอบเลขซ้ำ, สร้าง `RoomId`, บันทึกผ่าน repository และ refresh รายการบนหน้าจอ

## 4. Database

ตารางหลักคือ `dbo.Rooms` ประกอบด้วย:

- `Id UNIQUEIDENTIFIER` เป็น primary key
- `RoomNumber NVARCHAR(50)` เป็นข้อมูลบังคับและมี unique constraint

SQL Server Repository ใช้ parameter ที่ระบุชนิดและขนาดตรงกับ schema แล้ว

## 5. การทดสอบและ Build

- มี automated tests ทั้งหมด 41 รายการ
- ป๋ายืนยันว่ารัน test ผ่านครบ 41 รายการด้วยคำสั่ง `dotnet test`
- Release build ของ App ผ่าน
- Release build ของ Test project ผ่าน
- มี unit tests สำหรับ Domain, Application และ In-Memory Repository
- มี integration tests สำหรับ SQL Server connection และ repository

## 6. GitHub Actions

Workflow อยู่ที่ `.github/workflows/dotnet.yml` และทำงานตามลำดับดังนี้:

1. Checkout repository
2. ติดตั้ง .NET 10
3. ติดตั้ง SQL Server บน runner
4. สร้างฐานข้อมูล `RentFlowDb`
5. สร้างตารางจาก `001_CreateRooms.sql`
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

## 8. สิ่งที่ยังไม่อยู่ในขอบเขตปัจจุบัน

- UC-01 รุ่น MVP จะเริ่มจากค่าเช่า ค่าน้ำ และค่าไฟเท่านั้น ดูรายละเอียดที่ [UC-01: สร้างบิลประจำเดือน](uc-01-create-monthly-bill.md)
- การจัดการผู้เช่า
- การแก้ไขและลบห้องพัก
- การจัดการมิเตอร์น้ำและไฟ
- การคำนวณบิลรายเดือน
- Use Case สำหรับสร้างบิลและการเชื่อมกับ UI
- การสร้าง `BillId` และการบันทึก Bill ผ่าน Repository
- การยืนยันผล GitHub Actions รอบล่าสุดจนจบทุกขั้นตอน

## 9. ความเสี่ยงและข้อจำกัด

- Integration tests ต้องใช้ SQL Server และฐานข้อมูลที่เตรียมไว้
- CI ใช้เวลานานจากการติดตั้ง SQL Server ใหม่บน runner ทุกครั้ง
- Credential เดิมเคยอยู่ใน Git history และถูกเปลี่ยนแล้ว; workflow ปัจจุบันใช้ GitHub Secret แทน
- ยังไม่มี license อย่างเป็นทางการ
