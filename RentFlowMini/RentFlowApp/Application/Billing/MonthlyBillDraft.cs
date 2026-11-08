using RentFlowApp.Domain.Billing;

namespace RentFlowApp.Application.Billing;

// ร่างบิลที่ยังไม่บันทึกและยังไม่ได้เลขใบแจ้งหนี้ ถ้าคำนวณไม่ได้จะมีแต่ Reason ให้หน้าจอแจ้งผู้ใช้
public sealed record MonthlyBillDraft(Bill? Bill, string? Reason);
