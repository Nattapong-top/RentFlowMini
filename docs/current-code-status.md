# Current Code Status

เอกสารนี้สรุปสถานะโค้ดปัจจุบันของ RentFlowMini จาก repository, การ build, ผลการรัน test และ GitHub Actions

**วันที่สรุป:** 2026-11-03
**อ้างอิง commit:** `bec330c`
**สถานะโดยรวม:** มีระบบห้องพัก ผู้เช่า ตั้งค่าราคากลาง บันทึกมิเตอร์รายเดือน และสร้างบิลประจำเดือนครบ 6 รายการ ทำงานได้ใน WinForms ด้วย In-Memory repository; automated tests ผ่าน 144 รายการ

## 1. โครงสร้างโปรเจกต์

```text
RentFlowMini/
├── RentFlowMini/RentFlowApp/
│   ├── Domain/
│   │   ├── Billing/
│   │   │   ├── Bill.cs
│   │   │   ├── BillingCalculator.cs
│   │   │   ├── BillingSettings.cs
│   │   │   ├── InvalidMeterReadingException.cs
│   │   │   └── MonthlyMeterReading.cs
│   │   ├── ValueObjects/
│   │   │   ├── BillId.cs
│   │   │   ├── BillingPeriod.cs
│   │   │   ├── MeterReading.cs
│   │   │   ├── Money.cs
│   │   │   ├── RoomId.cs
│   │   │   ├── RoomNumber.cs
│   │   │   └── TenantId.cs
│   │   └── Tenant.cs
│   ├── Application/
│   │   ├── Billing/
│   │   │   ├── CreateMonthlyBillRequest.cs
│   │   │   ├── CreateMonthlyBillService.cs
│   │   │   ├── IBillRepository.cs
│   │   │   ├── IBillingSettingsRepository.cs
│   │   │   ├── IMonthlyMeterReadingRepository.cs
│   │   │   ├── SaveMonthlyMeterReadingService.cs
│   │   │   └── UpdateBillingSettingsService.cs
│   │   ├── Rooms/
│   │   │   ├── CreateRoomService.cs
│   │   │   ├── IRoomEditor.cs
│   │   │   ├── IRoomRepository.cs
│   │   │   └── UpdateRoomRentService.cs
│   │   └── Tenants/
│   │       ├── CreateTenantService.cs
│   │       └── ITenantRepository.cs
│   ├── Infrastructure/
│   │   ├── Billing/
│   │   │   ├── InMemoryBillRepository.cs
│   │   │   ├── InMemoryBillingSettingsRepository.cs
│   │   │   ├── InMemoryMonthlyMeterReadingRepository.cs
│   │   │   └── SqlServerBillRepository.cs
│   │   ├── Rooms/
│   │   │   ├── InMemoryRoomRepository/InMemoryRoomRepository.cs
│   │   │   └── SqlServerRoomRepository.cs
│   │   └── Tenants/
│   │       ├── InMemoryTenantRepository.cs
│   │       └── SqlServerTenantRepository.cs
│   ├── Database/
│   │   ├── 001_CreateRooms.sql
│   │   ├── 002_CreateBills.sql
│   │   └── 003_CreateTenants.sql
│   ├── UI/
│   │   ├── RoomForm.cs                    (Form หลัก รวมแท็บทั้งหมด)
│   │   ├── BaseTabControl.cs              (ฐานร่วมของแท็บ)
│   │   ├── BillingPeriodSelector.cs       (เลือกเดือน + ปี พ.ศ.)
│   │   ├── RoomTabControl.cs
│   │   ├── MeterReadingTabControl.cs
│   │   ├── MonthlyBillTabControl.cs
│   │   ├── TenantTabControl.cs
│   │   └── BillingSettingsTabControl.cs
│   ├── Room.cs
│   ├── Program.cs
│   └── RentFlowApp.csproj
├── RentFlowApp.Tests/
│   ├── Domain/            (Bill, BillingCalculator, BillingPeriod, BillingSettings,
│   │                       MonthlyMeterReading, Tenant, MeterReading, Money, TenantId)
│   ├── Application/       (CreateMonthlyBill, SaveMonthlyMeterReading, UpdateBillingSettings,
│   │                       CreateRoom, UpdateRoomRent, CreateTenant)
│   ├── Infrastructure/    (Bill, BillingSettings, MonthlyMeterReading, Tenant repositories)
│   ├── Infrastucture/     (Room repositories และ SQL Server connection)
│   ├── UI/RoomFormTests.cs
│   ├── RoomIdTests.cs, RoomNumberTests.cs, RoomTests.cs
│   └── RentFlowApp.Tests.csproj
├── .github/workflows/dotnet.yml
├── docs/
└── README.md
```

## 2. ฟังก์ชันที่มีในปัจจุบัน

### ห้องพัก

- เพิ่มห้องพักผ่าน `CreateRoomService` พร้อมค่าเช่ารายเดือนและสถานะยกเว้น Cable TV (`IsCableTvExempt`)
- แก้ไขค่าเช่ารายเดือนผ่าน `UpdateRoomRentService` (ใช้ `IRoomEditor`)
- ตรวจสอบและ normalize เลขห้องด้วย `RoomNumber` และป้องกันเลขห้องซ้ำ

### ผู้เช่า

- `Tenant` มีชื่อ นามสกุล เบอร์โทร (ตัวเลข 9-10 หลัก ไม่บังคับ) ห้องที่ผูกไว้ วันเข้าอยู่ และเงินประกัน (ไม่บังคับ และบันทึกเฉพาะยอด ยังไม่มีการหักหรือคืน)
- เพิ่มผู้เช่าผ่าน `CreateTenantService` และแก้ไขด้วยการดับเบิลคลิกผ่าน `UpdateTenantService` ซึ่งย้ายไปห้องว่างได้ โดยบิลเก่ายังอยู่กับห้องเดิม
- ย้ายออกผ่าน `MoveOutTenantService`: ถามยืนยันก่อนและเตือนถ้ายังมียอดค้าง, คืนห้องทันที, เก็บวันที่และห้องเดิมเป็นประวัติ, ยกเลิกภายหลังไม่ได้ และแก้ไขข้อมูลต่อไม่ได้
- ตารางในแท็บ "ผู้เช่า (Tenants)" ซ่อนรหัสผู้เช่า เรียงตามห้อง ค้นหาจากชื่อ ห้อง หรือเบอร์ได้ แสดงยอดค้างชำระของแต่ละคน และเลือกแสดงผู้เช่าที่ย้ายออกแล้วได้
- `Bill.Tenant` เก็บรหัสและชื่อผู้เช่า ณ ตอนสร้างบิล ใบแจ้งหนี้และตารางบิลจึงไม่เปลี่ยนตามผู้เช่าคนใหม่หรือการแก้ชื่อภายหลัง และยอดค้างจะติดตามคนเดิมแม้ห้องมีผู้เช่าใหม่
- มี `InMemoryTenantRepository` และ `SqlServerTenantRepository` (หนึ่งห้องมีผู้เช่าได้ไม่เกินหนึ่งราย); `dbo.Tenants` ยังไม่มีคอลัมน์วันเข้าอยู่ เงินประกัน และการย้ายออก

### ตั้งค่าราคากลาง

- `BillingSettings` เก็บอัตราค่าน้ำ ค่าไฟ ค่าจอดมอเตอร์ไซค์ ค่าจอดรถยนต์ และค่า Cable TV
- แก้ไขผ่าน `UpdateBillingSettingsService` และแท็บ "ตั้งค่าพื้นฐาน" โดยราคาที่แก้มีผลกับบิลที่สร้างหลังจากนั้น ส่วนบิลที่ออกแล้วไม่เปลี่ยน
- แอปที่รันจริงบันทึกราคากลางและข้อมูลหอพักด้วย `JsonFileSettingsRepository` ลง `%LocalAppData%\RentFlow\settings.json` ซึ่งอยู่นอก repo; ถ้าอ่านไฟล์ไม่ได้จะแจ้งเตือน ใช้ค่าเริ่มต้นชั่วคราว และสำรองไฟล์เดิมเป็น `.bad` ก่อนบันทึกทับ
- ค่าเริ่มต้นใน `InMemoryBillingSettingsRepository`: น้ำ 19, ไฟ 8, มอเตอร์ไซค์ 100, รถยนต์ 500, Cable TV 60 บาท

### บันทึกมิเตอร์รายเดือน

- `MonthlyMeterReading` เก็บมิเตอร์น้ำและไฟ (ก่อนหน้า/ปัจจุบัน) ต่อห้องต่อรอบบิล และตรวจว่าค่าปัจจุบันไม่น้อยกว่าค่าก่อนหน้า
- `SaveMonthlyMeterReadingService` บันทึกแบบ upsert (มีอยู่แล้วจะ update)
- แท็บ "บันทึกมิเตอร์น้ำประปา-ไฟฟ้า": ซ่อนห้องที่จดแล้วในรอบนั้น, ตารางกรองตามรอบบิล, แสดงหน่วยที่ใช้, ดับเบิลคลิกเพื่อแก้ไข, กด Enter เพื่อบันทึก, ค่าก่อนหน้าเป็น read-only และดึงจากรอบก่อนหน้าอัตโนมัติ

### สร้างบิลประจำเดือน

- `BillingCalculator` คำนวณ 6 รายการ: ค่าเช่า, ค่าน้ำ, ค่าไฟ, ค่าจอดมอเตอร์ไซค์, ค่าจอดรถยนต์, ค่า Cable TV และยอดรวม
  - ค่าจอดรถคิดเมื่อเลือก checkbox ในรอบบิลนั้น
  - ค่า Cable TV เป็น 0 เมื่อยกเว้น
- `Bill` เก็บ snapshot ยอดทั้ง 6 รายการ พร้อมค่ามิเตอร์และอัตราน้ำ-ไฟที่ใช้คำนวณ รวมทั้ง `InvoiceNumber`, `CreatedAt` และ `DueDate` (วันที่ 5 ของเดือนถัดจากรอบบิล)
- `CreateMonthlyBillService` สร้างเลขใบแจ้งหนี้รูปแบบ `N001`, `N002`, ... โดยเริ่มนับใหม่ทุกรอบบิล
- ป้องกันบิลซ้ำของห้องและรอบเดียวกันที่ repository
- แท็บ "สร้างบิลประจำเดือน":
  - เลือกรอบบิลด้วย `BillingPeriodSelector` (เดือนภาษาไทย + ปี พ.ศ.)
  - ซ่อนห้องที่สร้างบิลแล้วในรอบนั้น และเลื่อนไปห้องถัดไปอัตโนมัติหลังสร้างบิล
  - โหลดค่าเช่าและสถานะยกเว้น Cable TV จากห้อง และโหลดอัตราล่าสุดจาก settings repository เดียวกับแท็บตั้งค่าพื้นฐาน
  - โหลดมิเตอร์ของรอบนั้นอัตโนมัติ; ถ้ายังไม่ได้จด จะเติมค่าก่อนหน้าจากรอบก่อนและแสดงคำเตือนให้บันทึกมิเตอร์ก่อน
  - แสดงสรุปบิล (ห้อง, ผู้เช่า, เลขใบแจ้งหนี้, รอบเดือน พ.ศ., วันที่ออกบิล, วันครบกำหนด, ยอดแต่ละรายการ, ยอดรวม)
  - ตารางประวัติบิลของรอบที่เลือก แสดงรอบบิลเป็น พ.ศ. และวันครบกำหนด; มิเตอร์ในตารางและสรุปบิลมาจากบิล จึงไม่เปลี่ยนตามการแก้มิเตอร์ภายหลัง; ดับเบิลคลิกเพื่อดูรายละเอียดและแก้ไขบิล
  - แก้ไขบิลที่สร้างแล้วผ่าน `UpdateBillChargesService` ได้เฉพาะช่องติ๊กค่าจอดมอเตอร์ไซค์, ค่าจอดรถยนต์ และยกเว้น Cable TV แล้วคำนวณยอดรวมใหม่ โดยคงค่าเช่า ค่าน้ำ ค่าไฟ เลขใบแจ้งหนี้ และวันที่ออกบิลเดิม; ปุ่มเปลี่ยนเป็น "บันทึกการแก้ไขบิล" และออกจากโหมดแก้ไขเมื่อเปลี่ยนห้องหรือรอบบิล

### ใบแจ้งหนี้

- ปุ่มเหนือตารางบิลพิมพ์ได้ทั้งรอบบิลหรือเฉพาะห้องที่เลือก (Ctrl/Shift เลือกหลายแถว) โดยเลือกเครื่องพิมพ์และดูตัวอย่างก่อนพิมพ์
- `MonthlyInvoiceBuilder` เตรียมข้อมูลจาก snapshot ในบิล และ `MonthlyInvoicePrintDocument` จัดหน้า A4 แผ่นละ 3 ใบตามแบบบิลกระดาษ โดยแยกค่าจอดมอเตอร์ไซค์ ค่าจอดรถยนต์ และ Cable TV คนละบรรทัด
- แท็บ "ตั้งค่าพื้นฐาน" แก้ชื่อหอพัก ที่อยู่ และบัญชีสำหรับโอนเงินได้; ถ้ายังไม่ได้กรอกชื่อและที่อยู่ ระบบจะไม่พิมพ์ใบแจ้งหนี้และแจ้งให้กรอกก่อน
- ใบแจ้งหนี้ของบิลที่จ่ายแล้วแสดง "ชำระแล้ว เมื่อวันที่ ... (ช่องทาง)" ที่บรรทัดกำหนดชำระ; "พิมพ์ทั้งรอบ" ยังพิมพ์ทุกบิลรวมบิลที่จ่ายแล้ว

### การรับชำระ

- แถบเหนือตารางบิลเลือกวันที่รับเงิน (ค่าเริ่มต้นวันนี้ และห้ามเกินวันนี้) และช่องทางเงินสดหรือโอนเข้าบัญชี แล้วกด "บันทึกรับชำระ" กับบิลที่เลือกทีละ 1 รายการ โดยรับเฉพาะเต็มจำนวน
- ตารางบิลมีคอลัมน์ "สถานะการชำระ" แสดง "ยังไม่จ่าย" หรือ "จ่ายแล้ว" พร้อมวันที่รับเงินและช่องทาง
- บิลที่จ่ายแล้วแก้ไขช่องติ๊กไม่ได้ทั้งใน UI และ `Bill`; ปุ่ม "ยกเลิกการรับชำระ" ถามยืนยันก่อน (ค่าเริ่มต้นของหน้าต่างคือ "ไม่") แล้วคืนบิลเป็นยังไม่จ่ายเพื่อแก้ไขต่อได้
- บิลที่ยังไม่จ่ายและเลยวันครบกำหนดแสดง "เกินกำหนด N วัน" เป็นสีแดง โดยคำนวณจากวันที่ปัจจุบันและไม่ได้บันทึกเป็นสถานะ; วันครบกำหนดเองยังไม่นับว่าเกิน
- ช่องติ๊ก "แสดงเฉพาะบิลค้างชำระทุกเดือน" แสดงบิลที่ยังไม่จ่ายทุกรอบ เรียงจากครบกำหนดเก่าสุด และปุ่มพิมพ์ทั้งหมดจะพิมพ์เฉพาะบิลในมุมมองนี้; ข้อความสรุปเหนือตารางแสดงจำนวนบิล ยอดรวม และจำนวนบิลเกินกำหนดของแถวที่แสดง
- ข้อมูลการรับชำระอยู่ใน In-Memory จึงหายเมื่อปิดแอปเช่นเดียวกับบิล; `SqlServerBillRepository` ยังไม่มีคอลัมน์รองรับ

### รายงาน

- แท็บ "รายงาน" ดูข้อมูลอย่างเดียว คำนวณโดย `MonthlyReportBuilder` และโหลดใหม่ทุกครั้งที่เปิดแท็บหรือเปลี่ยนรอบบิล
- การ์ดสรุปของรอบบิลที่เลือก: ยอดเรียกเก็บ รับแล้ว และค้างชำระ พร้อมจำนวนบิล; "รับแล้ว" นับตามรอบบิล ไม่ใช่วันที่รับเงิน รับแล้วกับค้างชำระจึงรวมกันเท่ายอดเรียกเก็บเสมอ
- การ์ดห้องพักแสดงสถานะปัจจุบัน ไม่ย้อนตามรอบบิลที่เลือก
- แสดงยอดเรียกเก็บแยกประเภท (รวมบิลที่ยังไม่จ่าย) และยอดรับชำระแยกเงินสดกับโอนเข้าบัญชีของรอบที่เลือก
- รายการค้างชำระแสดงทุกรอบบิล เรียงจากครบกำหนดเก่าสุด โดยใช้ชื่อผู้เช่าที่เก็บไว้ในบิล
- มุมมองรายปี (`YearlyReportBuilder`) แสดงตาราง ม.ค.-ธ.ค. พร้อมแถวรวมทั้งปี แยกตามประเภทค่าใช้จ่าย ยอดเรียกเก็บ รับแล้ว และค้างชำระ โดยใช้ปีปฏิทินและนับตามรอบบิล
- ส่งออกตารางรายปีเป็น Excel ด้วย `YearlyReportExcelExporter` (package ClosedXML; จำนวนเงินเป็นตัวเลขจริง) และเป็น PDF ด้วย `YearlyReportPrintDocument` ผ่านเครื่องพิมพ์ Microsoft Print to PDF ของ Windows; ไฟล์ส่งออกเป็นยอดรวม ไม่มีชื่อผู้เช่า และปีที่ไม่มีบิลจะไม่ให้ส่งออก
- ส่งออกรายงานรายเดือนเป็น Excel ด้วย `MonthlyReportExcelExporter` (แยกชีต "สรุป" กับ "ค้างชำระ") และเป็น PDF ด้วย `MonthlyReportPrintDocument` (A4 แนวตั้ง แบ่งหน้าเมื่อรายการยาว) โดยใช้ปุ่มเดียวกับรายปีและส่งออกตามมุมมองที่เปิดอยู่; รายการค้างชำระครอบคลุมทุกเดือน ณ วันที่ส่งออก **และมีชื่อผู้เช่ากับยอดหนี้ จึงควรระวังเมื่อส่งไฟล์ต่อ**
- ยังไม่มีกราฟ; รายงานเห็นเฉพาะข้อมูลตั้งแต่เปิดแอปครั้งนั้น เพราะบิลยังอยู่ใน In-Memory จึงยังใช้เป็นตัวเลขบัญชีจริงไม่ได้ รวมถึงไฟล์ที่ส่งออกด้วย

### อื่น ๆ

- `Program.cs` seed ข้อมูลตัวอย่างเมื่อเปิดครั้งแรก: 10 ห้อง (101-105, 201-205), ผู้เช่า 7 ราย และมิเตอร์รอบเดือน 09 ของปีปัจจุบัน
- `BillingPeriod` รองรับ `TryParse`, `GetPrevious()` และ `ToBuddhistValue()`
- ข้อความแจ้งผู้ใช้ใน UI เป็นภาษาไทย

## 3. Flow การทำงานหลัก

```text
[ตั้งค่า]      BillingSettingsTabControl → UpdateBillingSettingsService → IBillingSettingsRepository
[ห้องพัก]      RoomTabControl → CreateRoomService / UpdateRoomRentService → IRoomEditor
[ผู้เช่า]      TenantTabControl → CreateTenantService → ITenantRepository
[มิเตอร์]      MeterReadingTabControl → SaveMonthlyMeterReadingService → IMonthlyMeterReadingRepository
[สร้างบิล]     MonthlyBillTabControl → CreateMonthlyBillService → BillingCalculator → IBillRepository
```

ลำดับการใช้งานที่ออกแบบไว้: ตั้งค่าราคากลาง → เพิ่มห้องและผู้เช่า → บันทึกมิเตอร์ของรอบ → สร้างบิลทีละห้อง

## 4. การจัดเก็บข้อมูล

### แอปที่รันจริง

`Program.cs` ใช้ **In-Memory repository ทั้งหมด** (ห้อง, ผู้เช่า, มิเตอร์, บิล, ตั้งค่าราคา) ข้อมูลจึงหายเมื่อปิดโปรแกรม

### SQL Server (มี repository และ script แล้ว แต่ยังไม่ได้เชื่อมเข้าแอป)

1. `dbo.Rooms` — `Id`, `RoomNumber` (unique)
2. `dbo.Bills` — `Id`, `RoomId` (FK), `BillingPeriod` (`YYYY-MM`), `RentAmount`, `WaterAmount`, `ElectricityAmount`, `TotalAmount`; unique `(RoomId, BillingPeriod)`
3. `dbo.Tenants` — `Id`, `FirstName`, `LastName`, `PhoneNumber`, `RoomId` (FK, unique เมื่อไม่เป็น NULL)

ยังไม่มีตารางสำหรับ billing settings, monthly meter readings, ค่าเช่า/สถานะ Cable TV ของห้อง และยอดจอดรถ/Cable TV/เลขใบแจ้งหนี้ของบิล

## 5. การทดสอบและ Build

- `dotnet test` ผ่าน 144 รายการ (ล้มเหลว 0, ข้าม 0) เมื่อ 2026-11-03
- มี unit tests สำหรับ Domain, Application และ In-Memory repository
- มี UI tests ของ `RoomForm` 18 รายการ ครอบคลุมแท็บห้องพัก, ผู้เช่า และสร้างบิล (รวมการโหลดมิเตอร์อัตโนมัติ, คำเตือนเมื่อไม่มีมิเตอร์ และการเลื่อนไปห้องถัดไป)
- ยังไม่มี UI tests เฉพาะสำหรับแท็บบันทึกมิเตอร์และแท็บตั้งค่าราคากลาง
- มี integration tests สำหรับ SQL Server (Rooms, Bills, Tenants) ซึ่งต้องมี SQL Server; อ่าน connection string จาก `RENTFLOW_TEST_CONNECTION` หรือใช้ `localhost\SQLEXPRESS` แบบ Windows Authentication เมื่อไม่กำหนด

## 6. GitHub Actions

Workflow อยู่ที่ `.github/workflows/dotnet.yml` และทำงานตามลำดับ:

1. Checkout repository
2. ติดตั้ง .NET 10
3. ติดตั้ง SQL Server 2022 บน runner
4. สร้างฐานข้อมูล `RentFlowDb`
5. สร้างตารางจาก `001_CreateRooms.sql`, `002_CreateBills.sql` และ `003_CreateTenants.sql`
6. Restore, Build แบบ Release และ Run tests

รหัสผ่าน SQL Server ของ CI อ่านจาก GitHub Actions Secret `RENTFLOWMINI_SECRET` และไม่เก็บค่าไว้ใน workflow

## 7. การเปลี่ยนแปลงล่าสุด (ตั้งแต่ 2026-10-18)

- ขยาย `Bill` และ `BillingCalculator` เป็น 6 รายการ และเพิ่มค่าจอดรถ/Cable TV ใน `CreateMonthlyBillRequest` และ `CreateMonthlyBillService`
- แยก UI ออกจาก `RoomForm` เป็น tab control แยกไฟล์ (`BaseTabControl`, `RoomTabControl`, `MeterReadingTabControl`, `MonthlyBillTabControl`, `TenantTabControl`, `BillingSettingsTabControl`)
- เพิ่ม `BillingPeriodSelector` ใช้ร่วมกันในแท็บมิเตอร์และแท็บสร้างบิล
- แท็บสร้างบิลโหลดมิเตอร์อัตโนมัติและเตือนเมื่อยังไม่ได้จดมิเตอร์
- เพิ่มเลขใบแจ้งหนี้ (`InvoiceNumber`), วันที่ออกบิล (`CreatedAt`), `IBillRepository.Get(BillId)` และตารางประวัติบิล
- แสดงรอบบิลเป็น พ.ศ. และขยายตัวอักษรในกล่องสรุปบิล

## 8. สิ่งที่ยังไม่อยู่ในขอบเขตปัจจุบัน

- เชื่อม SQL Server repository เข้ากับแอปจริง และ persistence ของ settings / meter readings
- การแก้ไขหรือลบห้องพัก (นอกจากค่าเช่า) และการลบผู้เช่า
- ค่าตั้งต้นการจอดรถรายห้อง (`RoomParkingSettings`)
- การยกเลิกบิล และการแก้ไขค่าเช่า ค่าน้ำ หรือค่าไฟของบิลเดิม
- ค่าปรับล่าช้า การรับชำระบางส่วน และการยกยอดค้างเข้าบิลเดือนถัดไป
- ผู้เช่าเข้าออกกลางเดือน และประวัติค่าเช่า

## 9. ความเสี่ยงและข้อจำกัด

- **บิลใน SQL Server ไม่ครบ:** `SqlServerBillRepository` บันทึกเฉพาะค่าเช่า/น้ำ/ไฟ/ยอดรวม ไม่บันทึกค่าจอดรถ, Cable TV, `InvoiceNumber`, `CreatedAt` และ `GetAll()` / `Get()` ยังคืนค่าว่าง ต้องแก้ schema และ repository ก่อนเปลี่ยนแอปไปใช้ SQL Server
- **กฎที่ยังไม่บังคับในระดับ service:** `CreateMonthlyBillService` ยังไม่ตรวจว่ามีผู้เช่าหรือมีมิเตอร์ที่บันทึกแล้ว (BR-02, BR-03 ใน [billing-calculation-implementation-plan.md](billing-calculation-implementation-plan.md)); UI แสดงคำเตือนแต่ผู้ใช้ยังพิมพ์ค่ามิเตอร์เองแล้วสร้างบิลได้ และห้องว่างยังปรากฏให้เลือก
- ข้อมูลทั้งหมดอยู่ในหน่วยความจำ ปิดโปรแกรมแล้วหาย
- Integration tests ต้องใช้ SQL Server และฐานข้อมูลที่เตรียมไว้; CI ใช้เวลานานเพราะติดตั้ง SQL Server ใหม่ทุกครั้ง
- Credential เดิมเคยอยู่ใน Git history และถูกเปลี่ยนแล้ว; workflow ปัจจุบันใช้ GitHub Secret
- ยังไม่มี license อย่างเป็นทางการ
