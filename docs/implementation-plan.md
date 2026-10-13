# แผนการ Implement UC-01: สร้างบิลประจำเดือน

เอกสารนี้ใช้เป็นแผนทำงานสำหรับ UC-01 รุ่น MVP โดยแบ่งงานเป็นเรื่องเล็ก ๆ เพื่อให้ป๋าตรวจสอบผลลัพธ์ได้ทีละขั้น และลดความเสี่ยงจากการทำหลายส่วนพร้อมกัน

เอกสารที่เกี่ยวข้อง:

- [UC-01: สร้างบิลประจำเดือน](uc-01-create-monthly-bill.md)
- [Current Code Status](current-code-status.md)
- มาตรฐาน [Micro Development](../.ai/standards/micro_development.md)

## 1. ขอบเขต MVP

ระบบจะรองรับการสร้างบิลของห้องพัก โดยมีเฉพาะ:

- ค่าเช่า
- ค่าน้ำจากผลต่างมิเตอร์
- ค่าไฟจากผลต่างมิเตอร์
- ยอดรวม
- รอบบิลรูปแบบ `YYYY-MM`
- การป้องกันบิลซ้ำของห้องและรอบบิลเดียวกัน

ยังไม่ทำในแผนนี้:

- ผู้เช่าและเจ้าของห้อง
- Cable และ Parking
- การเปลี่ยนผู้เช่ากลางเดือน
- ประวัติค่าเช่า
- การ Recalculate บิลเดิม
- ราคาที่แยกตามอาคารหรือห้อง

## 2. หลักการทำงาน

แต่ละงานย่อยจะทำตามลำดับ:

```text
เขียนเอกสาร/ข้อตกลงสั้น ๆ
    ↓
เขียน Test ที่อธิบายพฤติกรรมหนึ่งเรื่อง
    ↓
ทำ Code เท่าที่จำเป็นให้ Test ผ่าน
    ↓
Refactor โดยไม่เปลี่ยนพฤติกรรม
    ↓
รัน Test และตรวจ diff
    ↓
รายงานผลและรออนุมัติเรื่องถัดไป
```

ถ้ายังไม่มี implementation สามารถสร้างโครง class หรือ method ที่ throw `NotImplementedException` ได้ แต่ต้องระบุชัดว่าเป็น RED scaffold และยังไม่ถือว่าเสร็จ

## 3. แผนแต่ละ Phase

### Phase 0: เตรียมขอบเขตและโครงสร้าง — เสร็จแล้ว

เป้าหมาย: ทำให้ข้อกำหนด MVP และโครงสร้างเริ่มต้นชัดเจน

ผลลัพธ์:

- ลดขอบเขต UC-01 ให้เหลือ Rent, Water, Electricity และ Total
- แยกเอกสารข้อกำหนดออกจากเอกสารสถานะโค้ด
- กำหนดแนวทางทำงานแบบ Micro Development
- ตรวจสอบโครงสร้าง Domain, Application, Infrastructure และ Tests ที่มีอยู่

### Phase 1: คำนวณยอดบิลใน Domain — เสร็จแล้ว

เป้าหมาย: ให้ระบบคำนวณจำนวนเงินพื้นฐานได้ โดยยังไม่ผูกกับฐานข้อมูลหรือหน้าจอ

พฤติกรรมที่ทำแล้ว:

- รับค่าเช่า มิเตอร์น้ำ มิเตอร์ไฟ และอัตราค่าบริการ
- คำนวณค่าน้ำจากผลต่างมิเตอร์
- คำนวณค่าไฟจากผลต่างมิเตอร์
- คำนวณยอดรวม
- ปฏิเสธมิเตอร์ที่ไม่มีค่า
- ปฏิเสธมิเตอร์ปัจจุบันที่น้อยกว่าค่าก่อนหน้า
- ใช้ `InvalidMeterReadingException` ร่วมกัน
- แจ้งข้อผิดพลาดมิเตอร์เป็นภาษาไทย

องค์ประกอบหลัก:

- `BillingCalculator`
- `BillingCalculationResult`
- `InvalidMeterReadingException`
- `BillingCalculatorTests`

Commit หลักของ Phase 1:

```text
5c51b74 feat: add monthly bill calculation and meter validation
```

### Phase 2: สร้างโมเดล Bill — เสร็จแล้วในระดับ Domain

เป้าหมาย: แปลงผลการคำนวณให้เป็นข้อมูลบิลที่มีโครงสร้าง

สิ่งที่ทำแล้ว:

1. สร้าง `Bill` ที่มี Room, Billing Period, Rent, Water, Electricity และ Total
2. ตรวจสอบ Billing Period ให้เป็นรูปแบบ `YYYY-MM`
3. สร้าง `Money` Value Object และป้องกันค่าเงินติดลบ
4. ใช้ `Money` ในผลคำนวณและโมเดล Bill
5. เขียน Test แยกตามความรับผิดชอบของ Bill, BillingPeriod และ Money

ผลลัพธ์ที่ต้องได้:

- Domain model ของ Bill ที่ไม่ขึ้นกับฐานข้อมูลหรือ UI
- Error กลางสำหรับข้อมูล Bill ที่ไม่ถูกต้อง หากจำเป็น
- ยังไม่สร้าง `BillId` ใน Phase นี้ โดยเลื่อนไป Phase 4 ซึ่งเป็นช่วงเชื่อม persistence

### Phase 3: สร้าง Use Case การสร้างบิล — เสร็จในระดับการคำนวณจาก Request

เป้าหมาย: รวมลำดับการทำงานตั้งแต่รับคำขอจนได้ Bill หนึ่งใบ

ลำดับที่คาดหวัง:

```text
รับ Room และ Billing Period
    ↓
อ่านค่าเช่าและ Meter ที่จำเป็น
    ↓
เรียก BillingCalculator
    ↓
สร้าง Bill
    ↓
ส่ง Bill กลับให้ผู้เรียก
```

สิ่งที่ทำแล้ว:

1. สร้างคำขอ `CreateMonthlyBill`
2. ใช้ `MeterReading` Value Object กับข้อมูลมิเตอร์
3. เรียก `BillingCalculator` จาก Application Service
4. คืน Bill เมื่อข้อมูลครบและคำนวณสำเร็จ

สิ่งที่ยังไม่อยู่ใน Phase 3:

- อ่านข้อมูลห้องและ Meter ผ่าน Repository
- หยุดเมื่อไม่พบห้องจากฐานข้อมูล
- ป้องกัน Bill ซ้ำ
- บันทึก Bill

### Phase 4.1: Domain และ In-Memory Repository — เสร็จแล้ว

เป้าหมาย: ทำให้การสร้างบิลมีความถูกต้องเมื่อมีข้อมูลเดิมอยู่แล้ว

สิ่งที่ทำแล้ว:

1. เพิ่ม `IBillRepository`
2. กำหนดและสร้าง `BillId`
3. ตรวจสอบ Room + Billing Period ซ้ำก่อนบันทึก
4. ปฏิเสธการสร้างบิลซ้ำ
5. เพิ่ม In-Memory Repository สำหรับทดสอบ

### Phase 4.2: SQL Server Persistence — เสร็จแล้ว

เป้าหมาย: บันทึกและอ่าน Bill จาก SQL Server จริง

สิ่งที่ทำแล้ว:

1. เพิ่ม SQL script สำหรับตาราง Bills (`002_CreateBills.sql`)
2. สร้าง `SqlServerBillRepository` พร้อมการตรวจสอบและจัดการข้อผิดพลาดบิลซ้ำ
3. เพิ่ม Integration Test สำหรับบันทึกและตรวจสอบบิลผ่าน SQL Server จริง
4. เพิ่มการสร้างตาราง Bills ใน GitHub Actions workflow

### Phase 5: เชื่อมหน้าจอ — เสร็จแล้ว

เป้าหมาย: ให้ผู้ดูแลเลือกห้อง ระบุรอบบิล และเห็นผลลัพธ์จาก Use Case

สิ่งที่ทำแล้ว:

1. ปรับปรุง `RoomForm` โดยใช้ `TabControl` แบ่งเป็นแท็บห้องพักและแท็บสร้างบิลประจำเดือน
2. เพิ่มส่วนเลือกห้อง, ระบุรอบบิล (`YYYY-MM`), และค่าเช่าห้อง
3. เพิ่มส่วนรับค่าน้ำ (อัตรา 19 บาท/หน่วย) และค่าไฟ (อัตรา 8 บาท/หน่วย)
4. แสดงสรุปผลลัพธ์ค่าเช่า ค่าน้ำ ค่าไฟ และยอดรวม (Total) เมื่อสร้างบิลสำเร็จ
5. แสดงข้อความแจ้งเตือนข้อผิดพลาดภาษาไทยเมื่อข้อมูลไม่ถูกต้อง หรือมิเตอร์ผิดปกติ หรือมีบิลซ้ำ
6. ปรับปรุง `Program.cs` เพื่อเชื่อมต่อ Use Case กับ Form และเพิ่ม Unit Tests ทดสอบ flow ระดับหน้าจอ (รวม 54 tests ผ่านครบ)

### Phase 6: ตรวจสอบ CI และเอกสาร — ยังไม่เริ่ม

เป้าหมาย: ให้โค้ดและ workflow ทำงานสอดคล้องกันบน GitHub Actions

รายการตรวจสอบ:

- SQL script ใช้ path จาก workspace ที่ถูกต้อง
- CI ใช้ secret ผ่าน environment variable โดยไม่บันทึกค่า secret
- Restore, Build และ Test ทำงานตามลำดับ
- ตรวจสอบผลการทำงานจริงของ GitHub Actions
- อัปเดต `current-code-status.md`

## 4. Definition of Done ของ MVP

ถือว่า UC-01 MVP เสร็จเมื่อ:

- สร้าง Bill จาก Room และ Billing Period ที่ถูกต้องได้
- คำนวณค่าเช่า ค่าน้ำ ค่าไฟ และยอดรวมถูกต้อง
- ปฏิเสธ Billing Period ที่ไม่ใช่ `YYYY-MM`
- ปฏิเสธข้อมูล Meter ที่ไม่ครบ
- ปฏิเสธ Current Meter ที่น้อยกว่า Previous Meter
- ป้องกัน Bill ซ้ำของ Room และ Billing Period เดิม
- บันทึกและอ่าน Bill ได้
- Tests ที่เกี่ยวข้องผ่านใน local/Visual Studio
- GitHub Actions ผ่านครบตาม workflow

## 5. ข้อจำกัดที่ต้องติดตามแยกต่างหาก

- Testhost ใน environment บางครั้งทำงานค้างหรือเชื่อมต่อไม่ได้ แม้ Visual Studio รัน Test ผ่าน
- GitHub Actions ใช้เวลาหลายนาทีเพราะติดตั้ง SQL Server ใหม่ทุกครั้ง
- Credential เดิมเคยอยู่ใน Git history และถูกเปลี่ยนแล้ว ปัจจุบัน workflow ใช้ GitHub Secret

ข้อจำกัดเหล่านี้ไม่ควรขยายขอบเขตของ Phase 2 จนกว่าจะมีอาการกระทบการใช้งานจริง

## 6. ขั้นตอนถัดไปที่แนะนำ
 
เริ่ม Phase 6: ตรวจสอบ CI และเอกสาร (ทดสอบ GitHub Actions workflow และอัปเดตเอกสารสรุปสถานะ)
