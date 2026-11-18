# เอกสารของ RentFlowMini

**สถานะ:** ใช้งานอยู่
**อัปเดตล่าสุด:** 2026-11-18
**ผู้อ่าน:** เจ้าของโปรเจ็กต์และ AI ที่ช่วยพัฒนา

สารบัญนี้บอกว่าเรื่องใดอยู่ในไฟล์ใด ถ้าต้องการรู้ว่าโปรแกรมทำอะไรได้ตอนนี้ ให้เริ่มที่ [current-code-status.md](current-code-status.md) ส่วนวิธีติดตั้ง เปิดใช้งาน และรันทดสอบอยู่ใน [README ของ repo](../README.md)

## เอกสารที่ใช้งานอยู่

| ไฟล์ | ตอบคำถาม |
| --- | --- |
| [current-code-status.md](current-code-status.md) | ตอนนี้โปรแกรมทำอะไรได้บ้าง และมีการเปลี่ยนแปลงอะไรล่าสุด |
| [decisions.md](decisions.md) | ทำไมจึงเลือกทางนี้ (ADR-001 ถึง ADR-006) |
| [roadmap.md](roadmap.md) | งานแบ่งเป็นระยะอย่างไร |
| [glossary.md](glossary.md) | คำศัพท์ที่ใช้ในหน้าจอ เอกสาร และโค้ด |

`roadmap.md` ยังมีเนื้อหาที่ไม่ตรงกับโค้ดบางส่วน เช่น ยังพูดถึง SQL Server ซึ่ง ADR-001 เปลี่ยนเป็น SQLite แล้ว

## เอกสารเก็บประวัติ

ไฟล์ใน [archive/](archive/) เป็นแผนหรือแบบที่เขียนไว้ก่อนลงมือ เก็บไว้เพื่อดูว่าเคยคิดอะไรและตัดสินใจอย่างไร **อย่าใช้เป็นข้อมูลของโค้ดปัจจุบัน** ถ้าเนื้อหาขัดกับเอกสารที่ใช้งานอยู่ ให้ยึดเอกสารที่ใช้งานอยู่และโค้ด

| ไฟล์ | เรื่อง |
| --- | --- |
| [uc-01-create-monthly-bill.md](archive/uc-01-create-monthly-bill.md) | Use case สร้างบิลประจำเดือน รุ่น MVP |
| [implementation-plan.md](archive/implementation-plan.md) | แผนทำ UC-01 รุ่น MVP |
| [billing-charges-design.md](archive/billing-charges-design.md) | แบบการคิดค่าใช้จ่ายรายเดือน |
| [billing-calculation-implementation-plan.md](archive/billing-calculation-implementation-plan.md) | แผนคำนวณบิล 6 รายการและหน้าจอสร้างบิล |
| [billing-settings-plan.md](archive/billing-settings-plan.md) | แผนตั้งค่าราคากลาง |
| [billing-meter-reading-plan.md](archive/billing-meter-reading-plan.md) | แผนบันทึกมิเตอร์น้ำ-ไฟประจำเดือน |
| [billing-meter-reading-ui-plan.md](archive/billing-meter-reading-ui-plan.md) | แผนหน้าจอบันทึกมิเตอร์ |
| [room-rent-cable-tv-design.md](archive/room-rent-cable-tv-design.md) | แบบค่าเช่าห้องและ Cable TV |
| [room-ui-design.md](archive/room-ui-design.md) | แบบหน้าจอจัดการห้องพัก |
| [room-tenant-vacancy-todo.md](archive/room-tenant-vacancy-todo.md) | งานค้างเรื่องผู้เช่าย้ายออกและกติกาแก้บิลที่ออกผิด |
| [tenant-implementation-plan.md](archive/tenant-implementation-plan.md) | แผนส่วนพื้นฐานของผู้เช่า |
| [tenant-ui-implementation-plan.md](archive/tenant-ui-implementation-plan.md) | แผนหน้าจอจัดการผู้เช่า |
| [tenant-sql-persistence-plan.md](archive/tenant-sql-persistence-plan.md) | แผนเก็บข้อมูลผู้เช่าใน SQL Server (ยกเลิกตาม ADR-001) |
| [sqlite-migration-plan.md](archive/sqlite-migration-plan.md) | แผนเปลี่ยนจาก SQL Server เป็น SQLite |

## มาตรฐานของเอกสาร

### หัวเอกสาร

ทุกไฟล์ที่ใช้งานอยู่ขึ้นต้นด้วยชื่อเรื่อง แล้วตามด้วยสามบรรทัดนี้

```markdown
**สถานะ:** ใช้งานอยู่ / ร่าง / เก็บประวัติ
**อัปเดตล่าสุด:** YYYY-MM-DD
**ผู้อ่าน:** ใครเป็นผู้อ่านหลัก
```

ไฟล์ที่ย้ายเข้า `archive/` ไม่ต้องแก้หัวเดิม แต่ต้องมีป้าย "เอกสารเก็บประวัติ" ต่อจากชื่อเรื่อง

### เนื้อหา

- หนึ่งเรื่องมีที่อยู่ที่เดียว ไฟล์อื่นใช้ลิงก์ไปหา ไม่คัดลอกเนื้อหาซ้ำ
- เขียนสิ่งที่โค้ดทำจริง ถ้ายังไม่ได้ตรวจกับโค้ด ให้เขียนว่ายังไม่ได้ตรวจ
- อ้างถึง class หรือไฟล์ด้วยชื่อในโค้ด เช่น `CreateMonthlyBillService` เพื่อให้ค้นเจอ
- ใช้ลิงก์แบบ relative เช่น `../README.md` ห้ามใช้ลิงก์ `file:///` ที่ชี้ไปที่ไดรฟ์ของเครื่อง
- ข้อความที่ขึ้น repo ใช้ภาษากลาง ไม่ใช้ชื่อเล่นหรือคำเรียกส่วนตัว
- เมื่อพฤติกรรมเปลี่ยน ให้แก้เอกสารที่เกี่ยวข้องใน PR เดียวกัน และแก้วันที่ "อัปเดตล่าสุด"

### Diagram

- ใช้ [Mermaid](https://mermaid.js.org/) เขียนเป็นข้อความในไฟล์ markdown เพราะ GitHub แสดงเป็นรูปได้ AI อ่านและแก้ต้นฉบับได้ และดูการเปลี่ยนแปลงใน diff ได้
- ของเล็ก เช่น โครงโฟลเดอร์ ใช้ตัวอักษรในบล็อก `text` ได้
- ไม่ใช้ไฟล์รูปที่แก้ได้เฉพาะในโปรแกรมวาดรูป เพราะ AI อ่านและดูแลต่อไม่ได้

### เมื่อไรจึงย้ายเข้า archive

ย้ายแผนหรือแบบเข้า `archive/` เมื่องานตามแผนเสร็จแล้ว หรือเมื่อมีการตัดสินใจที่ทำให้แผนนั้นใช้ไม่ได้ ถ้าการตัดสินใจนั้นสำคัญ ให้บันทึกเป็น ADR ใน [decisions.md](decisions.md) ด้วย
