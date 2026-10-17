# Tenant Foundation Implementation Plan

เอกสารนี้เป็นแผน implement ระยะ 1 ของ Tenant foundation ตาม [roadmap](roadmap.md)

**สถานะ:** Implement ระยะ Domain, Application และ In-Memory Repository แล้ว; รอการเชื่อมต่อ UI และ SQL Server
**ขอบเขตที่ทำแล้ว:** Domain, Application และ In-Memory Repository

## 1. เป้าหมาย

เพิ่มความสามารถพื้นฐานสำหรับจัดเก็บและค้นหาข้อมูลผู้เช่า โดยไม่กระทบการทำงานของ Room และ Bill ที่มีอยู่แล้ว

## 2. Requirement ที่ยืนยันแล้ว

### ข้อมูลผู้เช่า

| ฟิลด์ | บังคับ | พฤติกรรมที่ implement |
|---|---:|---|
| ชื่อ | ใช่ | trim และปฏิเสธค่าว่างด้วย `TenantFirstNameRequiredException` |
| นามสกุล | ใช่ | trim และปฏิเสธค่าว่างด้วย `ArgumentException` |
| เบอร์โทร | ไม่ใช่ | trim และแปลงค่าว่าง/whitespace เป็น `null` โดยไม่ตรวจรูปแบบ |

### ความสัมพันธ์กับห้อง

- ผู้เช่าหนึ่งคนผูกกับได้ไม่เกินหนึ่งห้อง
- ห้องหนึ่งมีผู้เช่าปัจจุบันได้ไม่เกินหนึ่งคน
- `RoomId` เป็น optional จึงสร้าง Tenant โดยไม่มีห้องได้
- หากเลือกห้อง ต้องเลือกได้เฉพาะห้องที่ยังไม่มีผู้เช่าปัจจุบัน
- ห้องที่มีผู้เช่าแล้วต้องไม่แสดงในรายการห้องว่าง หรืออย่างน้อยต้องเลือกไม่ได้
- ยังไม่มี operation สำหรับย้ายห้องหรือยกเลิกการผูกห้อง

## 3. สิ่งที่ยังไม่กำหนด

เรื่องต่อไปนี้ยังไม่อยู่ใน behavior ที่ implement:

- การแก้ไขข้อมูลผู้เช่า
- การกำหนดสถานะผู้เช่าปัจจุบัน/ผู้เช่าที่สิ้นสุดการเช่า
- การตรวจชื่อหรือเบอร์โทรซ้ำ
- การกำหนดรูปแบบเบอร์โทร
- การกำหนดสถานะผู้เช่าและประวัติการเช่า
- SQL Server repository, migration, UI และ Billing integration

กติกาที่ยังไม่ implement ต้องไม่ถูกสรุปว่าเป็น behavior ที่ระบบรองรับแล้ว

## 4. ลำดับการพัฒนา

### Phase 1: Domain contract

1. เขียน tests สำหรับข้อมูลที่ถูกต้องและ validation
2. เพิ่ม `Tenant` model และ `TenantId`
3. เพิ่ม value object เฉพาะข้อมูลที่มี validation ที่ยืนยันแล้ว
4. กำหนด equality ด้วย `TenantId`

ผลลัพธ์ที่ต้องได้: Tenant ที่สร้างด้วยชื่อและนามสกุลถูกต้องได้ และเบอร์โทรเป็น optional

### Phase 2: Application contract

1. เพิ่ม `ITenantRepository`
2. เพิ่ม service สำหรับสร้าง Tenant ตาม pattern ของ `CreateRoomService`
3. ให้ service สร้าง `TenantId` ใหม่ แล้วส่ง Tenant ให้ repository บันทึก
4. รองรับการอ่าน Tenant ตาม `TenantId` และอ่านทั้งหมด

Repository ป้องกันการเพิ่ม Tenant ที่มี `TenantId` ซ้ำ และต้องเพิ่มการป้องกัน `RoomId` ซ้ำก่อนถือว่า Tenant foundation พร้อมสำหรับ UI

### Phase 3: In-Memory Repository

1. เพิ่ม `InMemoryTenantRepository`
2. รองรับการเพิ่มและอ่าน Tenant
3. ตรวจ duplicate ของ `TenantId` และ `RoomId` เมื่อมีการผูกห้อง
4. ไม่ให้ผู้เช่าสองคนผูกกับห้องเดียวกัน

### Phase 4: Verification

1. รัน tests เฉพาะ Domain, Application และ Infrastructure ที่เพิ่ม
2. รัน `dotnet test` ทั้งโปรเจกต์
3. รัน `dotnet build --configuration Release`
4. ตรวจ diff และยืนยันว่าไม่มีการเปลี่ยนแปลง UI, SQL หรือ Billing โดยไม่จำเป็น

## 5. ไฟล์ที่คาดว่าจะเกี่ยวข้อง

ไฟล์ใหม่ที่คาดว่าจะเพิ่ม:

- `RentFlowMini/RentFlowApp/Domain/Tenant.cs`
- `RentFlowMini/RentFlowApp/Domain/ValueObjects/TenantId.cs`
- `RentFlowMini/RentFlowApp/Application/Tenants/ITenantRepository.cs`
- `RentFlowMini/RentFlowApp/Application/Tenants/CreateTenantService.cs`
- `RentFlowMini/RentFlowApp/Infrastructure/Tenants/InMemoryTenantRepository.cs`
- Tests ภายใต้ `RentFlowApp.Tests/Domain`, `Application` และ `Infrastructure`

ไฟล์ที่ implement แล้วอยู่ตามรายการข้างต้น และมี tests แยกตาม Domain, Application และ Infrastructure

## 6. สิ่งที่ไม่อยู่ในงานนี้

- WinForms UI
- SQL Server schema และ migration
- การเชื่อม Tenant เข้ากับการสร้าง Bill
- การกำหนดจำนวนผู้เช่าสูงสุดต่อห้อง
- การย้ายผู้เช่า เปลี่ยนห้อง หรือประวัติการเช่า
- Cable, Parking และ Owner

## 7. Definition of Done

- [x] สร้าง Tenant ด้วยชื่อและนามสกุลที่ถูกต้องได้
- [x] ปฏิเสธชื่อหรือนามสกุลที่ว่าง
- [x] รองรับเบอร์โทรที่ไม่มีค่า
- [ ] ผู้เช่าหนึ่งคนผูกกับได้ไม่เกินหนึ่งห้อง
- [ ] ห้องหนึ่งมีผู้เช่าปัจจุบันได้ไม่เกินหนึ่งคน
- [ ] UI แสดงหรือเลือกเฉพาะห้องว่าง
- [x] มี tests สำหรับกรณีปกติและกรณีผิดพลาด
- [ ] Tests เดิมของ Room และ Billing ยังผ่าน
- [x] `dotnet build --configuration Release` ผ่าน
- [ ] ตรวจ secret ใน source code, เอกสาร และ log ครบถ้วน

## 8. ผลการทำงานล่าสุด

- Commit implementation ล่าสุด: `bf2dfd3 feat: add tenant foundation`
- Tenant tests ผ่าน 14 รายการ
- Release build ผ่านโดยไม่มี warning หรือ error
- Full test suite ผ่าน 61 รายการ และล้ม 7 รายการที่เป็น SQL Server integration tests เนื่องจาก environment ไม่รองรับ encryption certificate ของ SQL Server

## 9. แผนก่อนเริ่ม Tenant UI

1. เพิ่ม repository operation สำหรับตรวจว่าห้องมี Tenant อยู่แล้วหรือไม่ เช่น `ExistsByRoomId(RoomId roomId)`
2. ให้ Application service ปฏิเสธการผูกห้องที่ไม่ว่าง เพื่อป้องกันการ bypass UI
3. ปรับ In-Memory tests จาก “ห้องเดียวกันใช้ซ้ำได้” เป็น “ห้องเดียวกันใช้ซ้ำไม่ได้”
4. เพิ่ม UI test ว่า ComboBox แสดงเฉพาะห้องที่ยังไม่มี Tenant
5. จากนั้นจึงเริ่มสร้าง Tenant UI ทีละ 1 test

## 10. ข้อสังเกตและงานถัดไป

- `TenantFirstNameRequiredException` เป็น custom exception และมีข้อความภาษาไทย
- กรณีนามสกุลว่างยังใช้ `ArgumentException` ทั่วไป และข้อความใน code ยังไม่เป็นภาษาไทย จึงควรแก้ในรอบถัดไปให้สอดคล้องกับมาตรฐาน error ล่าสุด
- ยืนยันกติกาการแก้ไข/ย้ายห้องก่อนเพิ่ม behavior ดังกล่าว
- แผนการพัฒนา UI อย่างละเอียด: [tenant-ui-implementation-plan.md](tenant-ui-implementation-plan.md)
- แผนการพัฒนา SQL Server Persistence อย่างละเอียด: [tenant-sql-persistence-plan.md](tenant-sql-persistence-plan.md)


