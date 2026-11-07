# RentFlowMini

โปรแกรมจัดการห้องพักขนาดเล็ก พัฒนาด้วย C# WinForms, .NET 10 และ SQLite

## ภาพรวมโปรเจกต์

RentFlowMini เป็นโปรเจกต์สำหรับฝึกพัฒนาซอฟต์แวร์ โดยเน้นการเขียนโปรแกรมเชิงวัตถุ (Object-Oriented Programming: OOP), Clean Architecture และการพัฒนาแบบทดสอบนำ (Test-Driven Development: TDD)

ปัจจุบันโปรแกรมรองรับการจัดการห้องพักและผู้เช่า ตั้งค่าราคากลาง บันทึกมิเตอร์น้ำ-ไฟรายเดือน และสร้างบิลประจำเดือน ดูรายละเอียดล่าสุดที่ [docs/current-code-status.md](docs/current-code-status.md)

## เทคโนโลยีที่ใช้

- C#
- .NET 10
- Windows Forms (WinForms)
- SQLite (`Microsoft.Data.Sqlite`)
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
Infrastructure (SQLite / In-Memory)
```

### ส่วนประกอบหลัก

- **Domain** — โมเดล `Room`, `Tenant`, `Bill`, `MonthlyMeterReading`, `BillingSettings`, `BillingCalculator` และ Value Objects เช่น `RoomNumber`, `Money`, `MeterReading`, `BillingPeriod`
- **Application** — Use Case ของห้องพัก ผู้เช่า มิเตอร์ ตั้งค่าราคา และสร้างบิล พร้อมสัญญา repository
- **Infrastructure** — SQLite Repository ที่แอปใช้จริงสำหรับทุกส่วน และ In-Memory Repository ที่ใช้ในเทสต์
- **UI** — WinForms แบ่งเป็นแท็บ: ห้องพัก, บันทึกมิเตอร์, สร้างบิลประจำเดือน, ผู้เช่า และตั้งค่าพื้นฐาน
- **Tests** — Unit Tests, UI Tests และ Integration Tests สำหรับ SQLite ซึ่งใช้ไฟล์ในโฟลเดอร์ชั่วคราว ไม่ต้องติดตั้งฐานข้อมูล

## ความสามารถในปัจจุบัน

- เพิ่มห้องพักพร้อมค่าเช่ารายเดือนและสถานะยกเว้น Cable TV, แก้ไขค่าเช่า และป้องกันเลขห้องซ้ำ
- เพิ่มผู้เช่าและผูกกับห้อง (หนึ่งห้องต่อผู้เช่าหนึ่งราย)
- ตั้งค่าราคากลาง: ค่าน้ำ ค่าไฟ ค่าจอดมอเตอร์ไซค์ ค่าจอดรถยนต์ และค่า Cable TV
- บันทึกมิเตอร์น้ำ-ไฟรายเดือน โดยดึงค่าก่อนหน้าจากรอบก่อนอัตโนมัติ และแก้ไขย้อนได้
- สร้างบิลประจำเดือน 6 รายการ (ค่าเช่า, น้ำ, ไฟ, จอดมอเตอร์ไซค์, จอดรถยนต์, Cable TV) พร้อมเลขใบแจ้งหนี้และป้องกันบิลซ้ำ
- เลือกรอบบิลเป็นเดือนภาษาไทยและปี พ.ศ.
- เก็บข้อมูลถาวรใน SQLite ปิดแล้วเปิดใหม่ข้อมูลอยู่ครบ

## สิ่งที่ต้องเตรียมก่อนใช้งาน

- ระบบปฏิบัติการ Windows
- .NET 10 SDK

## ฐานข้อมูล

ไม่ต้องติดตั้งฐานข้อมูลเพิ่ม เมื่อเปิดโปรแกรมครั้งแรก ระบบจะสร้างไฟล์ `%LocalAppData%\RentFlow\rentflow.db` และสร้างตารางให้เอง ถ้าเคยมีไฟล์ `settings.json` ในโฟลเดอร์เดียวกัน ระบบจะนำการตั้งค่าเข้าฐานข้อมูลครั้งเดียวโดยไม่แก้ไฟล์เดิม แบบตารางอยู่ใน [แผนเปลี่ยนเป็น SQLite](docs/sqlite-migration-plan.md)

> ยังไม่มีระบบสำรองข้อมูล จึงยังไม่ควรใส่ข้อมูลจริง ห้ามวางไฟล์ฐานข้อมูลในโฟลเดอร์ซิงก์คลาวด์หรือโฟลเดอร์แชร์ระหว่างใช้งาน และไฟล์นี้ไม่ได้เข้ารหัส

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

ไม่ต้องตั้งค่าฐานข้อมูลก่อนรันทดสอบ

## สถานะของโปรเจกต์

ฟีเจอร์หลักของรอบบิลรายเดือนทำงานได้ครบและเก็บข้อมูลใน SQLite (ผ่าน automated tests 427 รายการ) ดูลำดับงานถัดไปที่ [docs/roadmap.md](docs/roadmap.md)

ฟีเจอร์ที่วางแผนจะพัฒนาเพิ่มเติม ได้แก่

- ระบบสำรองและกู้คืนข้อมูล
- การแก้ไขและลบข้อมูลห้องพักและผู้เช่า
- ค่าตั้งต้นการจอดรถรายห้อง
- การแก้ไข ยกเลิก และพิมพ์ใบแจ้งหนี้

## สัญญาอนุญาต (License)

ปัจจุบันโปรเจกต์นี้จัดทำขึ้นเพื่อการเรียนรู้และพัฒนา และยังไม่ได้กำหนด License อย่างเป็นทางการ
