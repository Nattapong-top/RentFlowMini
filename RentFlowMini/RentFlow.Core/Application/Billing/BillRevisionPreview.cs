using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Billing;

// Revised เป็นสำเนาที่ยังไม่บันทึก ใช้แสดงร่างให้ผู้ใช้ดูก่อนยืนยัน
public sealed record BillRevisionPreview(Money OldTotal, Money NewTotal, Bill Revised);
