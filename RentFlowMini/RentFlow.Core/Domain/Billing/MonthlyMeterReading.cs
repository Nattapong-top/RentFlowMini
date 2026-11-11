using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Domain.Billing;

public sealed class MonthlyMeterReading
{
    private MonthlyMeterReading(
        RoomId roomId,
        BillingPeriod billingPeriod,
        MeterReading previousWaterMeter,
        MeterReading currentWaterMeter,
        MeterReading previousElectricityMeter,
        MeterReading currentElectricityMeter)
    {
        RoomId = roomId;
        BillingPeriod = billingPeriod;
        PreviousWaterMeter = previousWaterMeter;
        CurrentWaterMeter = currentWaterMeter;
        PreviousElectricityMeter = previousElectricityMeter;
        CurrentElectricityMeter = currentElectricityMeter;
    }

    public RoomId RoomId { get; }

    public BillingPeriod BillingPeriod { get; }

    public MeterReading PreviousWaterMeter { get; }

    public MeterReading CurrentWaterMeter { get; }

    public MeterReading PreviousElectricityMeter { get; }

    public MeterReading CurrentElectricityMeter { get; }

    public static MonthlyMeterReading Create(
        RoomId roomId,
        BillingPeriod billingPeriod,
        MeterReading? previousWaterMeter,
        MeterReading? currentWaterMeter,
        MeterReading? previousElectricityMeter,
        MeterReading? currentElectricityMeter)
    {
        ArgumentNullException.ThrowIfNull(roomId);
        ArgumentNullException.ThrowIfNull(billingPeriod);

        var waterReadings = ValidateReadings(
            "มิเตอร์น้ำ",
            previousWaterMeter,
            currentWaterMeter);
        var electricityReadings = ValidateReadings(
            "มิเตอร์ไฟฟ้า",
            previousElectricityMeter,
            currentElectricityMeter);

        return new MonthlyMeterReading(
            roomId,
            billingPeriod,
            waterReadings.Previous,
            waterReadings.Current,
            electricityReadings.Previous,
            electricityReadings.Current);
    }

    private static (MeterReading Previous, MeterReading Current) ValidateReadings(
        string meterType,
        MeterReading? previous,
        MeterReading? current)
    {
        if (previous is null || current is null)
        {
            throw new InvalidMeterReadingException(
                meterType,
                "ต้องระบุค่ามิเตอร์ก่อนหน้าและค่าปัจจุบัน");
        }

        if (current.Value < previous.Value)
        {
            throw new InvalidMeterReadingException(
                meterType,
                "ค่าปัจจุบันต้องไม่น้อยกว่าค่าก่อนหน้า");
        }

        return (previous, current);
    }
}
