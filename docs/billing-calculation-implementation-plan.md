# แผนการ Implement: การคำนวณและสร้างบิลประจำเดือนฉบับสมบูรณ์ (Billing Calculation & UI Implementation Plan)

เอกสารนี้กำหนดแผนการทำงานและขั้นตอนการพัฒนาสำหรับการคำนวณและสร้างบิลประจำเดือน (Phase 3 & 4) ตามข้อกำหนดใน `docs/billing-charges-design.md` โดยยึดมั่นตามหลักการ **TDD (Test-Driven Development) และ Micro Development** ซึ่งกำหนดให้ **ทุกๆ Task ต้องมี Unit Test หรือ UI Test ประกอบและผ่าน 100% ในทันที** ก่อนจะถือว่า Task นั้นเสร็จสิ้น

**สถานะ:** อนุมัติและพร้อมดำเนินการ (Approved)  
**วันที่:** 2026-10-18  
**เอกสารอ้างอิง:** `docs/billing-charges-design.md`, `docs/uc-01-create-monthly-bill.md`, `docs/billing-settings-plan.md`, `.ai/standards/tdd.md`

---

## 1. ขอบเขตและรายการค่าใช้จ่ายทั้งหมด (Scope of Charges)

ระบบการสร้างบิลประจำเดือนจะคำนวณและบันทึกยอดจริง (Snapshot) ลงใน `Bill` ประกอบด้วย:

1. **ค่าเช่า (Rent):** อ้างอิงจากค่าเช่ารายเดือนปัจจุบันของห้องพัก (`Room.MonthlyRent`)
2. **ค่าน้ำประปา (Water):** `(มิเตอร์น้ำปัจจุบัน - มิเตอร์น้ำก่อนหน้า) × อัตราค่าน้ำประปา (WaterRate)`
3. **ค่าไฟฟ้า (Electricity):** `(มิเตอร์ไฟฟ้าปัจจุบัน - มิเตอร์ไฟฟ้าก่อนหน้า) × อัตราค่าไฟฟ้า (ElectricityRate)`
4. **ค่าจอดรถมอเตอร์ไซค์ (Motorcycle Parking):** คิดตามอัตรา `MotorcycleParkingRate` หากมีการเลือก Checkbox ในรอบบิลนั้น (โหลดค่าเริ่มต้นจาก `RoomParkingSettings`)
5. **ค่าจอดรถยนต์ (Car Parking):** คิดตามอัตรา `CarParkingRate` หากมีการเลือก Checkbox ในรอบบิลนั้น (โหลดค่าเริ่มต้นจาก `RoomParkingSettings`)
6. **ค่า Cable TV (Cable TV):** อัตรา `CableTvRate` ต่อรอบบิล ยกเว้นกรณีห้องพักมีสถานะ `IsCableTvExempt = true`

---

## 2. ขั้นตอนการดำเนินงานและกฎ TDD (Implementation Tasks & TDD Workflow)

ทุก Task ด้านล่างนี้ **ต้องเขียนเทสต์กำกับและรันผ่านให้เรียบร้อยในแต่ละขั้นตอน** ห้ามยกยอดเทสต์ไปทำรวมไว้ที่ Task สุดท้าย:

```text
[Billing Calculation & UI Implementation Plan (TDD)]
├── Task 1: ขยาย Domain Model (`Bill`, `BillingCalculator`)
│   ├── เขียน Unit Test สำหรับคำนวณค่าจอดรถและ Cable TV (Red)
│   ├── ปรับปรุง Domain Model และ Calculator ให้คำนวณครบ 6 รายการ (Green)
│   └── ยืนยัน Unit Tests ผ่าน 100%
├── Task 2: ขยาย Application Services (`CreateMonthlyBillService` & Request)
│   ├── เขียน Unit Test สำหรับ Service ตรวจสอบมิเตอร์และสร้างบิลพร้อมค่าใช้จ่ายทั้งหมด (Red)
│   └── พัฒนา Application Service และ Validation Rules (Green)
│   └── ยืนยัน Unit Tests ผ่าน 100%
├── Task 3: พัฒนา UI หน้าจอสร้างบิลประจำเดือน (`RoomForm` - Monthly Bill Tab)
│   ├── เขียน UI Test สำหรับโหลดมิเตอร์อัตโนมัติ เช็ค Checkbox จอดรถ และแสดงรายละเอียดแยกประเภท (Red)
│   └── พัฒนาหน้าจอและตรรกะการเชื่อมโยง UI (Green)
└── Task 4: Integration & Regression Validation
    └── รัน Automated Tests ทั้งระบบผ่าน 100% และตรวจสอบความถูกต้องตาม Definition of Done
```

---

## 3. กฎทางธุรกิจและ Validation (Business Rules)

- **BR-01:** Billing Period ใช้รูปแบบ `YYYY-MM`
- **BR-02:** ห้องว่างตลอดรอบบิล หรือห้องที่ไม่มีผู้เช่า จะไม่ถูกสร้างบิล
- **BR-03:** ต้องมีการบันทึกมิเตอร์น้ำประปาและไฟฟ้าของรอบบิลนั้นก่อน จึงจะสามารถสร้างบิลได้ หากไม่มีข้อมูลมิเตอร์ ห้ามสร้างบิล
- **BR-04:** Checkbox ค่าจอดรถ (มอเตอร์ไซค์/รถยนต์) โหลดค่าตั้งต้นจากห้อง แต่ผู้ดูแลสามารถปรับเปลี่ยนเฉพาะรอบบิลนั้นได้โดยไม่กระทบค่าตั้งต้นของห้อง
- **BR-05:** สถานะยกเว้น Cable TV (`IsCableTvExempt`) จะหักลดค่า Cable TV เป็น 0 โดยอัตโนมัติสำหรับห้องนั้น
- **BR-06:** บิลที่สร้างแล้วต้องเก็บ Snapshot ราคาและยอดเงินจริงไว้ ห้ามเปลี่ยนแปลงย้อนหลังแม้จะมีการเปลี่ยนราคากลางในภายหลัง

---

## 4. Definition of Done (ต้องมีเทสต์ประกอบในทุก Task)

- [ ] **Task 1 Done:** `Bill` และ `BillingCalculator` รองรับคำนวณ 6 รายการ พร้อม Unit Test ผ่าน 100%
- [ ] **Task 2 Done:** `CreateMonthlyBillService` รองรับการสร้างบิลพร้อม Parking & Cable TV พร้อม Unit Test ผ่าน 100%
- [ ] **Task 3 Done:** UI หน้าสร้างบิลโหลดค่ามิเตอร์อัตโนมัติ มี Checkbox จอดรถ และแสดงรายละเอียดครบ พร้อม UI Test ผ่าน 100%
- [ ] **Task 4 Done:** รัน Test Suite ทั้งโครงการผ่าน 100% ไม่มี Regression
