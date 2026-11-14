namespace RentFlowApp.Application.Billing;

// ค่าเริ่มต้นของช่องกรอกมิเตอร์ เลขปัจจุบันเป็น null เมื่อยังไม่ได้บันทึกรอบนั้น
public sealed record MeterEntryDefaults(
    decimal PreviousWater,
    decimal PreviousElectricity,
    decimal? CurrentWater,
    decimal? CurrentElectricity,
    bool IsPreviousLocked);
