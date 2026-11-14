using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Billing;

public static class MeterReadingInput
{
    // meterName ใส่ในข้อความ error เพื่อบอกผู้ใช้ว่าช่องไหนผิด เช่น "มิเตอร์น้ำปัจจุบัน"
    public static MeterReading Parse(string? text, string meterName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidMeterReadingException(meterName, "ต้องระบุค่ามิเตอร์");
        }

        if (!decimal.TryParse(text.Trim(), out var value))
        {
            throw new InvalidMeterReadingException(meterName, "ต้องระบุเป็นตัวเลขที่ถูกต้อง");
        }

        return MeterReading.Create(value);
    }
}
