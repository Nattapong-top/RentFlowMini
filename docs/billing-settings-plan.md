# แผนการ Implement: การตั้งค่าราคากลาง (Billing Settings)

เอกสารนี้กำหนดแผนการทำงานสำหรับ **Phase 1: การตั้งค่าราคากลาง** ตามข้อกำหนดใน `docs/billing-charges-design.md` โดยใช้แนวทาง **Micro Development** ทำงานจากหน่วยย่อยในหน่วยความจำ (In-Memory) และ UI ก่อน แล้วจึงเชื่อมต่อฐานข้อมูล SQL Server ในลำดับถัดไป

**สถานะ:** ขั้นตอนที่ 1 (In-Memory, Service & UI) เสร็จสิ้นแล้ว  
**วันที่:** 2026-10-17  
**ขอบเขต:** การจัดการราคากลาง 5 รายการ (น้ำ, ไฟ, ค่าจอดมอเตอร์ไซค์, ค่าจอดรถยนต์, Cable TV)  
**เอกสารอ้างอิง:** `docs/billing-charges-design.md`, `RentFlowApp/Domain/Billing/BillingSettings.cs`, `RentFlowApp/Application/Billing/IBillingSettingsRepository.cs`

---

## 1. เป้าหมายและหลักการ

1. มีจุดศูนย์กลางให้ผู้ดูแลดูและแก้ไขราคากลางชุดเดียวของระบบ
2. รองรับราคากลาง 5 รายการ:
   - ค่าน้ำประปา (Water Rate - บาท/หน่วย) - ค่าเริ่มต้น 19.00 บาท
   - ค่าไฟฟ้า (Electricity Rate - บาท/หน่วย) - ค่าเริ่มต้น 8.00 บาท
   - ค่าจอดรถมอเตอร์ไซค์ (Motorcycle Parking Rate - บาท/เดือน) - ค่าเริ่มต้น 100.00 บาท
   - ค่าจอดรถยนต์ (Car Parking Rate - บาท/เดือน) - ค่าเริ่มต้น 500.00 บาท
   - ค่า Cable TV (Cable TV Rate - บาท/เดือน) - ค่าเริ่มต้น 60.00 บาท
3. **ใช้ In-Memory พร้อมข้อมูลเริ่มต้น (Seed Data)** เพื่อให้ UI ใช้งานและทดสอบได้ทันทีโดยไม่ต้องพึ่งพาฐานข้อมูล
4. ข้อความแจ้งเตือนและ validation ต้องแสดงผลเป็นภาษาไทย และป้องกันราคาติดลบผ่าน `Money` VO
5. เมื่อ In-Memory และ UI ผ่านการทดสอบเรียบร้อยแล้ว จึงขยายผลสู่ตาราง SQL Server (`dbo.BillingSettings`)

---

## 2. ลำดับขั้นตอนการพัฒนา (2 ขั้นตอนหลัก)

```text
[ขั้นตอนที่ 1: In-Memory, Service & UI] — ✅ เสร็จแล้ว
├── 1.1 ปรับปรุง InMemoryBillingSettingsRepository ให้มี Seed Data เริ่มต้น (น้ำ 19, ไฟ 8, มอเตอร์ไซค์ 100, รถยนต์ 500, Cable TV 60)
├── 1.2 สร้าง UpdateBillingSettingsService ใน Application Layer (รับ Money VO)
├── 1.3 เพิ่มแท็บ "ตั้งค่าราคากลาง" ใน RoomForm ผ่าน User Control แยกไฟล์ (BillingSettingsTabControl.cs) พร้อม Font 11F และกรอกเฉพาะตัวเลข
└── 1.4 เขียนและรัน Unit Tests สำหรับ Repository, Service และ UI Flow (ผ่านครบ 131 tests)

[ขั้นตอนที่ 2: SQL Server Persistence] — ⏳ รอเริ่มทำรอบถัดไป
├── 2.1 สร้างสคริปต์ Database: 004_CreateBillingSettings.sql
├── 2.2 สร้าง SqlServerBillingSettingsRepository
└── 2.3 เพิ่ม Integration Tests บน SQL Server จริง
```

---

## 3. รายละเอียดการimplement ที่ทำเสร็จแล้ว (ขั้นตอนที่ 1)

### 3.1 InMemoryBillingSettingsRepository
- เพิ่ม constructor กำหนดค่าเริ่มต้น (Seed Data) อัตโนมัติ:
  - Water: 19.00
  - Electricity: 8.00
  - Motorcycle Parking: 100.00
  - Car Parking: 500.00
  - Cable TV: 60.00

### 3.2 UpdateBillingSettingsService
- ออกแบบให้รับพารามิเตอร์เป็น `Money` Value Object ทั้ง 5 รายการ เพื่อความปลอดภัยทางโดเมนและการตรวจสอบค่าติดลบ

### 3.3 BillingSettingsTabControl (UI แยกไฟล์)
- สร้างเป็น User Control แยกอิสระจาก `RoomForm.cs`
- กำหนดขนาด Font เป็น `Segoe UI 11F` ให้สอดคล้องกับหน้าจออื่นๆ
- เพิ่ม Numeric Validation ดักจับ `KeyPress` ป้องกันการกรอกตัวอักษร อนุญาตเฉพาะตัวเลขและจุดทศนิยม 1 จุดเท่านั้น

### 3.4 Automated Tests
- `InMemoryBillingSettingsRepositoryTests`
- `UpdateBillingSettingsServiceTests`
- `RoomFormTests` (ตรวจสอบการสร้างแท็บและการแสดงผลเริ่มต้น)
- รวมเทสต์ทั้งหมดผ่าน 131 รายการ

---

## 4. รายละเอียดขั้นตอนที่ 2 (SQL Server Persistence - ลำดับถัดไป)

1. เขียนสคริปต์ `004_CreateBillingSettings.sql`:
   - ตาราง `dbo.BillingSettings` เก็บ 1 แถวเสมอ (Single-row configuration)
   - ประกอบด้วยคอลัมน์: `WaterRate`, `ElectricityRate`, `MotorcycleParkingRate`, `CarParkingRate`, `CableTvRate`, `UpdatedAt`
   - ใส่ Seed Data เริ่มต้นลงในตาราง
2. พัฒนา `SqlServerBillingSettingsRepository`:
   - `Get()` และ `Save()`
3. เขียน Integration Tests ทดสอบบนฐานข้อมูล SQL Server จริง

---

## 5. Definition of Done สำหรับขั้นตอนที่ 1

- [x] `InMemoryBillingSettingsRepository` มี Seed Data เริ่มต้นที่ถูกต้อง
- [x] มี `UpdateBillingSettingsService` (รับ Money VO) พร้อม Unit Tests
- [x] มี User Control แยกไฟล์ `BillingSettingsTabControl` โหลดและบันทึกข้อมูลราคากลางได้
- [x] Font 11F และป้องกันการกรอกตัวหนังสือในช่องกรอกตัวเลข
- [x] แสดงข้อความภาษาไทยและป้องกันค่าติดลบ
- [x] Test ผ่านครบ 131 รายการไม่มี regression
