namespace RentFlowApp.Application.Billing;

// Warning มีค่าเมื่อบันทึกแล้ว แต่บิลของรอบนั้นยังใช้เลขมิเตอร์เดิม ผู้ใช้ต้องไปดึงข้อมูลห้องใหม่ที่บิลเอง
public sealed record MeterReadingSaveResult(string? Warning);
