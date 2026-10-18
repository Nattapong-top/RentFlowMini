# แผนการ Implement: เฟส 2 บันทึกมิเตอร์น้ำ-ไฟประจำเดือน (Monthly Meter Reading)

เอกสารนี้กำหนดแผนการทำงานสำหรับ **Phase 2: การบันทึกและจัดการมิเตอร์น้ำ-ไฟประจำเดือน** ตามข้อกำหนดใน `docs/billing-charges-design.md` โดยใช้แนวทาง **Micro Development** และรักษามาตรฐาน UI ให้สอดคล้องกับระบบปัจจุบัน (เช่น ใช้ Font 11F, แยกไฟล์ UI, ป้องกันกรอกตัวหนังสือ)

**สถานะ:** กำลังวางแผนและรออนุมัติ  
**วันที่:** 2026-10-17  
**ขอบเขต:** บันทึกมิเตอร์น้ำ-ไฟประจำเดือนของห้องพัก ป้องกันการบันทึกซ้ำ และตรวจสอบค่ามิเตอร์ปัจจุบันต้องไม่น้อยกว่าค่าก่อนหน้า  
**เอกสารอ้างอิง:** `docs/billing-charges-design.md`, `RentFlowApp/Domain/Billing/MonthlyMeterReading.cs`, `RentFlowApp/Application/Billing/IMonthlyMeterReadingRepository.cs`

---

## 1. เป้าหมายและกฎทางธุรกิจ (Business Rules)

1. **การบันทึกต่อรอบบิล:** 
   - 1 ห้อง (Room) สามารถบันทึกมิเตอร์น้ำและไฟได้ 1 ชุดต่อ 1 รอบบิล (`BillingPeriod`, เช่น `2026-10`)
   - ห้ามบันทึกซ้ำในรอบบิลเดียวกัน (`IMonthlyMeterReadingRepository.Exists`)
2. **ความถูกต้องของตัวเลขมิเตอร์ (`MonthlyMeterReading` Domain):**
   - ต้องระบุทั้งค่ามิเตอร์ก่อนหน้า (`Previous`) และค่าปัจจุบัน (`Current`) ทั้งน้ำและไฟ
   - **ค่าปัจจุบันต้องไม่น้อยกว่าค่าก่อนหน้า** (`Current.Value >= Previous.Value`) หากน้อยกว่าต้องโยน `InvalidMeterReadingException`
3. **การเชื่อมโยงข้อมูล:**
   - เมื่อผู้ใช้เลือกห้องและรอบบิล ระบบควรพยายามดึง **"ค่ามิเตอร์ปัจจุบันของรอบบิลก่อนหน้า"** มาแสดงเป็น **"ค่ามิเตอร์ก่อนหน้า"** โดยอัตโนมัติ (ถ้ามี) เพื่อความสะดวกในการใช้งาน

---

## 2. ขอบเขตการพัฒนา (Scope of Work)

แผนงานแบ่งเป็น 4 ส่วนย่อยเพื่อให้ตรวจสอบและส่งมอบได้ทีละขั้น:

```text
[Phase 2: Monthly Meter Reading Implementation]
├── 2.1 Application Service: สร้าง SaveMonthlyMeterReadingService สำหรับตรวจสอบและบันทึกมิเตอร์
├── 2.2 UI Component: สร้าง MeterReadingTabControl (หรือขยายหน้าจอจัดการมิเตอร์แยกไฟล์) พร้อม Numeric Validation และ Font 11F
├── 2.3 Integration & Wiring: เชื่อมต่อ Repository, Service และ UI เข้ากับ RoomForm
└── 2.4 Automated Tests: เขียน Unit & UI Tests สำหรับ Service และ Validation ทั้งหมด
```

---

## 3. รายละเอียดการออกแบบทางเทคนิค

### 3.1 Application Service (`SaveMonthlyMeterReadingService`)
สร้าง Service ใน `RentFlowApp.Application.Billing`:
- **หน้าที่:**
  - รับข้อมูลจาก UI (RoomId, BillingPeriod, Water Previous/Current, Electricity Previous/Current)
  - ตรวจสอบว่ามีบันทึกซ้ำในรอบบิลนั้นแล้วหรือยัง (`IMonthlyMeterReadingRepository.Exists`) ถ้ามีให้แจ้งเตือนเป็นภาษาไทย
  - เรียก `MonthlyMeterReading.Create(...)` เพื่อตรวจสอบกฎ Domain
  - บันทึกลง Repository (`IMonthlyMeterReadingRepository.Add`)
- **Unit Tests:**
  - ทดสอบบันทึกสำเร็จเมื่อข้อมูลถูกต้อง
  - ทดสอบปฏิเสธเมื่อมิเตอร์ปัจจุบันน้อยกว่าก่อนหน้า
  - ทดสอบปฏิเสธเมื่อบันทึกซ้ำรอบเดิม

### 3.2 UI Component (`MeterReadingTabControl.cs`)
สร้าง User Control แยกไฟล์ (ตามแนวทางเดียวกับ `BillingSettingsTabControl`):
- **องค์ประกอบหน้าจอ:**
  - เลือกห้องพัก (ComboBox รายชื่อห้อง)
  - ระบุรอบบิล (TextBox เช่น `2026-10`)
  - กลุ่มมิเตอร์น้ำประปา: ค่าก่อนหน้า (`PrevWaterTextBox`), ค่าปัจจุบัน (`CurrWaterTextBox`)
  - กลุ่มมิเตอร์ไฟฟ้า: ค่าก่อนหน้า (`PrevElecTextBox`), ค่าปัจจุบัน (`CurrElecTextBox`)
  - ปุ่ม **"บันทึกมิเตอร์ประจำเดือน"**
  - ป้ายแสดงข้อความสถานะภาษาไทย (`_messageLabel`)
- **UX & Validation:**
  - ใช้ Font `Segoe UI 11F`
  - ใช้ Numeric Input Validation (กรอกได้เฉพาะตัวเลขและจุดทศนิยม)
  - ดึงค่ามิเตอร์ก่อนหน้าอัตโนมัติเมื่อเลือกห้องและรอบบิล (หรือให้ผู้ใช้กรอกเองกรณีเริ่มต้น)

---

## 4. Definition of Done สำหรับ Phase 2

- [ ] มี `SaveMonthlyMeterReadingService` พร้อม Unit Tests ตรวจสอบกฎธุรกิจ
- [ ] มี User Control แยกไฟล์ `MeterReadingTabControl` สำหรับบันทึกมิเตอร์น้ำ-ไฟ
- [ ] ป้องกันการบันทึกมิเตอร์ซ้ำในรอบบิลเดียวกัน
- [ ] ป้องกันมิเตอร์ปัจจุบันน้อยกว่าค่าก่อนหน้า และแสดงข้อความแจ้งเตือนภาษาไทยสุภาพ
- [ ] Test ผ่าน 100% ไม่มี regression กับเทสต์เดิม
