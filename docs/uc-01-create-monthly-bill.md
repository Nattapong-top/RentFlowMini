# UC-01: สร้างบิลประจำเดือน (MVP)

**สถานะ:** เตรียมข้อกำหนด / ยังไม่เริ่มพัฒนา
**ผู้ใช้งานหลัก:** ผู้ดูแลอาคาร
**ขอบเขต:** ค่าเช่า ค่าน้ำ และค่าไฟ

## 1. เป้าหมาย

ให้ผู้ดูแลอาคารสร้างบิลรายเดือนของห้องที่เลือก โดยระบบคำนวณค่าเช่า ค่าน้ำ ค่าไฟ และยอดรวม แล้วบันทึกผลลัพธ์เป็น Bill

## 2. ขอบเขต MVP

ระบบรุ่นแรกจะรองรับเฉพาะ:

- ห้องพัก
- ค่าเช่าปัจจุบันของห้อง
- มิเตอร์น้ำก่อนหน้าและปัจจุบัน
- มิเตอร์ไฟก่อนหน้าและปัจจุบัน
- อัตราค่าน้ำและค่าไฟกลาง
- Billing Period รูปแบบ `YYYY-MM`
- การสร้างบิลใหม่

ยังไม่รวม:

- การแยก Owner / Tenant
- Cable
- Parking
- การเปลี่ยนผู้เช่ากลางเดือน
- ประวัติค่าเช่า
- การ Recalculate บิลเดิม
- การแบ่งราคาแยกตามอาคารหรือห้อง

## 3. Input

```text
Room
Billing Period
```

ข้อมูลที่ระบบอ่านจาก Room และการตั้งค่ากลาง:

```text
Current Rent
Previous Water Meter
Current Water Meter
Previous Electricity Meter
Current Electricity Meter
Water Rate
Electricity Rate
```

`Billing Period` ต้องใช้รูปแบบ `YYYY-MM` เช่น `2026-09`

## 4. Main Flow

1. ผู้ดูแลเลือก Room
2. ผู้ดูแลระบุ Billing Period
3. ระบบตรวจสอบ Input
4. ระบบอ่านค่าเช่าและข้อมูล Meter
5. ระบบตรวจสอบว่าข้อมูลจำเป็นครบและ Meter ปัจจุบันไม่ต่ำกว่าค่าก่อนหน้า
6. ระบบคำนวณค่าเช่า ค่าน้ำ และค่าไฟ
7. ระบบรวมยอดเป็น Total
8. ระบบสร้างและบันทึก Bill ใหม่
9. ระบบแสดงผล Bill

## 5. สูตรการคำนวณ

```text
Water Usage = Current Water Meter - Previous Water Meter
Water Amount = Water Usage × Water Rate

Electricity Usage = Current Electricity Meter - Previous Electricity Meter
Electricity Amount = Electricity Usage × Electricity Rate

Total = Rent + Water Amount + Electricity Amount
```

## 6. Business Rules

| ID | กฎธุรกิจ |
|---|---|
| BR-01 | Billing Period ใช้รูปแบบ `YYYY-MM` |
| BR-02 | Billing Period หมายถึงค่าใช้จ่ายทั้งหมดของเดือนนั้น |
| BR-03 | ทุกบิลใน MVP มีรายการ Rent, Water และ Electricity |
| BR-04 | ค่าเช่าใช้ค่าเช่าปัจจุบันของ Room |
| BR-05 | Water และ Electricity คำนวณจากผลต่างของ Meter |
| BR-06 | ถ้าข้อมูลจำเป็นไม่ครบ ต้องหยุดการสร้าง Bill |
| BR-07 | ถ้า Current Meter น้อยกว่า Previous Meter ต้องหยุดและแจ้งข้อมูลผิดปกติ |
| BR-08 | MVP สร้าง Bill ใหม่เท่านั้น ยังไม่ Recalculate Bill เดิม |

## 7. Alternative Flows

### AF-01: ข้อมูลไม่ครบ

```text
ตรวจข้อมูล
    ↓
ข้อมูลไม่ครบ
    ↓
หยุดการสร้าง Bill
    ↓
แจ้งผู้ดูแลให้เติมข้อมูล
```

ระบบต้องไม่เปลี่ยนข้อมูลที่หายให้เป็นศูนย์โดยอัตโนมัติ

### AF-02: Meter ปัจจุบันน้อยกว่าค่าก่อนหน้า

ให้หยุดการสร้าง Bill และแจ้งว่าข้อมูล Meter ไม่ถูกต้อง ห้ามแก้ข้อมูลต้นทางอัตโนมัติ

### AF-03: มี Bill ของ Room และ Period เดิม

ใน MVP ให้แจ้งว่า Bill มีอยู่แล้วและหยุดการสร้างซ้ำ การ Recalculate จะเพิ่มในระยะถัดไป

## 8. Output

ในระดับ Domain ของ Phase 2 ระบบยังไม่สร้าง `Bill ID` เพราะยังไม่เชื่อม persistence โดย `Bill ID` จะถูกกำหนดใน Phase 4 ตอนออกแบบ Repository และการบันทึกข้อมูล

```text
Bill
├── Bill ID
├── Room
├── Billing Period
├── Rent
├── Water
├── Electricity
└── Total
```

ตัวอย่าง:

```text
Bill ID       : B001
Room          : 101
Period        : 2026-09

Rent          : 2,800
Water         :   150
Electricity   :   800
----------------------
Total         : 3,750
```

## 9. ความรับผิดชอบเบื้องต้น

```text
UI / Application Boundary
    รับ Room และ Billing Period

Application
    ประสานลำดับการสร้าง Bill

Domain
    ตรวจข้อมูลและคำนวณ Rent, Water, Electricity, Total

Infrastructure
    อ่านข้อมูลและบันทึก Bill
```

Candidate components:

- `CreateMonthlyBill`
- `BillingCalculator`
- `Bill`
- `RoomRepository`
- `MeterRepository`
- `BillRepository`

รายการเหล่านี้เป็น design candidates ยังไม่ใช่ class ที่ implement แล้ว

## 10. เกณฑ์ยอมรับเบื้องต้น

- สร้าง Bill จาก Room และ Billing Period ที่ถูกต้องได้
- คำนวณ Rent, Water, Electricity และ Total ถูกต้อง
- ปฏิเสธ Billing Period ที่ไม่ใช่รูปแบบ `YYYY-MM`
- ปฏิเสธกรณีข้อมูล Meter ไม่ครบ
- ปฏิเสธกรณี Current Meter น้อยกว่า Previous Meter
- ไม่สร้าง Bill ซ้ำสำหรับ Room และ Billing Period เดิม
- บันทึกและอ่าน Bill ได้ถูกต้อง

## 11. งานระยะถัดไป

หลัง MVP ผ่านการทดสอบแล้ว ค่อยเพิ่มตามลำดับ:

1. Recalculate Bill เดิม
2. Tenant
3. Cable
4. Parking
5. Owner
6. ประวัติค่าเช่าและการเปลี่ยนผู้เช่า
7. Pricing แยกตาม Building
