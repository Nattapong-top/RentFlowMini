namespace RentFlowApp.Application.Billing;

// Warning มีค่าเมื่อบันทึกแล้ว แต่บิลของรอบนั้นยังใช้เลขมิเตอร์เดิม ผู้ใช้ต้องไปดึงข้อมูลห้องใหม่ที่บิลเอง
// IsUpdate เป็นจริงเมื่อห้องนี้เคยบันทึกมิเตอร์ของรอบนั้นไว้แล้ว และการบันทึกครั้งนี้เป็นการแก้ไข
public sealed record MeterReadingSaveResult(string? Warning, bool IsUpdate);
