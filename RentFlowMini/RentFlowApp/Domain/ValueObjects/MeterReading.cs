using RentFlowApp.Domain.Billing;

namespace RentFlowApp.Domain.ValueObjects;

public sealed record MeterReading
{
    private MeterReading(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; }

    public static MeterReading Create(decimal value)
    {
        if (value < 0)
        {
            throw new InvalidMeterReadingException(
                "มิเตอร์",
                "ค่ามิเตอร์ต้องไม่ติดลบ");
        }

        return new MeterReading(value);
    }
}
